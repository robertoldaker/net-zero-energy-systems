Attribute VB_Name = "Loadflow"
' Manage load flow calculations
' Version 2 9 Jan 2024
' Input data held in collections
' Output results placed in separate output tables
' Analysis result vectors are generally double() global definitions - exception is flow() which is passed by reference by routines


Option Explicit

Public nodes As Collection
Public branches As Collection
Public controls As Collection
Public boundaries As Collection
Public zones As Collection
Public nord As NodeOrder
Public admat As SparseMatrix
Public ufac As SolveLinSym
Public btfer() As Double        ' Base node transfers
Public itfer() As Double        ' Boundary interconnection transfers
Public mism() As Double         ' Mismatch results
Public km() As Double           ' marginal km result
Public tlf() As Double          ' marginal tlf result
Public flow() As Double         ' flow results
Public free() As Double         ' free capacity
Public ord() As Long            ' order of free capacity
Public civang() As Variant      ' Voltage angles for base intact case, max controls and boundary interconnection

Public bc() As Double           ' boundary capacity determined by free capacity on circuit
Public mord() As Long           ' order of bc

Public ActiveBound As Boundary
Public ActiveTrip As Trip
Public tcvang() As Variant      ' trip control voltage angles
Public WorstTrip As Trip
Public WTCapacity As Double

Public OHL_SF() As Variant      ' Tables for calculating circuit km
Public Cable_SF() As Variant

Public MiscOut As DTable        ' Miscellaneous results table
Public mmap() As Long           ' Field map for MiscOut
Public NodeOut As DTable
Public BranchOut As DTable
Public ControlOut As DTable
Public ACCTOut As TopNTab
Public SCCTOut As TopNTab
Public DCCTOut As TopNTab

' Collection item types
Public Const TNode As Long = 0
Public Const TBranch As Long = 1
Public Const TControl As Long = 2
Public Const TBoundary As Long = 3

' Control setpoint types
Public setptmode As Long

Public Const SPZero As Long = 0     ' controls at zero cost points
Public Const SPMan As Long = 1      ' given by ISetPt input data
Public Const SPAuto As Long = 2     ' determined by optimiser values
Public Const SPBLANK As Long = -1   ' do not display

Public Const PUCONV As Double = 10000#
Public Const SCAP As Double = 1#        ' Ignore branches with cap < 1 MW
Public Const OVRLD As Double = -0.05
Public Const MAXCPI As Long = 20        ' Maximum cct constraints added per iteration
Public Const LRGCAP As Long = 50000


Public Function BranchName(n1 As String, n2 As String, code As String) As String
    BranchName = n1 & "-" & n2 & ":" & code
End Function

' Calculate circuit km using scaling factors in OHL_SF and Cable_SF arrays

Public Function Calc_km(region As String, voltage As String, ohl As Double, cable As Double) As Double
    Dim vc As Long, rr As Long
    
    For vc = 2 To UBound(OHL_SF, 2)
        If OHL_SF(1, vc) = voltage Then
            Exit For
        End If
    Next vc
    
    If vc > UBound(OHL_SF, 2) Then
        Calc_km = 0#
        MsgBox "km scale factor for voltage " & voltage & " not found"
        Exit Function
    End If
    
    For rr = 2 To UBound(OHL_SF, 1)
        If OHL_SF(rr, 1) = region Then
            Exit For
        End If
    Next rr
    
    If rr > UBound(OHL_SF, 1) Then
        Calc_km = 0#
'        MsgBox "km scale factor for region " & region & " not found"
        Exit Function
    End If
    
    Calc_km = ohl * OHL_SF(rr, vc) + cable * Cable_SF(rr, vc)
End Function

' Collection items

Public Function NewItem(itemtype As Long) As IData
    Select Case itemtype
        
        Case TNode
            Set NewItem = New Node
            
        Case TBranch
            Set NewItem = New Branch
            
        Case TControl
            Set NewItem = New Control
            
        Case TBoundary
            Set NewItem = New Boundary
            
        Case Else
            Set NewItem = Nothing
    End Select
    
End Function

Public Function Exists(key As String, items As Collection) As Boolean
    Dim it As Object
    
    On Error GoTo errorhandler
    Set it = items.item(key)
    
    Exists = True
    Exit Function

errorhandler:
    Exists = False
End Function

' Read all loadflow input data tables

Public Function ReadLFData(ndtab As String, brtab As String, cttab As String, bntab As String) As Boolean
    Dim ndtable As New DTable
    Dim brtable As New DTable
    Dim cttable As New DTable
    Dim bntable As New DTable
    
    ReadLFData = True
    
    If Not ndtable.Init(ndtab) Then
        MsgBox "Can't find data table " & ndtab
        ReadLFData = False
    End If
    If Not brtable.Init(brtab) Then
        MsgBox "Can't find data table " & brtab
        ReadLFData = False
    End If
    If Not cttable.Init(cttab) Then
        MsgBox "Can't find data table " & cttab
        ReadLFData = False
    End If
    If Not bntable.Init(bntab) Then
        MsgBox "Can't find data table " & bntab
        ReadLFData = False
    End If
    
    Set nodes = New Collection
    Set branches = New Collection
    Set controls = New Collection
    Set zones = New Collection
    Set boundaries = New Collection
    
    OHL_SF = Range("OHL").Value
    Cable_SF = Range("Cable").Value
    
    If ReadLFData Then
        If Not ndtable.Collect(nodes, TNode) Then
            MsgBox "Can't parse node data"
            ReadLFData = False
        End If
        
        If Not brtable.Collect(branches, TBranch) Then
            MsgBox "Can't parse branch data"
            ReadLFData = False
        End If
           
        If Not cttable.Collect(controls, TControl) Then
            MsgBox "Can't parse control data"
            ReadLFData = False
        End If
         
        If Not bntable.Collect(boundaries, TBoundary) Then
            MsgBox "Can't parse boundary data"
            ReadLFData = False
        End If
    End If
            
End Function

' Connectivity check - Returns subnet id for all nodes and first detected non-zero subnet

Private Function Subnets(snet() As Long) As Long
    Dim br As Branch
    Dim i As Long, j1 As Long, j2 As Long
    Dim chng As Boolean, nmx As Long
    
    nmx = nodes.Count - 1
    
    ReDim snet(nmx) As Long
    For i = 0 To nmx
        snet(i) = i
    Next i
    
    Do
        chng = False
        For Each br In branches
        
            j1 = br.node1.Index - 1
            j2 = br.node2.Index - 1
            
            If snet(j1) < snet(j2) Then
                snet(j2) = snet(j1)
                chng = True
            ElseIf snet(j2) < snet(j1) Then
                snet(j1) = snet(j2)
                chng = True
            End If
        Next br
        
    Loop While chng
    
    For i = 0 To nmx
        If snet(i) <> 0 Then
            MsgBox "Disconnected network detected at node " & nodes.item(i + 1).name
            Subnets = i
            Exit Function
        End If
    Next i
    Subnets = 0
End Function

' Create intact network admittance matrix
' Sets up node ordering object

Private Function AdmittanceMat(Optional LF As Boolean = True) As SparseMatrix
    Dim i As Long, nupb As Long, nr As Long, nc As Long
    Dim y As Double, n1 As Long, n2 As Long
    Dim adm As New SparseMatrix
    Dim br As Branch, nd As Node
    
    ' Calculate optimal ordering of nodes
    Set nord = New NodeOrder
    nord.Init
    
    If LF Then
        nupb = nord.nn - 1  ' exclude reference node
    Else
        nupb = nord.nn
    End If
    
    For Each nd In nodes
        nd.Pn = nord.NodePos(nd.Index)      ' cache node positions in admittance matrix
    Next nd
    
    adm.Init nupb, nupb, nord.fz / (nupb + 1)
    
    For Each br In branches
        With br
        .pn1 = .node1.Pn        ' cache node1 and node2 positions in admittance matrix
        .pn2 = .node2.Pn
        
        If .Xval <> 0# Then
            y = PUCONV / .Xval
    
            If .pn1 > .pn2 Then ' ensure upper diagonal
                nr = .pn2
                nc = .pn1
            Else
                nr = .pn1
                nc = .pn2
            End If
        
            adm.Addin nr, nr, y
            If nc <= nupb Then      ' ensure not reference
                adm.Addin nc, nc, y
                adm.Addin nr, nc, -y
            End If
        End If
        End With
    Next br
    
    Set AdmittanceMat = adm
End Function

' Setup base transfer vector

Private Sub BaseTransfers(tvec() As Double)
    Dim nd As Node
    
    ReDim tvec(nodes.Count - 1) As Double
    
    For Each nd In nodes    ' includes transfer at reference node & hvdc nodes
        tvec(nd.Pn) = nd.Generation - nd.Demand
    Next nd
End Sub

' Calculate base transfer plus interconnection
' isf scales interconnection allowance used in itfr

Public Sub CalcTransfers(isf As Double, tfr() As Double)
    Dim i As Long
    
    tfr = btfer
    
    If isf <> 0# Then
        For i = 0 To UBound(tfr)
            tfr(i) = tfr(i) + isf * itfer(i)
        Next i
    End If
End Sub


' Calculate all flows and mismatches from vangs and setpoints
' call with mism = transfers
' outages=false ensures intact network calculation irrespective of ActiveTrip

Public Sub CalcFlows(vang() As Double, setptmd As Long, outages As Boolean, lflow() As Double, mism() As Double)
    Dim br As Branch
    Dim f As Double
    
    ReDim lflow(branches.Count - 1) As Double
    
    For Each br In branches
        With br
            f = .flow(vang, setptmd, outages)
            lflow(.Index - 1) = f
            mism(.pn1) = mism(.pn1) - f
            mism(.pn2) = mism(.pn2) + f
        End With
    Next br
End Sub


' Calculate intact voltage angles for base transfers and each control
' sets mism by calculating base intact flows

Private Function CalcIntactVang() As Long
    Dim ct As Control, br As Branch
    Dim tvec() As Double, vang() As Double
    
    ReDim civang(controls.Count + 1) As Variant     ' index 0 is base vang, 1 .. n are controls and n+1 is boundary transfer
    
    For Each ct In controls
        ReDim tvec(nodes.Count - 1) As Double
        
        Set br = ct.CBranch
    
        If br.pn1 <= nord.nn Or br.pn2 <= nord.nn Then ' calc sensitivity if at least one node ac
            tvec(br.pn1) = -ct.Injmax
            tvec(br.pn2) = ct.Injmax
            ufac.Solve tvec, vang
            civang(ct.Index) = vang
        Else
            civang(ct.Index) = Empty
        End If
    Next ct
    
    BaseTransfers btfer
    mism = btfer
    
    ufac.Solve btfer, vang
    civang(0) = vang
    CalcFlows vang, SPZero, False, flow, mism
    
    CalcIntactVang = nord.NodeId(nord.nn)  ' Index of refnode
End Function

' Prepare node results for column output

Public Sub CalcNodeColumn(val() As Double, Data() As Variant)
    Dim n As Long, nd As Node
    
    n = nodes.Count
    ReDim Data(1 To n, 1 To 1)
    
    For Each nd In nodes
        Data(nd.Index, 1) = val(nd.Pn)
    Next nd
End Sub

' Prepare branch results for column output

Public Sub CalcBranchColumn(val() As Double, Data() As Variant)
    Dim n As Long, br As Branch
    
    n = branches.Count
    ReDim Data(1 To n, 1 To 1)
    
    For Each br In branches
        Data(br.Index, 1) = val(br.Index - 1)
    Next br
End Sub

' Prepare outage field

Public Sub BranchOutageColumn(Clear As Boolean, Data() As Variant)
    Dim n As Long, br As Branch
    
    n = branches.Count
    ReDim Data(1 To n, 1 To 1)
    
    If Clear Then
        For Each br In branches
            br.BOut = False
            Data(br.Index, 1) = Empty
        Next br
        
    Else
        For Each br In branches
            If br.BOut Then
                Data(br.Index, 1) = 1
            End If
        Next br
    End If
End Sub

' Prepare control results for column output

Public Sub CalcControlColumn(val() As Double, Data() As Variant)
    Dim n As Long, ct As Control
    
    n = controls.Count
    ReDim Data(1 To n, 1 To 1)
    
    For Each ct In controls
        Data(ct.Index, 1) = val(ct.Index)
    Next ct
End Sub

' Prepare setpoint results for column output

Public Sub CalcSetPointColumn(Data() As Variant)
    Dim n As Long, ct As Control
    
    n = controls.Count
    If n > 0 Then
        ReDim Data(1 To n, 1 To 1)
    End If
    
    For Each ct In controls
        Data(ct.Index, 1) = ct.SetPoint(setptmode)
    Next ct
End Sub

Public Sub CalcCtrlCostColumn(Data() As Variant)
    Dim n As Long, ct As Control
    
    n = controls.Count
    If n > 0 Then
        ReDim Data(1 To n, 1 To 1)
    End If
    
    For Each ct In controls
        Data(ct.Index, 1) = ct.Cost / ct.MaxCtrl * Abs(ct.SetPoint(setptmode))
    Next ct
End Sub

' Produce basic node output table

Public Function MakeNodeOutTable(name As String) As DTable
    Dim tout As DTable
    Dim nmap() As Long
    Dim results() As Variant
    Dim nd As Node
    
    Set tout = New DTable
    
    If Not tout.Init(name) Then
        MsgBox "Can't find data table " & name
        Exit Function
    End If
    
    If Not tout.FieldMap(nmap, Array("Node", "Zone", "Transfer", "Mismatch")) Then
        MsgBox name & " table required fields not found"
        Exit Function
    End If
    
    tout.Resize nodes.Count
    tout.GetData results
    For Each nd In nodes
        With nd
            tout.PopulateRow results, .Index, nmap, .name, .Zone.name, .Generation - .Demand, mism(.Pn)
        End With
    Next nd
    tout.PutData results
    
    Set MakeNodeOutTable = tout
End Function

' Produce basic branch output table

Public Function MakeBranchOutTable(name As String) As DTable
    Dim tout As DTable
    Dim nmap() As Long
    Dim results() As Variant
    Dim br As Branch
    
    Set tout = New DTable
    
    If Not tout.Init(name) Then
        MsgBox "Can't find data table " & name
        Exit Function
    End If
    
    If Not tout.FieldMap(nmap, Array("Branch", "Type", "Cap", "km", "Flow")) Then
        MsgBox name & " table required fields not found"
        Exit Function
    End If
    
    tout.Resize branches.Count
    tout.GetData results
    For Each br In branches
        With br
            tout.PopulateRow results, .Index, nmap, .name, .BType, .Cap, .km, flow(.Index - 1)
        End With
    Next br
    tout.PutData results
    
    Set MakeBranchOutTable = tout
End Function

' Produce basic control output table

Public Function MakeControlOutTable(name As String) As DTable
    Dim tout As DTable
    Dim nmap() As Long
    Dim results() As Variant
    Dim ct As Control
    
    Set tout = New DTable
    
    If Not tout.Init(name) Then
        MsgBox "Can't find data table " & name
        Exit Function
    End If
    
    If Not tout.FieldMap(nmap, Array("Control", "Type", "Cost", "SetPoint")) Then
        MsgBox name & " table required fields not found"
        Exit Function
    End If
    
    tout.Resize controls.Count
    tout.GetData results
    For Each ct In controls
        With ct
            tout.PopulateRow results, .Index, nmap, .longname, .CType, 0#, 0#
        End With
    Next ct
    tout.PutData results
    
    Set MakeControlOutTable = tout
End Function

Public Function MakeTopNTable(name As String, n As Long) As TopNTab
    Dim ttab As DTable
    Dim topn As TopNTab
    
    Set ttab = New DTable
    
    If Not ttab.Init(name) Then
        MsgBox "Can't find data table " & name
        Exit Function
    End If
    
    Set topn = New TopNTab
    
    topn.Init ttab, n
    
    Set MakeTopNTable = topn
End Function

' Report misc results

Private Sub MiscReport(item As Variant, Value As Variant)
    MiscOut.NewRow mmap, item, Value
End Sub

' Get array of input and output table names from excel range

Private Sub TableNames(tableref As String, tables() As String)
    Dim t As Variant
    Dim i As Long
    
    t = Range(tableref)
    ReDim tables(1 To UBound(t, 1))
    
    For i = 1 To UBound(t, 1)
        tables(i) = t(i, 1)
    Next i
End Sub


' Setup input collections, output tables, check connectivity and calulate intact voltage angles

Public Function Netcheck(tableref As String) As Boolean
    Dim res As Boolean
    Dim nd As Node
    Dim zn As Zone, tgen As Double, tdem As Double, timp As Double
    Dim snet() As Long, mm As Double, refindex As Long
    Dim results() As Variant
    Dim tables() As String
    
    TableNames tableref, tables
       
    Set MiscOut = New DTable
    If Not MiscOut.Init(tables(5)) Then
        MsgBox "Can't find data table " & tables(5)
        Netcheck = False
        Exit Function
    End If
    
    If Not MiscOut.FieldMap(mmap, Array("Item", "Value")) Then
        MsgBox "MiscOutput required fields not found"
        Netcheck = False
        Exit Function
    End If
    
    MiscOut.ClearRows
    
    res = ReadLFData(tables(1), tables(2), tables(3), tables(4))
    
    If Not res Then
        Netcheck = False
        Exit Function
    End If
    
    MiscReport "#nodes", nodes.Count
    MiscReport "#zones", zones.Count
    MiscReport "#branches", branches.Count
    MiscReport "#controls", controls.Count
    MiscReport "#boundaries", boundaries.Count
    
    For Each zn In zones
        tgen = tgen + zn.TGeneration
        tdem = tdem + zn.Tdemand
        timp = timp + zn.UnscaleGen - zn.UnscaleDem
    Next zn
    
    MiscReport "Generation", FormatNumber(tgen, 1)
    MiscReport "Demand", FormatNumber(tdem, 1)
    MiscReport "Imports", FormatNumber(timp, 1)
    
    res = (Subnets(snet) = 0)
    
    If Not res Then
        Netcheck = False
        Exit Function
    End If
    
    Set admat = AdmittanceMat(True)
    
    MiscReport "HVDC nodes", nodes.Count - nord.nn - 1
    MiscReport "NZ per row", nord.nz / nord.nn ' FormatNumber(nord.nz / nord.nn, 1)
    MiscReport "FZ per row", nord.fz / nord.nn ' FormatNumber(nord.fz / nord.nn, 1)
    
    Set ufac = New SolveLinSym      ' factorise admittance matrix
    ufac.Init admat, False
    refindex = CalcIntactVang()
    
    Set nd = nodes.item(refindex)
    
    MiscReport "RefNode", nd.name
    MiscReport "No Ctrl Mismatch", mism(nd.Pn)
    
    Set ActiveBound = Nothing
    Set ActiveTrip = Nothing
    
    ' Create output tables and populate with basic results
    
    Set NodeOut = MakeNodeOutTable(tables(6))
    Set BranchOut = MakeBranchOutTable(tables(7))
    Set ControlOut = MakeControlOutTable(tables(8))
    Set ACCTOut = MakeTopNTable(tables(9), 20)
    Set SCCTOut = MakeTopNTable(tables(10), 30)
    Set DCCTOut = MakeTopNTable(tables(11), 30)
    
    If (NodeOut Is Nothing) Or (BranchOut Is Nothing) Or (ControlOut Is Nothing) Or (ACCTOut Is Nothing) Or (SCCTOut Is Nothing) Or (DCCTOut Is Nothing) Then
        Netcheck = False
    Else
        Netcheck = True
    End If
    
End Function

' Calculate total vang using control setpoints and interconnection scaling factor iasf
' iasf scales the interconnection allowance used to compute ctrlva(upb)

Public Sub CalcVang(iasf As Double, ctrlva() As Variant, vang() As Double)
    Dim i As Long, j As Long, sf2 As Double, sp As Double
    Dim tv() As Double, n As Long
    Dim ct As Control
    
    n = controls.Count + 1
    
    vang = ctrlva(0)    ' base vangs
    
    If iasf <> 0# And IsArray(ctrlva(n)) Then
        tv = ctrlva(n)
        For i = 0 To UBound(vang, 1)
            vang(i) = vang(i) + tv(i) * iasf
        Next i
    End If
    
    For Each ct In controls
        With ct
        sp = .SetPoint(setptmode)
        If sp <> 0# And IsArray(ctrlva(.Index)) Then
            tv = ctrlva(.Index)
            sf2 = sp / .MaxCtrl
            For j = 0 To UBound(vang, 1)
                vang(j) = vang(j) + tv(j) * sf2
            Next j
        End If
        End With
    Next ct
End Sub

' Return node position of largest mismatch

Public Function MaxMismatch(mism() As Double) As Long
    Dim i As Long
    Dim maxm As Double, amism As Double
    Dim maxi As Long
    
    For i = 0 To UBound(mism)
        amism = Abs(mism(i))
        If amism > maxm Then
            maxm = amism
            maxi = i
        End If
    Next i
    MaxMismatch = maxi
End Function


' Calculate mwkm and loss results

Public Sub CalcBranchAux(lflow() As Double, mwkm() As Double, loss() As Double)
    Dim br As Branch
    Dim n As Long, f As Double
    
    n = branches.Count - 1
    
    ReDim mwkm(n) As Double
    ReDim loss(n) As Double
    
    For Each br In branches
        f = lflow(br.Index - 1)
        mwkm(br.Index - 1) = Abs(f * br.km)
        loss(br.Index - 1) = f * f * br.Rval / PUCONV
    Next br
End Sub

' Calculate cct free capacity in direction of flow

Public Sub CalcMinFree(flow() As Double)
    Dim i As Long
    Dim n As Long
    Dim br As Branch
    
    n = branches.Count - 1
    ReDim free(n) As Double
    ReDim ord(n) As Long
    
    For Each br In branches
        With br
            i = .Index - 1
        
            If .Cap > SCAP Then
                free(i) = .Cap - Abs(flow(i))
            Else
                free(i) = 99999#
            End If
            ord(i) = i
        End With
    Next br
    MergeSortFlt free, ord, n + 1
End Sub

' Calculate cct free capacity in flow direction given by vang

Public Sub CalcDirFree(flow() As Double, vang() As Double)
    Dim i As Long
    Dim n As Long
    Dim br As Branch
    
    n = branches.Count - 1
    ReDim free(n) As Double
    ReDim ord(n) As Long
    
    For Each br In branches
        With br
            i = .Index - 1
        
            If .Cap > SCAP Then
                free(i) = .Cap - flow(i) * .Dirn(vang)
            Else
                free(i) = 99999#
            End If
            ord(i) = i
        End With
    Next br
    MergeSortFlt free, ord, n + 1
End Sub

' Calculate the boundary scaling factor at which given flows will reach capacity
' Calculate boundcap implied by each circuit assuming flows correspond to specified transfer

Public Function CalcSF(mflow() As Double, ivang() As Double, tfer As Double, ia As Double) As Double
    Dim i As Long
    Dim n As Long
    Dim br As Branch
    Dim mfree As Double
    Dim iflow As Double
    
    n = branches.Count - 1
    ReDim mord(n) As Long
    ReDim bc(n) As Double
    
    For Each br In branches
    With br
        i = .Index - 1
        iflow = .flow(ivang, SPZero, True)     ' the flow resulting from interconnection
        
        If br.Cap > SCAP Then
            mfree = br.Cap - mflow(i) * Sgn(iflow)
        Else
            mfree = 99999#
        End If
        If Abs(iflow) < lpEpsilon Then
            bc(i) = 99999#
        Else
            bc(i) = tfer + ia * mfree / Abs(iflow)
        End If
        mord(i) = i
    End With
    Next br
    
    MergeSortFlt bc, mord, n + 1
    i = mord(0)
    Set br = branches.item(i + 1)
    iflow = br.flow(ivang, SPZero, True)
    
    If br.Cap > SCAP Then
        mfree = br.Cap - mflow(i) * Sgn(iflow)
    Else
        mfree = 99999#
    End If
    
    If Abs(iflow) < lpEpsilon Then
        MsgBox "Unable to calculate boundary capacity as interconnection flow too small"
    Else
        CalcSF = mfree / Abs(iflow)
    End If
End Function


' Calculate the loadflow with interconnection and control setptoints
' setpoints depend on setptmode global
' returns node index with largest mismatch

Public Sub CalcLoadFlow(cvang() As Variant, iasf As Double, lflow() As Double, Optional save As Boolean = True)
    Dim tfr() As Double, vang() As Double
    
    ReDim lflow(branches.Count - 1)
    
    CalcTransfers iasf, tfr
    mism = tfr
    CalcVang iasf, cvang, vang
    CalcFlows vang, setptmode, True, lflow, mism
            
    If save Then
        SaveLFResults lflow
    End If
End Sub

Public Sub SaveLFResults(lflow() As Double)
    Dim loss() As Double
    Dim mwkm() As Double
    Dim results() As Variant
    Dim mi As Long, mm As Double
    Dim nd As Node
    
    CalcNodeColumn mism, results
    NodeOut.PutColumn "Mismatch", results
        
    CalcBranchColumn lflow, results
    BranchOut.PutColumn "Flow", results
    
    BranchOutageColumn False, results
    BranchOut.PutColumn "Out", results
        
    CalcMinFree lflow
    CalcBranchColumn free, results
    BranchOut.PutColumn "Free", results
        
    CalcBranchAux lflow, mwkm, loss
    CalcBranchColumn mwkm, results
    BranchOut.PutColumn "MW.km", results
        
    CalcBranchColumn loss, results
    BranchOut.PutColumn "Loss", results
    
    CalcSetPointColumn results
    ControlOut.PutColumn "SetPoint", results
    
    CalcCtrlCostColumn results
    ControlOut.PutColumn "Cost", results
        
    mi = MaxMismatch(mism)
    mm = mism(mi)
    Set nd = nodes.item(nord.NodeId(mi))
    MiscReport "Max mismatch " & nd.name, mm
End Sub

' Calculate the boundary capability with manual control setpoints
' if cva(upb) is unpopulated then calculate loadflow at the planned transfer condition

Public Sub CalcBoundLF(cvang() As Variant, lflow() As Double, Optional save As Boolean = False)
    Dim mflow() As Double
    Dim ivang() As Double
    Dim iasf As Double
    Dim pt As Boolean
    Dim pfer As Double
    
    pt = ActiveBound Is Nothing
    
    CalcLoadFlow cvang, 0#, mflow, False    ' Loadflow at planned transfer
    
    If Not pt Then
        If ActiveTrip Is Nothing Then
            ivang = civang(controls.Count + 1)  ' get ivang from intact volt angles
        Else
            ivang = tcvang(controls.Count + 1)  ' get ivang from trip volt angles
        End If
        
        iasf = CalcSF(mflow, ivang(), Abs(ActiveBound.PlannedTransfer), Abs(ActiveBound.InterconAllowance))    ' smallest scaling factor

        CalcLoadFlow cvang, iasf, lflow(), save
        MiscReport "Boundary capacity", Format(bc(mord(0)), "0.00")
    Else
        lflow = mflow
        If save Then
            SaveLFResults mflow
        End If
    End If
    
End Sub

Public Sub ClearOutages()
    Dim br As Branch
    
    For Each br In branches
        br.BOut = False
    Next br
End Sub

' Balance HV nodes

Public Function BalanceHVDC() As Long
    Dim r1 As Long, r2 As Long
    Dim title As String
    
    title = "Balance HVDC"
    
    ResetLP
    r1 = ctrllp.SolveLP(r2)
    
    If r1 = lpOptimum Then
        MiscReport title, "Ctrl cost: " & Format(ControlCost(), "0.00")
    Else
        If r1 = lpInfeasible Then
            MiscReport title, "Unresolvable constraint " & ctrllp.cname(r2)
        Else
            MiscReport "title", "Unknown optimiser fail"
        End If
    End If
    
    BalanceHVDC = r1
End Function

' Optimise the loadflow
' if activebound is nothing then optimise the planned transfer condition
' else add boundary circuits plus overloads and optimise with sensitivities to the boundary transfer variable

Public Function OptimiseLoadflow(cva() As Variant, lflow() As Double, Optional save As Boolean = True) As Long
    Dim i As Long, iter As Long, xa As Long
    Dim r1 As Long, r2 As Long
    Dim nd As Node, br As Branch
    Dim title As String, iasf As Double
    Dim ivang() As Double
    Dim pt As Boolean
    
    setptmode = SPAuto
    pt = ActiveBound Is Nothing
    r1 = BalanceHVDC()
    
    CalcLoadFlow cva, 0#, lflow, r1 <> lpOptimum     ' start at planned transfer
    
    If r1 <> lpOptimum Then
        OptimiseLoadflow = r1
        Exit Function
    End If
    
    xa = UBound(cva, 1)
    
    If Not pt Then                                  ' add boundary circuits with free calculated in direction of boundary interconnection
        ivang = cva(xa)
        CalcDirFree lflow, ivang
        For Each br In ActiveBound.BoundCcts
            PopulateConstraint br, free(br.Index - 1), br.Dirn(ivang), iasf, cva, False
        Next br
        
        title = "Capacity of boundary circuits"
        r1 = ctrllp.SolveLP(r2)
        
        If r1 = lpOptimum Then
            iasf = boun.Value(ctrllp) / Abs(ActiveBound.InterconAllowance)
            MiscReport title, Format(BoundCap(), "0.00")
        Else
            If r1 = lpInfeasible Then
                MiscReport title, "Unresolvable constraint " & ctrllp.cname(r2)
            Else
                MiscReport title, "Unknown optimiser fail"
            End If
        End If
        
        CalcLoadFlow cva, iasf, lflow, r1 <> lpOptimum
        
        If r1 <> lpOptimum Then
            OptimiseLoadflow = r1
            Exit Function
        End If
    End If
    
    Do
        iter = iter + 1
        
        If pt Then
            CalcMinFree lflow
        Else
            CalcDirFree lflow, ivang       ' free in direction of interconnection transfer
        End If
        
        i = 0
        While (free(ord(i)) < OVRLD) And (i <= MAXCPI)
            Set br = branches.item(ord(i) + 1)
            Debug.Print br.name, Format(free(ord(i)), "0.0")
            If pt Then
                PopulateConstraint br, free(ord(i)), lflow(ord(i)), 0#, cva, pt
            Else
                PopulateConstraint br, free(ord(i)), br.Dirn(ivang), iasf, cva, pt
            End If
            i = i + 1
        Wend
        
        If i = 0 Then
            Exit Do
        End If
        
        MiscReport "Iter " & CStr(iter), "Ovrlds: " & CStr(i - 1)
        
        r1 = ctrllp.SolveLP(r2)
        
        If Not pt Then
           iasf = boun.Value(ctrllp) / Abs(ActiveBound.InterconAllowance)
           Debug.Print Format(BoundCap(), "0.00")
        End If
        
        CalcLoadFlow cva, iasf, lflow, r1 <> lpOptimum
        
        If r1 <> lpOptimum Then
            If r1 = lpInfeasible Then
                MiscReport "Optimiser", "Unresolvable constraint " & ctrllp.cname(r2)
            Else
                MiscReport "Optimiser", "Unknown optimiser fail"
            End If
            
            OptimiseLoadflow = r1
            Exit Function
        End If
    Loop
    
    If Not pt Then
        MiscReport "Boundary capacity (all circuits)", Format(BoundCap(), "0.00")
        CalcSF lflow, ivang, Abs(BoundCap()), Abs(ActiveBound.InterconAllowance)
    End If
    
    If save Then
        SaveLFResults lflow
        
        MiscReport "Boundary optimisation", "Ctrl cost: " & Format(ControlCost(), "0.00")
    End If
    
    OptimiseLoadflow = r1
End Function

' Calculate nodal loss and mwkm sensitivities
' Based on base flows bflow()

Public Sub CalcNodeMarginals(bflow() As Double)
    Dim tv() As Double, va() As Double
    Dim sflow() As Double
    Dim i As Long, j As Long
    Dim nd As Node, br As Branch
    
    ReDim tlf(nodes.Count - 1)
    ReDim km(nodes.Count - 1)
    
    For i = 0 To nord.nn - 1 ' for each node excluding refnode and hvdc nodes
        ReDim tv(nodes.Count - 1)
        tv(i) = 1#
        
        ufac.Solve tv, va
        CalcFlows va, SPZero, True, sflow, tv
        
        For Each br In branches
            j = br.Index - 1
            tlf(i) = tlf(i) + 2# * bflow(j) * sflow(j) * br.Rval / PUCONV
            If bflow(j) * sflow(j) < 0# Then
                km(i) = km(i) - Abs(sflow(j) * br.km)
            Else
                km(i) = km(i) + Abs(sflow(j) * br.km)
            End If
        Next br
    Next i
End Sub

' Set the active boundary always clearing any active trip

Public Sub SetActiveBoundary(bnd As Boundary)
    Dim iflow() As Double
    Dim ivang() As Double
    Dim mi As Long, mm As Double
    Dim nd As Node
    
    If Not ActiveBound Is bnd Then
        Set ActiveBound = bnd
        
        If Not ActiveTrip Is Nothing Then
            ActiveTrip.Deactivate
            Set ActiveTrip = Nothing
        End If
        
        If Not bnd Is Nothing Then
            ' calc interconnection vang for new boundary
            bnd.InterconnectionTransfers itfer
            mism = itfer
            ufac.Solve itfer, ivang
            civang(controls.Count + 1) = ivang
            CalcFlows ivang, SPZero, False, iflow, mism
            mi = MaxMismatch(mism)
            mm = mism(mi)
            Set nd = nodes.item(nord.NodeId(mi))
            MiscReport bnd.name & " setup mismatch " & nd.name, mm
            MiscReport "Planned Transfer", Format(bnd.PlannedTransfer, "0.00")
            MiscReport "Interconnection Allowance", Format(bnd.InterconAllowance, "0.00")
            ACCTOut.Clear
            SCCTOut.Clear
            DCCTOut.Clear
        Else
            civang(controls.Count + 1) = Empty
            MiscReport "Boundary unspecified", ""
        End If
    Else
        ' nothing to do
    End If
End Sub

' Set a trip to be active
' Sets trip voltage angles tcvang if successful

Public Function SetActiveTrip(tr As Trip) As Boolean
    Dim vang() As Double
    Dim iflow() As Double
    Dim sensmat() As Double
    
    If ActiveTrip Is tr Then                  ' nothing to do - note trip not reactivated
        
        SetActiveTrip = True
        Exit Function
    End If
    
    If Not ActiveTrip Is Nothing Then           ' deactive current activetrip
        ActiveTrip.Deactivate
        Set ActiveTrip = Nothing
    End If
    
    If tr Is Nothing Then                         ' nothing to do
        SetActiveTrip = True
        Exit Function
    End If
                                 '
    If tr.TripVectors(civang, tcvang) Then
        MiscReport "Setup Trip " & tr.name, tr.TripDescription
        Set ActiveTrip = tr
        SetActiveTrip = True
            
    Else
        MiscReport "Trip" & tr.name, "splits AC network"
        SetActiveTrip = False
    End If
End Function


' Run calculator
' If bound is nothing then run planned transfer case
' If bound is not already active then setup itfer and civang
' optionally save boundary max transfer or planned transfer loadflows
' optionally undertake nodemarginals calcfor planned transfer only

Public Function RunBoundCalc(bound As Boundary, tr As Trip, setptmd As Long, nodemarginals As Boolean, Optional save As Boolean = False) As Long
    Dim results() As Variant
    Dim vang() As Double, cvang() As Variant
    Dim nd As Node
    Dim mi As Long, mm As Double
    Dim res As Long
    
    SetActiveBoundary bound
    
    If Not SetActiveTrip(tr) Then
        RunBoundCalc = lpZeroPivot
        Exit Function
    End If
        
    If ActiveTrip Is Nothing Then
        cvang = civang
    Else
        cvang = tcvang
    End If
    
    If setptmd = SPAuto Then
        setptmode = SPAuto
        res = OptimiseLoadflow(cvang, flow, save)
        If res <> lpOptimum Then
            RunBoundCalc = res
            Exit Function
        End If
        If Not ActiveBound Is Nothing Then
            If Abs(BoundCap()) < WTCapacity Then
                Set WorstTrip = tr
                WTCapacity = Abs(BoundCap())
            End If
        End If
        If save Then
            ReportConstraints results
            BranchOut.PutColumn "Shadow", results
        End If
    Else
        setptmode = SPMan
        CalcBoundLF cvang, flow, save
        If Not ActiveBound Is Nothing Then
            If bc(mord(0)) < WTCapacity Then
                Set WorstTrip = tr
                WTCapacity = bc(mord(0))
            End If
        End If
        If save Then
            BranchOut.ClearColumn "Shadow"
        End If
    End If
       
    If bound Is Nothing And nodemarginals Then
        CalcNodeMarginals flow
        
        CalcNodeColumn tlf, results
        NodeOut.PutColumn "TLF", results
        CalcNodeColumn km, results
        NodeOut.PutColumn "km", results
    Else
        NodeOut.ClearColumn "TLF"
        NodeOut.ClearColumn "km"
    End If
       
End Function

' Run a trip case and fill in relevant TopN tables
' boundary must be specified

Public Sub RunTrip(bn As Boundary, tr As Trip, setptmd As Long, Optional save As Boolean = False)
    Dim limccts As String
    Dim res As Long
    
    If tr Is Nothing Then ' intact network case
        ACCTOut.SetBoundary bn, bn.PlannedTransfer + bn.InterconAllowance ' use full interconnection for intact case
        res = RunBoundCalc(bn, Nothing, setptmd, False, save)
        If res <> lpOptimum Then
            ACCTOut.Error Nothing, res, ""
        Else
            If setptmd = SPAuto Then
                limccts = Limitccts()
            End If
            ACCTOut.Insert Nothing, bc, mord, limccts, save
        End If
        
    Else
        If left(tr.name, 1) = "S" Then ' single circuit trip
            SCCTOut.SetBoundary bn, bn.PlannedTransfer + bn.InterconAllowance ' use full interconnection for single cct trips
            res = RunBoundCalc(bn, tr, setptmd, False, save)
            If res <> lpOptimum Then
                SCCTOut.Error tr, res, ""
            Else
                If setptmd = SPAuto Then
                    limccts = Limitccts()
                End If
                SCCTOut.Insert tr, bc, mord, limccts, save
            End If
        
        Else
            DCCTOut.SetBoundary bn, bn.PlannedTransfer + 0.5 * bn.InterconAllowance ' use half interconnection for double cct trips
            res = RunBoundCalc(bn, tr, setptmd, False, save)
            If res <> lpOptimum Then
                DCCTOut.Error tr, res, ""
            Else
                If setptmd = SPAuto Then
                    limccts = Limitccts()
                End If
                DCCTOut.Insert tr, bc, mord, limccts, save
            End If
        End If
    End If

End Sub

' Run trip collection
' Boundary must be specified

Public Sub RunTripSet(bn As Boundary, trips As Collection, setptmd As Long)
    Dim tr As Trip
    
    For Each tr In trips
        RunTrip bn, tr, setptmd, False
    Next tr
    
    ACCTOut.UpdateTable
    SCCTOut.UpdateTable
    DCCTOut.UpdateTable
End Sub

' Run all trips
' Save results for intact max transfer case
'

Public Sub RunAllTrips(bn As Boundary, setptmd As Long)
    WTCapacity = 99999#
    
    ACCTOut.Clear
    SCCTOut.Clear
    DCCTOut.Clear
    
    RunTrip bn, Nothing, setptmd, True
    
    RunTripSet bn, bn.STripList, setptmd

    RunTripSet bn, bn.DTripList, setptmd

    MiscReport "Worst Case Trip", Format(WTCapacity, "0.00")
    RunBoundCalc bn, WorstTrip, setptmd, False, True
    
End Sub

Public Sub Start()
    UserForm1.Show
End Sub


Public Sub Test()
    UserForm1.TextBox1.text = "TestTables"
    UserForm1.Show
    
End Sub

