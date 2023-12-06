VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "LP"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
' Linear Programme Object V2
'
' L A Dale 24 Jan 2017
'
' 1 Jun 2018 Support for multisegment variables
' 4 Feb 2018 Updated use of MO object
' 5 Feb 2018 Uses EnterBasis to swap constraints, read-only cmap remains for debug purposes
' Dec 2020 Removed multisegment variables, added constraint names
' 17 Nov 2023 Replaced basis sparseinverse object with lu factors of basis

Option Explicit
Option Base 0

' This code solves an LP which in standard primal form is minimise Ct.X s.t. A.X <= B & X >= 0
' where Ct is the cost of each variable X and A.X <= B represents the constraints on X
' The code actually solves the dual form maximise Bt.Y such that At.Y = C & Y >= 0
' Y (and At) will need to be augmented by slack variables Ys to achieve the equalities
' The value of the primal variables X at the optimum can be derived from D (shadows of Y)
' Primal equality constraints can be flagged using EQ() as bool (means constraint not removed from basis but does not force initialisation into basis)
' Primal constraints can be skipped using Skip()as bool


#Const debugmsg = 2         ' 0=silent, 1 = not used, 2 = basis changes
Private Const FASTCOUNT As Long = -2        ' the number of constraint candidates to be considered for basis entry.  Make -ve to consider all constraints

Public Id As Long           'used to identify LP instance

'Inputs
Private atm As SparseMatrix 'Constraint matrix
Private bv() As Double      'Primal constraint magnitudes
Private ev() As Double      'Primal constraint magnitude modifiers
Private cv() As Double      'Variable costs
Private eqc() As Boolean    'Primal constraint is equality
Private sk() As Boolean     'Skip this constraint
Private vn() As String      'Variable name
Private cn() As String      'Constraint name

'Outputs
Private dv() As Double    'Slack of constraints
Private yv() As Double    'Dual variables

'Internals
Private maxiters As Long
Private startsearch As Long
Private basisvalid As Boolean

Private sv() As Double    'Sensitivity of dual variables to cost perturbations
Private ssv() As Double   'Save of sv for the most infringed constraint
Private bm As SparseMatrix ' basis formed by transposing selected constraint rows from atm
Private bmsolve As SolveLin ' factors bm and solves bm.X = B
Private vmax As Long      'number of variables-1 (column upb of Atm)
Private cmax As Long      'number of constraints-1 (row upb of Atm)
Private cm() As Long      'Ordering of rows (constraints) in atm, bv, etc)
Private vm() As Long      'map of constraints to basis variables
Private fastsearch As Long   'enable greedy column selection while >0


Public Sub Init(amat As SparseMatrix, bvec() As Double, cvec() As Double, vname() As String, cname() As String)
    Dim i As Long
    
    Set atm = amat
    bv = bvec
    cv = cvec
    vn = vname
    cn = cname
    vmax = atm.cupb
    cmax = atm.Rupb
        
    ReDim sk(cmax) As Boolean
    ReDim eqc(cmax) As Boolean
    ReDim dv(cmax) As Double
    ReDim ev(cmax) As Double
    ReDim yv(vmax) As Double
    ReDim sv(vmax) As Double
    ReDim cm(cmax) As Long
    ReDim vm(cmax) As Long
    
    Set bm = New SparseMatrix
    Set bmsolve = New SolveLin

    maxiters = (vmax + 1) * 2
    
    InitCOrder
End Sub

Public Sub InitCOrder()
    Dim i As Long
    
    For i = 0 To cmax
        cm(i) = i
        vm(i) = i
    Next i
    basisvalid = False
End Sub

Public Sub SaveCOrder(corder() As Long)
    corder = cm
End Sub

Public Sub RestoreCOrder(corder() As Long)
    Dim i As Long
    
    cm = corder
    For i = 0 To cmax
        vm(cm(i)) = i
    Next i
    basisvalid = False
End Sub

' Make basis transpose from relevant rows of atm, transpose btm and make solver
    
Private Sub MakeBasis()
    Dim btm As New SparseMatrix
    
    btm.CopyMap atm, cm, vmax
    bm.Transpose btm
    bmsolve.Init bm
    basisvalid = True
End Sub

' Update basis matrix by replacing col v with constraint c, recalc bmsolve

Private Sub UpdateBasis(v As Long, cons As Long)
    bm.ReplaceCol v, atm, cons
    bmsolve.Init bm
    basisvalid = True
End Sub

' Transposed constraint matrix

Public Property Get TConsMat() As SparseMatrix
    Set TConsMat = atm
End Property

'Mark inverse as invalid

Public Sub MatAltered()
    basisvalid = False
End Sub

' Equality flag

Public Property Let Equality(constraint As Long, flag As Boolean)
    eqc(constraint) = flag
End Property
Public Property Get Equality(constraint As Long) As Boolean
    Equality = eqc(constraint)
End Property

' Skip flag

Public Property Let Skip(constraint As Long, flag As Boolean)
    sk(constraint) = flag
End Property
Public Property Get Skip(constraint As Long) As Boolean
    Skip = sk(constraint)
End Property

' Column map
Public Property Get cmap(column As Long) As Long
    cmap = cm(column)
End Property

Public Property Let cname(constraint As Long, name As String)
    cn(constraint) = name
End Property
Public Property Get cname(constraint As Long) As String
    cname = cn(constraint)
End Property

' Set a variable using a constraint

Public Sub EnterBasis(ByVal var As Long, ByVal cons As Long)
    Dim oc As Long, cp As Long
    
    If cm(var) <> cons Then
        oc = cm(var)             ' save old contents of var
        cp = vm(cons)            ' current position of constraint
        cm(var) = cons
        cm(cp) = oc
        vm(cons) = var
        vm(oc) = cp
    End If
    basisvalid = False       ' Mark basis invalid even if already in basis
End Sub

' Costs
Public Property Let cvec(column As Long, cost As Double)
    cv(column) = cost
End Property
Public Property Get cvec(column As Long) As Double
    cvec = cv(column)
End Property

' Magnitudes  (NB adjusts original bv values)
Public Property Let bvec(column As Long, Magnitude As Double)
    bv(column) = Magnitude
End Property
Public Property Get bvec(column As Long) As Double
    bvec = bv(column)
End Property

' Magnitude modifiers  (NB adjusts original bv values)
Public Property Let evec(column As Long, Magnitude As Double)
    ev(column) = Magnitude
End Property
Public Property Get evec(column As Long) As Double
    evec = ev(column)
End Property


' Compute objective

Public Function Objective() As Double
    Dim i As Long, r As Long
    Dim res As Double
        
    For i = 0 To vmax
        r = cm(i)
        res = res + (bv(r) + ev(r)) * yv(i)
    Next i
    
    Objective = res
End Function

' Lookup constraint shadow cost

Public Function Shadow(constraint As Long) As Double
    Dim r As Long
    
    r = vm(constraint)
    
    If r > vmax Then
        Shadow = 0#
    Else
        Shadow = yv(r)
    End If
End Function

' Constraint in basis?

Public Function InBasis(constraint As Long) As Boolean

    InBasis = vm(constraint) <= vmax
End Function

' Look up constraint slack

Public Function Slack(constraint As Long) As Double
    Dim r As Long
    
    r = vm(constraint)
    
    If r > vmax Then
        If sk(constraint) Then
            CalcSlack constraint
        End If
        Slack = dv(constraint)
    Else
        Slack = 0#
    End If
End Function

' 1/Sensitivity of infringed constraint to relaxation of c

Public Function ISENS(c As Long) As Double
    If vm(c) > vmax Then
        ISENS = 0#
    Else
        ISENS = ssv(vm(c))
    End If
End Function

' Calculate slack of primal constraint represented at Atm row r
' returns slack, stores in dv(r), returns dual variable sensitivities in sv()

Private Function CalcSlack(r As Long) As Double
    Dim res As Double, i As Long, j As Long
        
    res = bv(r) + ev(r)             ' capacity of constraint
    
    atm.RowToVec r, sv
    bmsolve.Solve sv, sv        ' sensistivity of variables to constraint
    
    For i = 0 To vmax
        j = cm(i)
        res = res - (bv(j) + ev(j)) * sv(i) 'effect objective
    Next i
    dv(r) = res
    CalcSlack = res
End Function


' Find most negative dual variable
' Returns -1 if all positive

Private Function MostNegativeVar() As Long
    Dim i As Long, r As Long
    Dim m As Double
    
    r = -1
    m = -lpEpsilon
    
    For i = 0 To vmax
        If sk(cm(i)) Then
            Debug.Print "Skipped constraint in basis"
        End If
        If yv(i) < m Then
            m = yv(i)
            r = i
        End If
    Next i
    
    MostNegativeVar = r
End Function

' Check all dual vars positive

Private Function Negprices() As Boolean
    Dim i As Long
    
    For i = 0 To vmax
        If yv(i) < -lpEpsilon Then
            Negprices = True
            Exit Function
        End If
    Next i
    Negprices = False
End Function

' Find best replacement constraint for negative variable v
' Returns -1 if none available

Private Function BestReplacementVar(v As Long) As Long
    Dim i As Long, m As Long, r As Long
    Dim s As Double
    Dim ms As Double
    
    m = -1
    For i = vmax + 1 To cmax
        r = cm(i)
        If Not sk(r) Then
            s = CalcSlack(r)            ' Calc slack and sensitivities
            If sv(v) < -lpEpsilon Then    ' Sensitivity to v must be negative
                s = -s * sv(v)          ' Calc rate that v replacement will improve objective
                If m < 0 Or s > ms Then
                    ms = s
                    m = i
                End If
            End If
        End If
    Next i

    BestReplacementVar = m
End Function

' Find most negative slack

Private Function MostInfringed() As Long
    Dim i As Long, j As Long, m As Long, r As Long
    Dim s As Double, ms As Double
    
    m = -1
    ms = -lpEpsilon
    For i = vmax + 1 To cmax
        j = i + startsearch
        If j > cmax Then
            j = j - cmax + vmax
        End If
        r = cm(j)
        If Not sk(r) Then
            s = CalcSlack(r)
            If s < -lpEpsilon Then
                fastsearch = fastsearch - 1
            End If
            If s < ms Then
                ms = s
                m = j
                ssv = sv
            End If
            If fastsearch = 0 Then
                MostInfringed = m
                startsearch = m - vmax
                Exit Function
            End If
        End If
    Next i
    
    MostInfringed = m
End Function

' Find most constraining basis var
' Uses saved sensitivities in ssv()

Private Function FindExitVar() As Long
    Dim i As Long, m As Long, r As Long
    Dim f As Double, mf As Double
            
    m = -1
    
    For i = 0 To vmax
        If eqc(cm(i)) Then
'            Debug.Print "Here"    ' skip equality constraint
        ElseIf ssv(i) > lpEpsilon Then
            f = yv(i) / ssv(i)
            
            If m < 0 Or f < mf Then
                m = i
                mf = f
            End If
        End If
    Next i
   
   FindExitVar = m
End Function

' Solve the linear program starting with current column order

Public Function SolveLP(ByRef Return2 As Long) As Long
    Dim ev As Long, xv As Long, i As Long
    Dim iter As Long, t As Long
    Dim res As Long
    Dim chk As Double, bchk As Double
    
    On Error GoTo errorhandler
    
    startsearch = 0
    fastsearch = FASTCOUNT

    res = lpIters
    
    If Not basisvalid Then
        MakeBasis
    End If
    
    Do
        bmsolve.Solve cv, yv    ' Calc dual variables
        
        xv = MostNegativeVar()
        
        If xv > -1 Then
            ev = BestReplacementVar(xv)
            
#If debugmsg > 1 Then
                Debug.Print "LP"; Id; "Variable("; vn(xv); ")= "; Round(yv(xv), 2);
#End If
            If ev = -1 Then
                Debug.Print " fail: basis variable "; vn(xv); " negative"
                Return2 = xv
                SolveLP = lpUnbounded
                Exit Function
            End If
            
        Else
            ev = MostInfringed()
            
            If ev <> -1 Then
                xv = FindExitVar()
                
#If debugmsg > 1 Then
                Debug.Print "LP"; Id; "Constraint("; cn(cm(ev)); ")= "; Round(dv(cm(ev)), 1);
#End If
                If xv = -1 Then
                    Debug.Print " fail: unresolvable infringed constraint "; cn(cm(ev))
                    Return2 = cm(ev)
                    SolveLP = lpInfeasible
                    Exit Function
                End If
            Else
#If debugmsg > 1 Then
                Debug.Print "Solved"; iter; " iterations"
#End If
                SolveLP = lpOptimum
                Exit Function
            End If
        End If
        
        'xv is the most restrictive (or negative) basis row
        'ev is the variable (primal constraint) to enter the basis
#If debugmsg > 1 Then
        Debug.Print " Enter constraint "; cn(cm(ev)); " for "; cn(cm(xv))
#End If
        
        UpdateBasis xv, cm(ev)
        dv(cm(ev)) = 0#
        EnterBasis xv, cm(ev)
        basisvalid = True

                
        fastsearch = FASTCOUNT
        iter = iter + 1
        
'        If iter Mod InvIters = 0 Then
'            invbm.Check2 lpEpsilon
'        End If
    Loop Until iter > maxiters
    
    Debug.Print "LP"; Id; " fail: maximum iterations exceeded"
    Return2 = cm(ev)
    SolveLP = lpIters
    Exit Function
    
errorhandler:
    Select Case Err
        
        Case vbObjectError + 600, 1004:
            Debug.Print "LP"; Id; " fail due to zero pivot"
            PrintBasis
            SolveLP = lpZeroPivot
            Exit Function
            
        Case Else:
            Debug.Print "LP"; Id; " Error # " & Err & " : " & Error(Err)
'            MsgBox "Error # " & Err & " : " & Error(Err)
            SolveLP = lpUnknown
            Exit Function
    End Select
End Function

Private Sub PrintBasis()
    Dim i As Long
    
    For i = 0 To vmax
        Debug.Print cn(cm(i)); Tab;
        If (i + 1) Mod 8 = 0 Then
            Debug.Print
        End If
    Next i
End Sub



' Test a simple LP problem
' 3 producers with max output and match demand constraints
'

Public Function Test1() As Boolean
    Dim amat As New SparseMatrix
    Dim bv(6) As Double, cv(2) As Double
    Dim cn(6) As String, vn(2) As String
    Dim rc1 As Long, rc2 As Long, expected As Double
    
    On Error GoTo errorhandler
    
    With amat
        .Init 6, 2
        cn(0) = "G1z"
        .Cell(0, 0) = 1#    'zero constraint
        cn(1) = "G1m"
        .Cell(1, 0) = -1#   'capacity constraint
        
        cn(2) = "G2z"
        .Cell(2, 1) = 1#
        cn(3) = "G2m"
        .Cell(3, 1) = -1#
        
        cn(4) = "G3z"
        .Cell(4, 2) = 1#
        cn(5) = "G3m"
        .Cell(5, 2) = -1#
        
        cn(6) = "Dem"
        .Cell(6, 0) = 1#    'demand constraint
        .Cell(6, 1) = 1#
        .Cell(6, 2) = 1#
    End With
    vn(0) = "G1"
    bv(1) = 200#    'cap
    cv(0) = 10#     'cost
    
    vn(1) = "G2"
    bv(3) = 300#
    cv(1) = 20#
    
    vn(2) = "G3"
    bv(5) = 400#
    cv(2) = 30#
    
    bv(6) = -600#   ' demand
    
    Init amat, bv, cv, vn, cn
    
    'place zero constraints in basis
    EnterBasis 0, 0
    EnterBasis 1, 2
    EnterBasis 2, 4
    
    expected = bv(1) * cv(0) + bv(3) * cv(1) + cv(2) * (-bv(6) - bv(1) - bv(3))
        
    rc1 = SolveLP(rc2)
    
    Test1 = (rc1 = lpOptimum) And (Objective = -expected)
    Exit Function
    
errorhandler:
    Test1 = False
End Function



Private Sub debugstart()
    Dim i As Long, s As String
    
    For i = 0 To vmax
        If sk(cm(i)) Then
            s = "!"
        Else
            s = ""
        End If
        Debug.Print vn(i); "  "; cn(cm(i)); s,
        If (i + 1) Mod 2 = 0 Then
            Debug.Print
        End If
    Next i
End Sub

