VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Boundary"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' A network boundary

Option Explicit
Option Base 0
Implements IData

Public name As String
Public trips As String
Public STripList As Collection
Public DTripList As Collection
Private InOut() As Long
Private GenIn As Double         ' scaleable generation inside boundary
Private GenInUS As Double       ' unscaleable generation inside boundary
Private GenOut As Double
Private GenOutUS As Double
Private DemIn As Double
Private DemInUS As Double
Private DemOut As Double
Private DemOutUS As Double
Public PlannedTransfer As Double
Public InterconAllowance As Double
Private kgin As Double          ' inside generation scaling for interconnection allowance
Private kdin As Double
Private kgout As Double
Private kdout As Double
Public BoundCcts As Collection


Public Property Get IData_Name() As String
    IData_Name = name
End Property

Public Property Get Inside(z As Zone) As Long
    Inside = InOut(z.Index - 1)
End Property


Public Function Idata_FieldMap(mtable As DTable, map() As Long) As Boolean
    Dim i As Long, fnames As Variant
    Dim zitem As Zone
    
    fnames = Array("Boundary", "Trips")
    ReDim fnames(zones.Count + 1)
    fnames(0) = "Boundary"
    fnames(1) = "Trips"
    For i = 2 To UBound(fnames)
        Set zitem = zones.item(i - 1)
        fnames(i) = zitem.name
    Next i
        
    Idata_FieldMap = mtable.FieldMap(map, fnames)
End Function

Public Function IData_Init(Data() As Variant, row As Long, map() As Long) As Boolean
    Dim i As Long
    Dim zn As Zone, z1 As Long, z2 As Long
    Dim br As Branch
    
    name = Data(row, map(0))
    trips = Data(row, map(1))
    
    ReDim InOut(zones.Count - 1) As Long
    
    For i = 2 To UBound(map)
        InOut(i - 2) = Data(row, map(i))
    Next i
    
    GenIn = 0#
    GenInUS = 0#
    DemIn = 0#
    DemInUS = 0#
    GenOut = 0#
    GenOutUS = 0#
    DemOut = 0#
    DemOutUS = 0#
    
    For Each zn In zones
        If InOut(zn.Index - 1) <> 0 Then
            GenIn = GenIn + zn.TGeneration
            GenInUS = GenInUS + zn.UnscaleGen
            DemIn = DemIn + zn.Tdemand
            DemInUS = DemInUS + zn.UnscaleDem
        Else
            GenOut = GenOut + zn.TGeneration
            GenOutUS = GenOutUS + zn.UnscaleGen
            DemOut = DemOut + zn.Tdemand
            DemOutUS = DemOutUS + zn.UnscaleDem
        End If
    Next zn
    
    PlannedTransfer = GenIn + GenInUS - DemIn - DemInUS
    InterconAllowance = InterconnectionAllowance(GenIn + GenInUS, DemIn + DemInUS, DemIn + DemInUS + DemOut + DemOutUS)
    
    If PlannedTransfer < 0 Then
        InterconAllowance = -InterconAllowance  ' Interconnection allowance increases magnitude of planned transfer
    End If
    
    kgin = InterconAllowance / (GenIn + DemIn)
    kdin = -kgin
    kdout = InterconAllowance / (GenOut + DemOut)
    kgout = -kdout
    
    Set BoundCcts = New Collection
    
    For Each br In branches
        z1 = br.node1.Zone.Index
        z2 = br.node2.Zone.Index
        
        If InOut(z1 - 1) Xor InOut(z2 - 1) Then
            BoundCcts.Add br
        End If
    Next br
    
    Set STripList = New Collection
    Set DTripList = New Collection
    AutoTripList
           
    IData_Init = True
End Function

Private Function InterconnectionAllowance(gin As Double, din As Double, dtot As Double) As Double
    Dim x As Double, t As Double, y As Double
    
    If din < 0# Then
        x = gin - din
    Else
        x = gin + din
    End If
    
    x = 0.5 * x / dtot
    t = 1# - ((x - 0.5) / 0.5415) ^ 2
    y = Sqr(t) * 0.0633 - 0.0243
    
    InterconnectionAllowance = y * dtot
End Function

Public Sub InterconnectionTransfers(itfr() As Double)
    Dim nd As Node
    Dim zn As Zone
    Dim p As Long, z As Long
    
    ReDim itfr(nodes.Count - 1)
    
    For Each nd In nodes
        p = nd.Pn
        Set zn = nd.Zone
        z = zn.Index - 1
        
        If Not nd.External Then
            If InOut(z) <> 0 Then
                itfr(p) = kgin * nd.Generation - kdin * nd.Demand
            Else
                itfr(p) = kgout * nd.Generation - kdout * nd.Demand
            End If
        End If
    Next nd
End Sub

' Create the triplist for this boundary

Private Sub AutoTripList()
    Dim br As Branch
    Dim tr As Trip
    Dim i As Long, a As Long, b As Long
    Dim nm As String, tstr As String, tsplit As Variant
    
    For Each br In BoundCcts    ' Create single branch trip for each boundary circuit
        Set tr = New Trip
        nm = "S" & CStr(i)
        tr.OneBranch nm, br
        STripList.Add tr, nm
        i = i + 1
    Next br
    
    For a = 1 To BoundCcts.Count - 1    ' create all 2 boundary circuit combinations
        For b = a + 1 To BoundCcts.Count
            Set tr = New Trip
            nm = "D" & CStr(a) & "," & CStr(b)
            tr.Join nm, STripList.item(a), STripList.item(b)
            DTripList.Add tr, nm
        Next b
    Next a
    
    If trips <> "" Then         ' create multi cct trips from any specified in trips
        tsplit = Split(trips, ";")
        For i = 0 To UBound(tsplit)
            Set tr = New Trip
            nm = "T" & CStr(i)
            tstr = tsplit(i)
            tr.Init nm, tstr
            DTripList.Add tr, nm
        Next i
    End If
            
End Sub
