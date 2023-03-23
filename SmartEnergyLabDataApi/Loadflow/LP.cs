/*
' Linear Programme Object V2
'
' L A Dale 24 Jan 2017
'
' 1 Jun 2018 Support for multisegment variables
' 4 Feb 2018 Updated use of MO object
' 5 Feb 2018 Uses EnterBasis to swap constraints, read-only cmap remains for debug purposes
' Dec 2020 Removed multisegment variables, added constraint names

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
Private vn() As String      'Variable name (dual cost)
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
Private btm As SparseMatrix
Private invbm As SparseInverse
Private vmax As Long      'number of variables-1 (column upb of Atm)
Private cmax As Long      'number of constraints-1 (row upb of Atm)
Private cm() As Long      'Ordering of rows (constraints) in atm, bv, etc)
Private vm() As Long      'map of constraints to basis variables
Private fastsearch As Long   'enable greedy column selection while >0


Public Sub Init(amat As SparseMatrix, bvec() As Double, cvec() As Double, vname() As String, cname() As String)
    Dim mv As LPMSV
    Dim i As Long
    
    Set atm = amat
    bv = bvec
    cv = cvec
    vn = vname
    cn = cname
    vmax = atm.Cupb
    cmax = atm.Rupb
        
    ReDim sk(cmax) As Boolean
    ReDim eqc(cmax) As Boolean
    ReDim dv(cmax) As Double
    ReDim ev(cmax) As Double
    ReDim yv(vmax) As Double
    ReDim sv(vmax) As Double
    ReDim cm(cmax) As Long
    ReDim vm(cmax) As Long
    
    Set btm = New SparseMatrix
    Set invbm = New SparseInverse

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

    
Private Sub MakeInvBasis()
    Dim i As Long, c As Long, tsz As Long
    
    For i = 0 To vmax                   ' Calc size of basis transpose
        c = cm(i)
        tsz = tsz + atm.Rsize(c)
    Next i
    btm.Init vmax, vmax, tsz / (vmax + 1)
    For i = 0 To vmax
        btm.ReplaceRow i, atm, cm(i)    ' Build basis transpose
    Next i
    
    invbm.Init btm
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
    
    invbm.MultMat atm, r, sv        ' sensistivity of variables to constraint
    
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
        MakeInvBasis
    Else
        invbm.Check2 lpEpsilon
    End If
    
    Do
        invbm.MultVec cv, yv    ' Calc dual variables
        
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
        
        invbm.Update xv, atm, cm(ev)
        dv(cm(ev)) = 0#
        EnterBasis xv, cm(ev)
        basisvalid = True

                
        fastsearch = FASTCOUNT
        iter = iter + 1
        
        If iter Mod InvIters = 0 Then
            invbm.Check2 lpEpsilon
        End If
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
*/
using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.Loadflow
{
    // Linear Programme Object V2
    //
    // L A Dale 24 Jan 2017
    //
    // 1 Jun 2018 Support for multisegment variables
    // 4 Feb 2018 Updated use of MO object
    // 5 Feb 2018 Uses EnterBasis to swap constraints, read-only cmap remains for debug purposes
    // Dec 2020 Removed multisegment variables, added constraint names

    // This code solves an LP which in standard primal form is minimise Ct.X s.t. A.X <= B & X >= 0
    // where Ct is the cost of each variable X and A.X <= B represents the constraints on X
    // The code actually solves the dual form maximise Bt.Y such that At.Y = C & Y >= 0
    // Y (and At) will need to be augmented by slack variables Ys to achieve the equalities
    // The value of the primal variables X at the optimum can be derived from D (shadows of Y)
    // Primal equality constraints can be flagged using EQ() as bool (means constraint not removed from basis but does not force initialisation into basis)
    // Primal constraints can be skipped using Skip()as bool
    public class LP {
        private const int FASTCOUNT = -2; // the number of constraint candidates to be considered for basis entry.  Make -ve to consider all constraints
        //
        public int Id; // used to identify LP instance
        
        // Inputs
        private SparseMatrix atm; // Constraint matrix
        private double[] bv; // Primal constraint magnitudes
        private double[] ev; // Primal constraint magnitude modifiers
        private double[] cv; // Variable costs
        private bool[] eqc; // Primal constriant is equality
        private bool[] sk; // Sip this constraint
        private string[] vn; // Variable name dual cost
        private string[] cn; // Constraint name

        // Outputs
        private double[] dv; // Slack of contraints
        private double[] yv; // Dual variables
        
        // Internals
        private int maxiters;
        private int startsearch;
        private bool basisvalid;

        private double[] sv; // Sensitivity of dual variables to cost perturbations
        private double[] ssv; // Save of sv for the most infringed constraint
        private SparseMatrix btm;
        private SparseInverse invbm;
        private int vmax; // number of variables-1 (column upb of Atm)
        private int cmax; // number of constraints-1 (row upb of Atm)
        private int[] cm; // Ordering of rows (constraints) in atm, bv, etc)
        private int[] vm; // map of constraints to basis variables
        private int fastsearch; // enable greedy column selection while >0

        public LP() {

        }
        
        public LP(SparseMatrix amat, double[] bvec, double[] cvec, string[] vname, string[] cname) {
            Init(amat, bvec, cvec, vname, cname);
        }

        public void Init(SparseMatrix amat, double[] bvec, double[] cvec, string[] vname, string[] cname) {
            atm = amat;
            bv = Utilities.CopyArray(bvec);
            cv = Utilities.CopyArray(cvec);
            vn = Utilities.CopyArray(vname);
            cn = Utilities.CopyArray(cname);
            vmax = atm.Cupb;
            cmax = atm.Rupb;
            sk = new bool[cmax+1];
            eqc = new bool[cmax+1];
            dv = new double[cmax+1];
            ev = new double[cmax+1];
            yv = new double[vmax+1];
            sv = new double[vmax+1];
            cm = new int[cmax+1];
            vm = new int[cmax+1];

            btm = new SparseMatrix();
            invbm = new SparseInverse();

            maxiters = (vmax+1) * 2;

            InitCOrder();
        }

        public void InitCOrder() {
            int i;
            for(i=0;i<=cmax;i++) {
                cm[i] = i;
                vm[i] = i;
            }
            basisvalid = false;
        }

        public void SaveCOrder(ref int[] corder) {
            corder = Utilities.CopyArray(cm);
        }

        public void RestoreCOrder(int[] corder) {
            int i;
            Array.Copy(corder,cm,cm.Length);
            for( i=0;i<=cmax;i++) {
                vm[cm[i]] = i;
            }
            basisvalid = false;
        }

        private void MakeInvBasis() {
            int i, c, tsz=0;
            int rsize;
            for(i=0;i<=vmax;i++) { // Calc size of basis transpose
                c = cm[i];
                rsize = atm.GetRsize(c);
                tsz = tsz + rsize;
                //??Console.WriteLine($"i={i},c={c},rsize={rsize},tsz={tsz}");
            }
            btm.Init(vmax, vmax, ((double) tsz) / (double) (vmax+1));
            for( i=0;i<=vmax;i++) {
                btm.ReplaceRow(i,atm, cm[i]); // Build basis transpose
            }

            invbm.Init(btm);
            basisvalid = true;
        }

        // Transposed constraint matrix
        public SparseMatrix TConsMat {
            get {
                return atm;
            }
        }

        // Mark inverse as invalid
        public void MatAltered() {
            basisvalid = false;
        }

        // Equality flag
        public void SetEquality(int constraint, bool flag) {
            eqc[constraint] = flag;
        }
        public bool GetEquality(int constraint) {
            return eqc[constraint];
        }

        // Skip flag
        public void SetSkip(int constraint, bool flag) {
            sk[constraint] = flag;
        }
        public bool GetSkip(int constraint) {
            return sk[constraint];
        }

        // Column map
        public int GetCmap(int column) {
            return cm[column];
        }

        public void SetCname(int constraint, string name) {
            cn[constraint] = name;
        }

        public string GetCname(int constraint) {
            return cn[constraint];
        }

        // Set a variable using a constraint
        public void EnterBasis(int var, int cons) {
            int oc, cp;
            if ( cm[var] != cons) {
                oc = cm[var]; // save old contents of var
                cp = vm[cons]; // current position of constraint
                cm[var] = cons;
                cm[cp] = oc;
                //??
                if (cons==119 && var==303) {
                    int xyz=1;
                }
                vm[cons] = var;
                if (oc==119 && cp==303) {
                    int xyz=1;
                }
                vm[oc] = cp;
            }
            basisvalid = false; // MArk basis invalid even if already in basis
        }

        // Costs
        public void SetCvec(int column, double cost) {
            cv[column] = cost;
        }
        public double GetCvec(int column) {
            return cv[column];
        }

        // Magnitudes (NB adjusts original by values)
        public void SetBvec( int column, double magnitude) {
            bv[column] = magnitude;
        }
        public double GetBvec(int column) {
            return bv[column];
        }

        // Magnitude modifiers (NB adjusts original bv values)
        public void SetEvec(int column, double magnitude) {
            ev[column] = magnitude;
        }
        public double GetEvec(int column) {
            return ev[column];
        }

        // Compute objective
        public double Objective() {
            int i, r;
            double res=0;

            for(i=0;i<=vmax;i++) {
                r = cm[i];
                res = res + (bv[r] + ev[r]) * yv[i];
                //??Console.WriteLine($"i={i,6},r={r,6},bv={bv[r],16:f6},ev={ev[r],16:f6},yv={yv[i],16:f6},res={res,16:f6}");
            }
            return res;
        }

        // Lookup constraint shadow cost
        public double Shadow(int constraint) {
            int r;

            r = vm[constraint];
            double result;
            if ( r>vmax) {
                result = 0;
            } else {
                result = yv[r];
            }
            return result;            
        }

        // Constraint in basis
        public bool InBasis(int constraint) {
            return vm[constraint] <= vmax;
        }

        // Lookup constraint slack
        public double Slack(int constraint) {
            int r;

            r = vm[constraint];

            double result;
            if ( r > vmax) {
                if ( sk[constraint]) {
                    CalcSlack(constraint);
                }
                result = dv[constraint];
            } else {
                result = 0;
            }
            return result;
        }

        // 1/Sensitivity of infringed constraint to relaxation of c
        public double ISENS(int c) {
            double result;
            if ( vm[c] > vmax) {
                result =0;
            } else {
                result = ssv[vm[c]];
            }
            return result;
        }

        // Calculate slack of primal constraint represented at Atm row r
        // returns slack, stores in dv(r), returns dual variable sensitivities in sv()
        private double CalcSlack(int r) {
            double res;
            int i,j;

            res = bv[r] + ev[r]; // capacity of constraint


            invbm.MultMat(atm, r, ref sv); // sensistivity of variables to constraint

            for(i=0;i<=vmax;i++) {
                j = cm[i];
                res = res - (bv[j] + ev[j]) * sv[i]; // effect objective
            }
            dv[r] = res;
            
            return res;
        }

        // Find most negative dual variable
        // Returns -1 if all positive
        private int MostNegativeVar() {
            int i, r;
            double m;

            r = -1;
            m= -LPhdr.lpEpsilon;

            for(i=0;i<=vmax;i++) {
                if ( sk[cm[i]]) {
                    Console.WriteLine("Skipped constraint in basis");
                }
                if ( yv[i] < m ) {
                    m = yv[i];
                    r = i;
                }
            }
            return r;
        }

        // Check all dual vars positive
        private bool Negprices() {
            int i;

            for(i=0;i<vmax;i++) {
                if ( yv[i] < -LPhdr.lpEpsilon) {
                    return true;
                }
            }
            return false;
        }

        // Find best replacement constraint for negative variable v
        // Returns -1 if none available
        private int BestReplacementVar( int v) {
            int i, m, r;
            double s, ms=0;

            m = -1;
            for( i=vmax+1;i<=cmax;i++) {
                r = cm[i];
                if ( !sk[r]) {
                    s = CalcSlack(r);                   //  Calc slack and sensitivities
                    if ( sv[v] < -LPhdr.lpEpsilon ) {    // Sensitivity to v must be negative
                        s = -s*sv[v];                   // Calc rate that v replacement will improve objective
                        if ( m<0 || s>ms ) {
                            ms = s;
                            m = i;
                        }
                    }
                }
            }
            return m;
        }

        // Find most negative slack
        private int MostInfringed() {
            int i,j,m,r;
            double s,ms=0;

            m = -1;
            ms = -LPhdr.lpEpsilon;
            for( i=vmax+1;i<=cmax;i++) {
                j = i+startsearch;
                if ( j>cmax) {
                    j = j - cmax + vmax;
                }
                r = cm[j];
                if ( !sk[r] ) {                    
                    s = CalcSlack(r);
                    if ( s < -LPhdr.lpEpsilon) {
                        fastsearch = fastsearch - 1;
                    }
                    if ( s < ms ) {
                        ms = s;
                        m = j;
                        ssv = Utilities.CopyArray(sv);
                    }
                    if ( fastsearch == 0 ) {
                        startsearch = m - vmax;
                        return m;
                    }
                }
            }
            return m;
        }

        // Find most constraining basis var
        // Uses saved sensitivities in ssv()
        private int FindExitVar() {
            int i, m, r;
            double f, mf=0;

            m = -1;

            for (i=0;i<=vmax;i++) {
                if ( eqc[cm[i]]) {

                } else if ( ssv[i] > LPhdr.lpEpsilon) {
                    f = yv[i] / ssv[i];

                    if ( m<0 || f<mf) {
                        m = i;
                        mf = f;
                    }
                }
            }
            return m;
        }

        // Solve the linear program starting with current column order
        public int SolveLP(ref int Return2) {
            int ev=0, xv, i;
            int iter=0, t;
            int res;
            double chk, bchk;
            
            try {
                startsearch =0;
                fastsearch = FASTCOUNT;
                res = LPhdr.lpIters;
                if ( !basisvalid) {
                    MakeInvBasis();
                } else {
                    invbm.Check2(LPhdr.lpEpsilon);
                }

                do {
                    invbm.MultVec(cv, ref yv); // Calc dual variables

                    xv = MostNegativeVar();

                    if ( xv>-1 ) {
                        ev = BestReplacementVar(xv);

                        if ( ev==-1) {
                            Console.WriteLine($" fail: basis variable {vn[xv]} negative");
                            Return2 = xv;
                            return LPhdr.lpUnbounded;
                        } 
                    } else {

                        ev = MostInfringed();

                        if ( ev!=-1) {
                            xv = FindExitVar();

                            if ( xv==-1 ) {
                                Console.WriteLine($" fail: unsolvable infringed constraint {cn[cm[ev]]}");
                                Return2 = cm[ev];
                                return LPhdr.lpInfeasible;
                            }
                        } else {
                            #if DEBUG
                                PrintFile.PrintVars($"Solved {iter} iterations");
                            #endif
                            return LPhdr.lpOptimum;
                        }
                    }

                    invbm.Update(xv, atm, cm[ev]);


                    dv[cm[ev]] = 0;
                    EnterBasis(xv, cm[ev]);

                    basisvalid = true;

                    fastsearch = FASTCOUNT;
                    iter = iter + 1;

                    if ( iter % LPhdr.InvIters == 0 ) {
                        invbm.Check2(LPhdr.lpEpsilon);
                    }

                } while( iter<=maxiters);

                Console.WriteLine($"LP {Id} fail: maximum iterations exceeded");
                Return2 = cm[ev];
                return LPhdr.lpIters;
            } catch(ZeroPivotException) {
                Console.WriteLine($"LP {Id} fail due to zero pivot");
                PrintBasis();
                return LPhdr.lpZeroPivot;
            } catch(Exception e) {
                Console.WriteLine($"LP {Id} Error {e.Message}");
                return LPhdr.lpUnknown;
            }
        }

        private void printDiag(string prepend) {
            if ( vm.Length >119 && vm[119]==303) {
                Console.WriteLine($"{prepend} vm[119]={vm[119]}");
            }
        }

        private void PrintBasis() {
            int i;

            for(i=0;i<=vmax;i++) {
                Console.Write($"{cn[i]}\t");
                if ( (i+1) % 8 ==0 ) {
                    Console.WriteLine();
                }
            }
        }


        // Test a simple LP problem
        // 3 producers with max output and match demand constraints
        public bool Test1() {
            SparseMatrix amat = new SparseMatrix();
            double[] bv = new double[7], cv = new double[7];
            string[] cn = new string[7], vn = new string[7];
            int rc1, rc2=0;
            double expected;

            amat.Init(6,2);
            cn[0] = "G1z";
            amat.SetCell(0,0,1); // Zero constraint
            cn[1] = "G1m";
            amat.SetCell(1,0,-1); // Capacity constraint

            cn[2] = "G2z";
            amat.SetCell(2,1,1);
            cn[3] = "G2m";
            amat.SetCell(3,1,-1);

            cn[4] = "G3z";
            amat.SetCell(4,2,1);
            cn[5] = "G3m";
            amat.SetCell(5,2,-1);

            cn[6] = "Dem";
            amat.SetCell(6,0,1); // Demand constraint
            amat.SetCell(6,1,1);
            amat.SetCell(6,2,1);

            vn[0] = "G1";
            bv[1] = 200;    // cap
            cv[0] = 10;     // cost

            vn[1] = "G2";
            bv[3] = 300;
            cv[1] = 20;

            vn[2] = "G3";
            bv[5] = 400;
            cv[2] = 30;

            bv[6] = -600; // demand

            Init(amat, bv, cv, vn, cn);

            // place zero constraints in basis
            EnterBasis(0,0);
            EnterBasis(1,2);
            EnterBasis(2,4);

            expected = bv[1]*cv[0] + bv[3]*cv[1] + cv[2] * ( -bv[6] - bv[1] - bv[3]);

            rc1 = SolveLP(ref rc2);
            bool result = (rc1 == LPhdr.lpOptimum) && (Objective() == -expected);
            return result;
        }

        public void PrintState() {
            int i;
            PrintFile.PrintVars("atm");
            atm.PrintState();
            // btm
            PrintFile.PrintVars("btm");
            btm.PrintState();
            // invbm
            PrintFile.PrintVars("invbm");
            invbm.PrintState();
                
            for(i=0;i<=cmax;i++) {
                PrintFile.PrintVars("i", i, "sk", sk[i], "eqc", eqc[i], "dv", dv[i], "ev", ev[i], "cm", cm[i], "vm", vm[i], "bv", bv[i], "cn", cn[i]);
            }

            for(i=0;i<=vmax;i++) {
                PrintFile.PrintVars("i", i, "yv", yv[i], "sv", sv[i], "cv", cv[i], "vn", vn[i]);
            }

        }

    }
}