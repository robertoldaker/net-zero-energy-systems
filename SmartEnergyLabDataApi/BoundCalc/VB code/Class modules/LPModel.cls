VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "LPModel"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
' Describes a part (or all) of a LP model
'
' Permits definition of (named) Variables and Constraints
' for an LP which maximises Ct.X Such that A.X <= B and X>0
' To minimise Ct.X make C -ve
'
' Usage:
'   Call Init to start definitions
'   Call DefineVariable and DefineConstraint to describe the model
'
' L Dale 22 Feb 2017
' Revised to use collections and improve include functionality
' 17 Nov 2023 Removed multisegment variables
' 1 Dec 2023 Incorporated references to X>=0 and X <= Max constraints in variable definitions. Added paired variable to represent negative values.
' Types of constraints extended to <=, >= and =

Option Explicit
Option Base 0
Private LPVariables As New Collection    ' Variable definitions
Private LPConstraints As New Collection  ' Constraint definitions
Private errors As Long

Public Function VarCollection() As Collection
    Set VarCollection = LPVariables
End Function

Public Function ConsCollection() As Collection
    Set ConsCollection = LPConstraints
End Function

Public Function Exists(key As String, col As Collection) As Boolean
    Dim obj As Object
    
    On Error GoTo errorhandler
    Set obj = col.item(key)
    Exists = True
    Exit Function
    
errorhandler:
    Exists = False
End Function

Public Function VarExists(name As String) As Boolean
    VarExists = Exists(name, LPVariables)
End Function

Public Function ConsExists(name As String) As Boolean
    ConsExists = Exists(name, LPConstraints)
End Function

'
' Define lp variable, and optionally: name of initially determining constraint, non-zero welfare and maximum value
' NB all vsrisbles will have a zero constraint (varname plus suffix 'zc') and this will determine the initial value of the variable
' If maxvalue (>=0) is given a new maxvalue constraint is created (varname plus suffix 'mc')
' To minimise Ct.X make welfare -ve

Public Function VarDef(varname As String, Optional InitCons As String = "Unset", Optional Welfare As Double = 0#, Optional MaxValue As Double = -1#) As LPVarDef
    
    If VarExists(varname) Then
        MsgBox "Repeated name: Variable " & varname
        errors = errors + 1
    End If
    
    Set VarDef = New LPVarDef
    LPVariables.Add VarDef, varname
    
    With VarDef
        .name = varname
        .Welfare = Welfare
        
        Set .Vzc = ConsDef(varname & "zc", CTGTE, 0#, Array(varname, 1#)) ' create zero constraint
        
        If InitCons = "Unset" Then ' set variable to zero by default
            .InitC = varname & "zc"
        End If
        
        If Not MaxValue < 0 Then
            Set .Vmc = ConsDef(varname & "mc", CTLTE, MaxValue, Array(varname, 1#))
        End If
    End With
End Function


' Define lp variable positive & negative pair.  Initially determining constraints for positive and negative variables are zero constraints.
' , non-zero cost, maximum value, presence of negative range variable and its min value
' NB all vsrisbles will have a zero constraint (varname plus suffix 'zc') and this will determine the initial value of the variable unless another constraint is named
' If maxvalue (>=0) is given a new maxvalue constraint is created (varname plus suffix 'mc')
' The 'neg' flag signals the variable should have a companion for negative range (varname plus suffix 'n') with corresponding zero constraint
' If minvalue (<0) is given a new minvalue constraint is created for the (varname plus suffix 'nmc')
' To minimise Ct.X make costs -ve

Public Function PairDef(varname As String, Optional Pwelfare As Double = 0#, Optional Nwelfare As Double = 0#, _
                Optional MaxValue As Double = -1#, Optional MinValue As Double = 1#) As LPVarDef
    
    If VarExists(varname) Then
        MsgBox "Repeated name: Variable " & varname
        errors = errors + 1
    End If
    
    Set PairDef = New LPVarDef
    LPVariables.Add PairDef, varname
    
    With PairDef
        .name = varname
        
        Set .Vpv = VarDef(varname & "p", Welfare:=Pwelfare, MaxValue:=MaxValue)
        
        Set .Vnv = VarDef(varname & "n", Welfare:=Nwelfare, MaxValue:=-MinValue)
    End With
        
End Function


'
' Define constraint of form pairs (variable * constant) <=/>=/= magnitude
'
Public Function ConsDef(name As String, CType As Long, Magnitude As Double, Pairs As Variant) As LPConsDef
    Dim i As Long
    Dim def As LPConsDef
    
    If ConsExists(name) Then
        MsgBox "Repeated name: Constraint " & name
        errors = errors + 1
    End If
       
    Set def = New LPConsDef
    With def
        .name = name
'        .Id = numc
        .CType = CType
        .Magnitude = Magnitude
        .Pairs = Pairs
    End With
    LPConstraints.Add def, name
    Set ConsDef = def
End Function

'
' Include a model in this model
' Update original variable and constraint definitions
'
Public Sub Include(model As LPModel)
    Dim vdef As LPVarDef
    Dim cdef As LPConsDef
    
    For Each vdef In model.VarCollection
        LPVariables.Add vdef, vdef.name
    Next vdef
    
    For Each cdef In model.ConsCollection
        LPConstraints.Add cdef, cdef.name
    Next cdef
End Sub

'
' Make LP from model
' NB Initial LP ordering of constraints matches model order
'
Public Function MakeLP() As LP
    Dim numv As Long
    Dim numc As Long
    Dim amat As SparseMatrix
    Dim bvec() As Double
    Dim cvec() As Double
    Dim vn() As String, vname As String
    Dim cn() As String, cname As String
    Dim vdef As LPVarDef
    Dim cdef As LPConsDef
    Dim Pairs() As Variant
    Dim eqcvec() As Boolean
    Dim sn As Double
    Dim i As Long, j As Long, cid As Long
    Dim rlp As LP

    numc = LPConstraints.Count
    For Each vdef In LPVariables
        If vdef.Vpv Is Nothing Then ' not a pair
            numv = numv + 1
        End If
    Next vdef
    
    Set amat = New SparseMatrix
    amat.Init numc - 1, numv - 1
    ReDim bvec(numc - 1)
    ReDim eqcvec(numc - 1)
    ReDim cn(numc - 1)
    ReDim vn(numv - 1)
    ReDim cvec(numv - 1)
    
    ' Process variable definitions
    i = 0
    For Each vdef In LPVariables
        With vdef
            If .Vpv Is Nothing Then ' not a pair
                .Id = i
                cvec(i) = .Welfare
                vn(i) = .name
                i = i + 1
            End If
        End With
    Next vdef

    ' Process constraint definitions
    i = 0
    For Each cdef In LPConstraints
        With cdef
            .Id = i
            cn(i) = .name
            If .CType = CTLTE Then
                sn = 1#
            Else
                sn = -1#   ' change sign of pair coefs and magnituge on = and >= constraints
            End If
            bvec(i) = .Magnitude * sn
            eqcvec(i) = (.CType = CTEQ)
            Pairs = .Pairs
        End With
        
'        If UBound(Pairs) < 1 Then
'            MsgBox "LP Build: No variables referenced by constraint " & cn(i)
'            errors = errors + 1
'        End If
        For j = 0 To UBound(Pairs) Step 2
            vname = Pairs(j)
            If Not VarExists(vname) Then
                MsgBox "LP Build: Unknown variable " & vname & " in constraint " & cn(i)
                errors = errors + 1
            End If
            Set vdef = LPVariables.item(vname)
            
            If vdef.Vpv Is Nothing Then ' not a pair
                amat.Cell(i, vdef.Id) = Pairs(j + 1) * sn
            Else
                amat.Cell(i, vdef.Vpv.Id) = Pairs(j + 1) * sn
                amat.Cell(i, vdef.Vnv.Id) = -Pairs(j + 1) * sn
            End If
        Next j
        i = i + 1
    Next cdef
    
    Set rlp = New LP
    rlp.Init amat, bvec, cvec, vn, cn
    
    'set the initial basis using initialisation info from model
    For Each vdef In LPVariables
        If vdef.Vpv Is Nothing Then
            cname = vdef.InitC
            If Not ConsExists(cname) Then
                MsgBox "LP build: Unknown constraint " & cname & " initialising variable " & vn(i)
                errors = errors + 1
            End If
            cid = ConsId(cname)
            rlp.EnterBasis vdef.Id, cid
            If eqcvec(cid) Then
                rlp.Equality(cid) = True
            End If
        End If
    Next vdef
    
    If errors > 0 Then
        Set MakeLP = Nothing
        MsgBox "Too many errors to complete LPModel build"
    End If
    
    Set MakeLP = rlp
End Function

' Provide the id of the named constraint

Public Function ConsId(name As String) As Long
    Dim cdef As LPConsDef
    
    Set cdef = LPConstraints.item(name)
    ConsId = cdef.Id
End Function

' Provide the id of named variable

Public Function VarId(name As String) As Long
    Dim vdef As LPVarDef
    
    Set vdef = LPVariables.item(name)
    VarId = vdef.Id
End Function


Public Function Test() As Boolean
    ' Model definitions stored to aid debugging
    Dim dcdef As LPConsDef
    Dim gvdef(2) As LPVarDef    ' generation variables
    
'    Dim mmo As New MO
    Dim mlp As LP, rc1 As Long, rc2 As Long
    Dim expected As Double, res As Double

    ' Create 3 gen variables with min and max constraints (-ve costs to minimise)
    Set gvdef(0) = VarDef("g0", Welfare:=-10#, MaxValue:=200#)
    Set gvdef(1) = VarDef("g1", Welfare:=-20#, MaxValue:=300#)
    Set gvdef(2) = VarDef("g2", Welfare:=-30#, MaxValue:=400#)
    
    Set dcdef = ConsDef("demc", CTEQ, 550#, Array("g0", 1#, "g1", 1#, "g2", 1#))
    
    Set mlp = Me.MakeLP()
    rc1 = mlp.SolveLP(rc2)
    
    Debug.Print "G1"; gvdef(0).Value(mlp)
    Debug.Print "G2"; gvdef(1).Value(mlp)
    Debug.Print "G3"; gvdef(2).Value(mlp)
    Debug.Print "Price"; dcdef.Shadow(mlp)
    
    expected = -10# * 200# - 20# * 300# - (550# - 200# - 300#) * 30#
    res = mlp.Objective
    Test = (expected = res)
End Function
