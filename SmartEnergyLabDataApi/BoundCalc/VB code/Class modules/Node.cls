VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Node"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' A network node

Option Explicit
Implements IData

Public name As String
Public Index As Long
Public Pn As Long           ' Row/Column position of node in admittance matrix
Public Zone As Zone
Public Demand As Double
Public Generation As Double
Public External As Boolean

Public Property Get IData_Name() As String
    IData_Name = name
End Property

Public Function Idata_FieldMap(mtable As DTable, map() As Long) As Boolean
    Idata_FieldMap = mtable.FieldMap(map, Array("Node", "Zone", "Demand", "Generation", "Ext"))
End Function

Public Function IData_Init(Data() As Variant, row As Long, map() As Long) As Boolean
    Dim zstr As String
    Dim zitem As Zone
    
    name = Data(row, map(0))
    Index = nodes.Count + 1
    zstr = Data(row, map(1))
    Demand = Data(row, map(2))
    Generation = Data(row, map(3))
    External = Data(row, map(4))
    
    If Exists(zstr, zones) Then
        Set Zone = zones.item(zstr)
    Else
        Set zitem = New Zone
        zitem.Index = zones.Count + 1
        
        zones.Add zitem, zstr
        Set Zone = zitem
    End If
    With Zone
        .name = zstr
        If External Then
            .UnscaleDem = .UnscaleDem + Demand
            .UnscaleGen = .UnscaleGen + Generation
        Else
            .Tdemand = .Tdemand + Demand
            .TGeneration = .TGeneration + Generation
        End If
    End With
    
    IData_Init = True
End Function

Public Sub Linkto(br As Branch)
'   Not used in load flow
End Sub
