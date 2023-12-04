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
'   Where Variables X must be positive and have positive costs
'   And Constraints are of the form A.X <= B
'
' Usage:
'   Call Init to start definitions
'   Call DefineVariable and DefineConstraint to describe the model
'
' L Dale 22 Feb 2017
' Revised to use collections and improve include functionality
' 17 Nov 2023 Removed multisegment variables

Option Explicit
Option Base 0
Private LPVariables As New Collection    ' Variable definitions
Private LPConstraints As New Collection  ' Constraint definitions
  

Public Function VarCollection() As Collection
    Set VarCollection = LPVariables
End Function

Public Function ConsCollection() As Collection
    Set ConsCollection = LPConstraints
End Function

Public Function Exists(key As String, col As Collection) As Boolean
    Dim obj As Object
    
    On Error GoTo errorhandler
    Set obj = col.Item(key)
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
' Define variable, and its cost
'
Public Function VarDef(name As String, InitCons As String, Optional cost As Double = 0#) As LPVarDef
    Dim def As LPVarDef
    
    If VarExists(name) Then
        MsgBox "Repeated name: Variable " & name
    End If
    
    Set def = New LPVarDef
    With def
        .name = name
'        .Id = numv
        .cost = cost
        .InitC = InitCons
    End With
    
    LPVariables.Add def, name
    Set VarDef = def
End Function


'
' Define constraint of form magnitude > (variable * constant) ...
'
Public Function ConsDef(name As String, Equality As Boolean, Magnitude As Double, Pairs As Variant) As LPConsDef
    Dim i As Long
    Dim def As LPConsDef
    
    If ConsExists(name) Then
        MsgBox "Repeated name: Constraint " & name
    End If
       
    Set def = New LPConsDef
    With def
        .name = name
'        .Id = numc
        .Equality = Equality
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
'    Dim mvs As Collection, mv As LPMSV
    Dim vdef As LPVarDef
    Dim cdef As LPConsDef
    Dim Pairs() As Variant
    Dim eqcvec() As Boolean, eqc As Boolean
    Dim i As Long, j As Long, cid As Long
    Dim rlp As LP

    numv = LPVariables.Count
    numc = LPConstraints.Count
    
    Set amat = New SparseMatrix
    amat.Init numc - 1, numv - 1
    ReDim bvec(numc - 1)
    ReDim eqcvec(numc - 1)
    ReDim cn(numc - 1)
    ReDim vn(numv - 1)
    ReDim cvec(numv - 1)
'    Set mvs = New Collection

    For i = 0 To numv - 1
        Set vdef = LPVariables.Item(i + 1)
        With vdef
            .Id = i
            cvec(i) = .cost
            vn(i) = .name
'            If Not .MOmgr Is Nothing Then   ' Create mv structure if a multisegment variable
'                Set mv = New LPMSV
'                mv.name = .name
'                Set mv.mmo = New MO
'                .MOmgr.Copy mv.mmo          ' copy in merit order
'                mv.pos = .Ipos              ' initial position
'                mv.vid = i                  ' and var id
'                mvs.Add mv, .name
'            End If
        End With
    Next i

    For i = 0 To numc - 1
        Set cdef = LPConstraints.Item(i + 1)
        With cdef
            .Id = i
            cn(i) = .name
            bvec(i) = .Magnitude
            eqcvec(i) = .Equality
            Pairs = .Pairs
        End With
        
'        If UBound(Pairs) < 1 Then
'            MsgBox "LP Build: No variables referenced by constraint " & cn(i)
'        End If
        For j = 0 To UBound(Pairs) Step 2
            vname = Pairs(j)
            If Not VarExists(vname) Then
                MsgBox "LP Build: Unknown variable " & vname & " in constraint " & cn(i)
            End If
            amat.Cell(i, VarId(vname)) = Pairs(j + 1)
        Next j
    Next i
    
'    For Each mv In mvs
'        Set vdef = LPVariables.Item(vn(mv.vid))
'        If Not ConsExists(vdef.vzc) Then
'            MsgBox "LP build: Unknown constraint " & vdef.vzc & " for multisegment variable " & vn(i)
'        End If
'        If Not ConsExists(vdef.vmc) Then
'            MsgBox "LP build: Unknown constraint " & vdef.vmc & " for multisegment variable " & vn(i)
'        End If
'        If Not ConsExists(vdef.vdc) Then
'            MsgBox "LP build: Unknown constraint " & vdef.vdc & " for multisegment variable " & vn(i)
'        End If
'        mv.dcid = ConsId(vdef.vdc)
'        mv.mcid = ConsId(vdef.vmc)
'        mv.zcid = ConsId(vdef.vzc)
'    Next mv
    
    Set rlp = New LP
    rlp.Init amat, bvec, cvec, vn, cn
    
    'set the initial basis using initialisation info from model
    
    For i = 0 To numv - 1
        Set vdef = LPVariables.Item(vn(i))
        cname = vdef.InitC
        If Not ConsExists(cname) Then
            MsgBox "LP build: Unknown constraint " & cname & " initialising variable " & vn(i)
        End If
        cid = ConsId(cname)
        rlp.EnterBasis vdef.Id, cid
        If eqcvec(i) Then
            rlp.Equality(i) = True
        End If
    Next i

    Set MakeLP = rlp
End Function

' Provide the id of the named constraint

Public Function ConsId(name As String) As Long
    Dim cdef As LPConsDef
    
    Set cdef = LPConstraints.Item(name)
    ConsId = cdef.Id
End Function

' Provide the id of named variable

Public Function VarId(name As String) As Long
    Dim vdef As LPVarDef
    
    Set vdef = LPVariables.Item(name)
    VarId = vdef.Id
End Function


Public Function Test() As Boolean
    ' Model definitions stored to aid debugging
    Dim dcdef As LPConsDef
    Dim gvdef(2) As LPVarDef    ' generation variables
    Dim gzc(2) As LPConsDef     ' generation minimum constraints
    Dim gmc(2) As LPConsDef     ' generation max constraints
'    Dim mmo As New MO
    Dim mlp As LP, rc1 As Long, rc2 As Long
    Dim expected As Double, res As Double
    
'    mmo.Add "G1", 10#, 200#
'    mmo.Add "G2", 20#, 300#
'    mmo.Add "G3", 30#, 400#

    ' Create 3 gen variables with min and max constraints
    Set gvdef(0) = Me.VarDef("g0var", "g0zc", 10#)
    Set gvdef(1) = Me.VarDef("g1var", "g1zc", 20#)
    Set gvdef(2) = Me.VarDef("g2var", "g2zc", 30#)
    Set gzc(0) = Me.ConsDef("g0zc", False, 0#, Array("g0var", 1#))
    Set gzc(1) = Me.ConsDef("g1zc", False, 0#, Array("g1var", 1#))
    Set gzc(2) = Me.ConsDef("g2zc", False, 0#, Array("g2var", 1#))
    Set gmc(0) = Me.ConsDef("g0mc", False, 200#, Array("g0var", -1#))
    Set gmc(1) = Me.ConsDef("g1mc", False, 300#, Array("g1var", -1#))
    Set gmc(2) = Me.ConsDef("g2mc", False, 400#, Array("g2var", -1#))
    
    Set dcdef = Me.ConsDef("demc", False, -550#, Array("g0var", 1#, "g1var", 1#, "g2var", 1#))
'    Set gzc = Me.ConsDef("gzc", False, 0#, Array("gvar", 1#))
'    Set gmc = Me.ConsDef("gmc", False, 999#, Array("gvar", -1#))
    
'    With gvdef
'        Set .MOmgr = mmo
'        .vzc = "gzc"
'        .vmc = "gmc"
'        .vdc = "dem"
'    End With
    
    Set mlp = Me.MakeLP()
    rc1 = mlp.SolveLP(rc2)
    
    Debug.Print "G1"; mlp.Slack(ConsId("g0zc"))
    Debug.Print "G2"; mlp.Slack(ConsId("g1zc"))
    Debug.Print "G3"; mlp.Slack(ConsId("g2zc"))
    
    expected = 10# * 200# + 20# * 300# + (550# - 200# - 300#) * 30#
    res = -mlp.Objective
    Test = (expected = res)
End Function
