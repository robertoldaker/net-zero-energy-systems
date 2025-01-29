VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Branch"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' A network branch

#Const LF = True        ' If true customise for loadflow else customise for short circuit calc

Option Explicit
Implements IData

Public name As String
Public Index As Long
#If LF Then
Public node1 As Node
Public node2 As Node
#Else
Public node1 As NodeSC
Public node2 As NodeSC
#End If
Public pn1 As Long      ' Row/column position of node1 and node 2 in admittance matrix
Public pn2 As Long
Public Rval As Double
Public Xval As Double
Public y As Double      ' admittance in 1MVA base
Public Cap As Double
Public km As Double
Public BType As String
Public BOut As Boolean
#If LF Then
Public BCtrl As Control
#End If

Public Property Get IData_Name() As String
    IData_Name = name
End Property

Public Function Idata_FieldMap(mtable As DTable, map() As Long) As Boolean
    Idata_FieldMap = mtable.FieldMap(map, Array("Region", "Node1", "Node2", "Code", "R", "X", "Cap", "OHL", "Cable", "Link Type"))
End Function

Public Function IData_Init(Data() As Variant, row As Long, map() As Long) As Boolean
    Dim n1str As String, n2str As String, code As String
    Dim ohl As Double, cable As Double, region As String
    Dim vcode As String
    
    IData_Init = True
    
    region = Data(row, map(0))
    n1str = Data(row, map(1))
    n2str = Data(row, map(2))
    code = Data(row, map(3))
    Rval = Data(row, map(4))
    Xval = Data(row, map(5))
    Cap = Data(row, map(6))
    ohl = Data(row, map(7))
    cable = Data(row, map(8))
    BType = Data(row, map(9))
    Index = branches.Count + 1
    
    vcode = Mid(n1str, 5, 1)
    If vcode <> Mid(n2str, 5, 1) Then ' a transformer
        km = 0#
    Else
        Select Case vcode
            Case "4"
                km = Calc_km(region, "400kV", ohl, cable)
                
            Case "2"
                km = Calc_km(region, "275kV", ohl, cable)
                
            Case "1"
                km = Calc_km(region, "132kV", ohl, cable)
    
            Case Else
                km = 0#
        End Select
    End If
    
    If Xval <> 0# Then
        y = PUCONV / Xval
    End If
    
    name = BranchName(n1str, n2str, code)
    If Exists(n1str, nodes) Then
        Set node1 = nodes.item(n1str)
    Else
        MsgBox "Node " & n1str & " definition missing in Branch " & name
        IData_Init = False
    End If
    If Exists(n2str, nodes) Then
        Set node2 = nodes.item(n2str)
    Else
        MsgBox "Node " & n2str & " definition missing in Branch " & name
        IData_Init = False
    End If
        
    node1.Linkto Me
    node2.Linkto Me

End Function

' calculate flow given vang and setptmd and outage state if outages = true

Public Function flow(vang() As Double, setptmd As Long, outages As Boolean) As Double
    Dim f As Double
    
    If BOut And outages Then
        f = 0#
            
 #If LF Then
 
    ElseIf BCtrl Is Nothing Then
        f = (vang(pn1) - vang(pn2)) * y     ' Uncontrolled ac branch
        
    Else
        Select Case BCtrl.CType
                
            Case "QB"
                f = (vang(pn1) - vang(pn2) + BCtrl.SetPoint(setptmd)) * y
                    
            Case "HVDC"
                f = BCtrl.SetPoint(setptmd)
                    
            Case Else
                MsgBox "Unknown control type " & BCtrl.CType
                f = 0#
                
            End Select
            
#Else
    ElseIf Xval <> 0# Then
        f = (vang(pn1) - vang(pn2)) * y
        
    Else
        f = 0#
#End If

    End If
            
    flow = f
End Function

' calculate flow direction given vang

Public Function Dirn(vang() As Double) As Double
    If vang(pn1) >= vang(pn2) Then
        Dirn = 1#
    Else
        Dirn = -1#
    End If
End Function

