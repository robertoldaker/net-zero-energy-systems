VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Control"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' A network flow control

Option Explicit
Implements IData

Public name As String           ' branch code
Public longname As String       ' branch name
Public Index As Long
Public CBranch As Branch
Public CType As String
Public MinCtrl As Double
Public MaxCtrl As Double
Public Injmax As Double
Public Cost As Double
Public ISetPt As Double         ' Initial/manual SetPoint
Public CtVar As LPVarDef

Public Property Get IData_Name() As String
    IData_Name = name
End Property

Public Function Idata_FieldMap(mtable As DTable, fields() As Long) As Boolean
    Idata_FieldMap = mtable.FieldMap(fields, Array("Node1", "Node2", "Code", "Type", "MinCtrl", "MaxCtrl", "Cost", "ISetPt"))
End Function

Public Function IData_Init(Data() As Variant, row As Long, fields() As Long) As Boolean
    Dim node1 As String
    Dim node2 As String

    IData_Init = True
    
    node1 = Data(row, fields(0))
    node2 = Data(row, fields(1))
    name = Data(row, fields(2))
    
    longname = BranchName(node1, node2, name)
    Index = controls.Count + 1
    CType = Data(row, fields(3))
    MinCtrl = Data(row, fields(4))
    MaxCtrl = Data(row, fields(5))
    Cost = Data(row, fields(6))
    ISetPt = Data(row, fields(7))
    
    If Exists(longname, branches) Then
        Set CBranch = branches.item(longname)
    Else
        MsgBox "Control " & longname & " not a defined branch"
        IData_Init = False
    End If
    
    Set CBranch.BCtrl = Me
    
    Select Case CType
        Case "QB"
            Injmax = PUCONV * MaxCtrl / CBranch.Xval
        
        Case "HVDC"
            Injmax = MaxCtrl
                
        Case Else
             MsgBox "Unknown control type " & CType & " found at " & name
             IData_Init = False
    End Select
End Function

' Calculate or provide setpoint value

Public Function SetPoint(spt As Long) As Double
    Dim v As Double
    
    v = MaxCtrl / Injmax
    
    Select Case spt
        Case SPAuto
            SetPoint = v * CtVar.Value(ctrllp)
            
        Case SPMan
            SetPoint = ISetPt
            
        Case SPZero
            SetPoint = 0#
            
        Case Else
            MsgBox "Unknown setpoint type request"
    End Select
End Function
