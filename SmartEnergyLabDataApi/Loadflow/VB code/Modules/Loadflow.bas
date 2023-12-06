Attribute VB_Name = "Loadflow"
' Linearised loadflow

Option Base 0
Option Explicit

Public ntab As DTable       'Node table
Public zstr() As Variant        'Zone name
Public nstr() As Variant        'Node name
Public gen() As Variant         'Gen MW
Public dem() As Variant         'Dem MW
Public ext() As Variant         'Node transfer not scaled by IA
Public nind As DIndex       'Node name index
Public nord As New NodeOrder    'AC nodeordering
Public btfr() As Double         'G-D
' Public bvang() As Double    'Base vang (all controls null)

Public btab As DTable       'AC Branch table
Public n1str() As Variant       'Node1 name
Public n2str() As Variant       'Node2 name
Public lcstr() As Variant       'Linecode name
Public bn1() As Long            'Node1 order position
Public bn2() As Long            'Node2 order position
Public xval() As Variant        'Reactance  = 0 for HVDC
Public bcap() As Variant        'Capacity
Public bout() As Boolean        'Outaged?
Public bind As DIndex       'Branch name index
Public bctrl() As Long      'Index to controller
Public ipflow() As Variant  'Intact planned flow

Public ctab As DTable       'Controls table
Public cbid() As Long           'Branch id
Public cminc() As Variant       'Min control action
Public cmaxc() As Variant       'Max control action
Public ccost() As Variant       'Cost of max action
Public injmax() As Double       'Max control injection
Public ctype() As Variant       'QB or HVDC
Public cvang() As Variant       'Component Vang 0=base, 1 per max control (fwd direction), last for xfer
Public csp() As Variant         'Control setpoints

Public Const PUCONV As Double = 10000# 'Conversion for % on 100MVA to pu on 1MVA
Public Const MINCAP As Double = 1#     'Branch flows ignored if cap below MINCAP

Public admat As SparseMatrix
Public ufac As SolveLinSym

Private Const BPL As Long = 31  ' bits per long
Public Const STPASS As Long = 0
Public Const STFAIL As Long = -1
Public Const STWARN As Long = 1
Public Const MINFREE As Double = 1#
Public Const SCAP As Double = 1#    ' Ignore branches with cap < 1 MW
Public Const CSENS As Double = 0.001    ' Require MW flow for max action
Public Const OVRLD As Double = -0.05

Public Sub Start()
    ControlForm.Show
End Sub


' Setup and run base case loadflow (all branches)

Public Function NetCheck() As Boolean
    Dim mism() As Double, bvang() As Double
    Dim dup As Long, i As Long, j1 As Long, j2 As Long
    Dim nmx As Long, bmx As Long, ln As String
    Dim snet() As Long, r As Long, r1 As Long, r2 As Long
    Dim st As Long, mi As Long, mm As Double
    Dim tout() As Variant, vang() As Double, bflow() As Variant
    Dim cn1str() As Variant, cn2str() As Variant, clcstr() As Variant
    Dim noderef As String, branchref As String, ctrlref As String
    
'    On Error GoTo errorhandler
    NetCheck = False
    
    st = ControlForm.Newstage("Link to nodes table")
    
    noderef = ControlForm.NodeBox.Text
    
    If Not ControlForm.RangeExists(noderef) Then
        ControlForm.StageResult st, STFAIL, "Range not found: " & noderef
        Exit Function
    End If
    
    Set ntab = New DTable
    ntab.Init noderef, Array("Zone", "Node", "Demand", "Generation", "Ext")
    ntab.GetColumn "Node", nstr
    ntab.GetColumn "Zone", zstr
    ntab.GetColumn "Generation", gen
    ntab.GetColumn "Demand", dem
    ntab.GetColumn "Ext", ext
    nmx = UBound(nstr, 1)
    
    ControlForm.StageResult st, STPASS, "Count " & CStr(nmx)
     
    'Create an index for node name and check unique
    
    st = ControlForm.Newstage("Unique node names")
    
    Set nind = New DIndex
    dup = nind.MkIndex(nstr, 1)
    If dup <= nmx Then
        ControlForm.StageResult st, STFAIL, "Duplicate node name " & nstr(dup, 1)
        Exit Function
    Else
        ControlForm.StageResult st, STPASS, ""
    End If
    
    st = ControlForm.Newstage("Link to branches table")
    
    branchref = ControlForm.BranchBox.Text
    
    If Not ControlForm.RangeExists(branchref) Then
        ControlForm.StageResult st, STFAIL, "Range not found: " & branchref
        Exit Function
    End If
    
    Set btab = New DTable
    btab.Init branchref, Array("Node1", "Node2", "Code", "X", "Cap")
    btab.GetColumn "Node1", n1str
    btab.GetColumn "Node2", n2str
    btab.GetColumn "Code", lcstr
    btab.GetColumn "X", xval
    btab.GetColumn "Cap", bcap
    ControlForm.StageResult st, STPASS, "Count " & CStr(UBound(n1str, 1))
    
    st = ControlForm.Newstage("Find referenced nodes")
    
    bmx = UBound(n1str, 1)
    ReDim bn1(1 To bmx) As Long
    ReDim bn2(1 To bmx) As Long
    ReDim bout(1 To bmx) As Boolean
    
    Set bind = New DIndex
    bind.Init
'    ControlForm.CctListBox.Clear
    
    For i = 1 To bmx
        j1 = nind.Lookup(n1str(i, 1))
        j2 = nind.Lookup(n2str(i, 1))
        ln = LineName(i)
        
        If j1 > nmx Then
            ControlForm.StageResult st, STFAIL, "Node " & n1str(i, 1) & " not found in line " & ln
            Exit Function
        End If
        If j2 > nmx Then
            ControlForm.StageResult st, STFAIL, "Node " & n2str(i, 1) & " not found in line " & ln
            Exit Function
        End If
        If Not bind.AddKey(i, ln) Then
            ControlForm.StageResult st, STFAIL, "Line name " & ln & " not unique"
            Exit Function
        End If
'        ControlForm.CctListBox.AddItem ln
    Next i
    
    ReDim bctrl(1 To UBound(n1str, 1)) As Long
    ControlForm.StageResult st, STPASS, ""
    
    st = ControlForm.Newstage("Link to controls table")
    
    ctrlref = ControlForm.CtrlBox.Text
    Set ctab = New DTable
    ctab.Init ctrlref, Array("Node1", "Node2", "Code", "MinCtrl", "MaxCtrl", "Type")
    ctab.GetColumn "Node1", cn1str
    ctab.GetColumn "Node2", cn2str
    ctab.GetColumn "Code", clcstr
    ctab.GetColumn "MinCtrl", cminc
    ctab.GetColumn "MaxCtrl", cmaxc
    ctab.GetColumn "Cost", ccost
    ctab.GetColumn "Type", ctype
    ControlForm.StageResult st, STPASS, "Count " & CStr(UBound(cmaxc, 1))
    
    st = ControlForm.Newstage("Find control elements")
    
    ReDim injmax(1 To UBound(cmaxc, 1)) As Double
    ReDim cbid(1 To UBound(cmaxc, 1)) As Long
    
    For i = 1 To UBound(cmaxc, 1)
        ln = cn1str(i, 1) & "-" & cn2str(i, 1) & ":" & clcstr(i, 1)
        j1 = bind.Lookup(ln)
        If j1 > bmx Then
            ControlForm.StageResult st, STFAIL, "QB " & ln & " not found in branch table"
            Exit Function
        End If
        cbid(i) = j1
        bctrl(j1) = i   ' Link branch to control
        
        Select Case ctype(i, 1)
            Case "QB"
                injmax(i) = PUCONV * cmaxc(i, 1) / xval(j1, 1)
        
            Case "HVDC"
                injmax(i) = cmaxc(i, 1)
                
            Case Else
                ControlForm.StageResult st, STFAIL, "Unknown control type " & ctype(i, 1) & "found at " & ln
                Exit Function
        End Select
    Next i
    ControlForm.StageResult st, STPASS, ""
    
    st = ControlForm.Newstage("Network connected")
    
    ReDim snet(1 To nmx)
    Subnets snet
    
    For i = 1 To nmx
        If snet(i) <> 1 Then
            ControlForm.StageResult st, -1, "Disconnected network detected at node " & nstr(i, 1)
            Exit Function
        End If
    Next i
    ControlForm.StageResult st, STPASS, ""
    
    st = ControlForm.Newstage("Node order")
    
    nord.Init nind, n1str, n2str, xval
    
    For i = 1 To UBound(xval, 1)
        bn1(i) = nord.NodePos(nind.Lookup(n1str(i, 1)))
        bn2(i) = nord.NodePos(nind.Lookup(n2str(i, 1)))
    Next i
    ControlForm.StageResult st, STPASS, "Av non-zero per row = " & Format(nord.nz / nind.Count, "0.00") & ", Fill-in = " & Format((nord.fz - nord.nz) / nord.nz, "0.0%")
    
    st = ControlForm.Newstage("Build admittance matrix and factorise")
    
    Set admat = AdmittanceMat()
    
    Set ufac = New SolveLinSym
    ufac.Init admat, False
    ControlForm.StageResult st, STPASS, "Reference node = " & nstr(nord.NodeId(nord.nn), 1)
    
    st = ControlForm.Newstage("Base case load flow (ac part)")
    
    BaseTransfers btfr
    mism = btfr
    ufac.Solve btfr, bvang
    CalcACFlows bvang, bflow, mism
    
    If btab.FindCol("BFlow") >= 0 Then
        btab.PutColumn "BFlow", bflow
    End If
    
    If ntab.FindCol("BVang") >= 0 Then
        CalcNodeCol bvang, tout
        ntab.PutColumn "BVang", tout
    End If
    
    If ntab.FindCol("BMismatch") >= 0 Then
        CalcNodeCol mism, tout
        ntab.PutColumn "BMismatch", tout
    End If
    
    mm = mism(nord.nn)
    ControlForm.StageResult st, STPASS, "No control mismatch at ref node " & Format(mm, "0.0")
    
    st = ControlForm.Newstage("Calculate control sensitivities")
    
    ReDim cvang(UBound(cmaxc, 1) + 1)
    cvang(0) = bvang
    For i = 1 To UBound(cmaxc, 1)
        CtrlSensitivity i, cvang
        If ntab.FindCol("CVang" & CStr(i)) >= 0 Then
            vang = cvang(i)
            CalcNodeCol vang, tout
            ntab.PutColumn "CVang" & CStr(i), tout
        End If
    Next i
    
    BuildOptimiser
    
    ControlForm.StageResult st, STPASS, ""
        
    NetCheck = True
    Exit Function
    
errorhandler:
    ControlForm.StageResult st, STFAIL, "Unexpected error num=" & Err.Number
    
    NetCheck = False
End Function

' Returns subnet id for all nodes

Private Sub Subnets(snet() As Long)
    Dim i As Long, j1 As Long, j2 As Long
    Dim chng As Boolean
    
    For i = 1 To UBound(snet)
        snet(i) = i
    Next i
    
    Do
        chng = False
        For i = 1 To UBound(n1str, 1)
            j1 = nind.Lookup(n1str(i, 1))
            j2 = nind.Lookup(n2str(i, 1))
            
            If snet(j1) < snet(j2) Then
                snet(j2) = snet(j1)
                chng = True
            ElseIf snet(j2) < snet(j1) Then
                snet(j1) = snet(j2)
                chng = True
            End If
        Next i
    Loop While chng
End Sub

' Set up admittance matrix
' Default lf=true selects most non-zero row as reference

Private Function AdmittanceMat(Optional lf As Boolean = True) As SparseMatrix
    Dim i As Long, nupb As Long, nr As Long, nc As Long
    Dim y As Double
    Dim adm As New SparseMatrix
    
    If lf Then
        nupb = nord.nn - 1  ' exclude reference node
    Else
        nupb = nord.nn
    End If
    
    adm.Init nupb, nupb, nord.fz / (nupb + 1)
    
    For i = 1 To UBound(xval, 1)
        If xval(i, 1) <> 0# Then
            y = PUCONV / xval(i, 1)
    
            If bn1(i) > bn2(i) Then ' ensure upper diagonal
                nr = bn2(i)
                nc = bn1(i)
            Else
                nr = bn1(i)
                nc = bn2(i)
            End If
        
            adm.Addin nr, nr, y
            If nc <= nupb Then      ' ensure not reference
                adm.Addin nc, nc, y
                adm.Addin nr, nc, -y
            End If
        End If
    Next i
    
    Set AdmittanceMat = adm
End Function

' Setup transfer vector

Public Sub BaseTransfers(tvec() As Double)
    Dim i As Long, p As Long
    Dim vupb As Long
    
    vupb = UBound(gen, 1) - 1      ' rebase
    ReDim tvec(vupb) As Double
    
    For i = 1 To UBound(gen, 1)     ' includes transfer at reference node & hvdc nodes
        p = nord.NodePos(i)
        tvec(p) = gen(i, 1) - dem(i, 1)
    Next i
End Sub

' Calculate ac flows and associated mismatches
' call with mism = transfers

Public Sub CalcACFlows(vang() As Double, flow() As Variant, mism() As Double)
    Dim i As Long, nupb As Long
    Dim y As Double, v1 As Double, v2 As Double, f As Double
    Dim sp() As Variant, c As Long
    
    ReDim flow(1 To UBound(xval, 1), 1 To 1) As Variant
    
    nupb = nord.nn
    
    For i = 1 To UBound(xval, 1)
        
        If xval(i, 1) <> 0# And Not bout(i) Then       ' ac branch
            
            y = PUCONV / xval(i, 1)
            v1 = vang(bn1(i))
            v2 = vang(bn2(i))
            f = (v1 - v2) * y
        
            mism(bn1(i)) = mism(bn1(i)) - f
            mism(bn2(i)) = mism(bn2(i)) + f
            flow(i, 1) = f
        End If
    Next i
End Sub

' Calculate all flows and mismatches from vangs and setpoints
' call with mism = transfers

Public Sub CalcFlows(vang() As Double, sp() As Variant, flow() As Variant, mism() As Double)
    Dim i As Long, nupb As Long
    Dim y As Double, v1 As Double, v2 As Double, f As Double
    Dim c As Long
    
    ReDim flow(1 To UBound(xval, 1), 1 To 1) As Variant
    
    nupb = nord.nn
    
    For i = 1 To UBound(xval, 1)
        If bout(i) Then
            f = 0#
            
        ElseIf bctrl(i) = 0 Then       ' Uncontrolled ac branch
            
            y = PUCONV / xval(i, 1)
            v1 = vang(bn1(i))
            v2 = vang(bn2(i))
            f = (v1 - v2) * y
        Else
            
            c = bctrl(i)
            
            Select Case ctype(c, 1)
                Case "QB"
                    y = PUCONV / xval(i, 1)
                    v1 = vang(bn1(i))
                    v2 = vang(bn2(i))
                    f = (v1 - v2 + sp(c, 1)) * y
                    
                Case "HVDC"
                    f = sp(c, 1)
                    
                Case Else
                    Err.Raise vbError + 610, , "Unknown control type"
            End Select
        End If
            
        mism(bn1(i)) = mism(bn1(i)) - f
        mism(bn2(i)) = mism(bn2(i)) + f
        flow(i, 1) = f
    Next i
    
End Sub

Public Sub CalcNodeCol(nquant() As Double, tquant() As Variant)
    Dim nupb As Long
    Dim i As Long, j As Long
    
    nupb = UBound(nstr, 1)
    ReDim tquant(1 To nupb, 1 To 1) As Variant
    
    For i = 1 To nupb
        j = nord.NodePos(i)
        tquant(i, 1) = nquant(j)
    Next i
End Sub

Public Sub CalcBranchCol(nquant() As Double, tquant() As Variant)
    Dim nupb As Long
    Dim i As Long
    
    nupb = UBound(xval, 1)
    ReDim tquant(1 To nupb, 1 To 1) As Variant
    
    For i = 1 To nupb
        If nquant(i) < 9999# Then
            tquant(i, 1) = nquant(i)
        End If
    Next i
End Sub

' Calculate total vang using control setpoints
' iasf scales the interconnection allowance used in ctrlva(0)

Public Sub CalcVang(sp() As Variant, iasf As Double, ctrlva() As Variant, vang() As Double)
    Dim i As Long, j As Long, sf2 As Double
    Dim tv() As Double, n As Long
    
    n = UBound(ctrlva, 1)
    
    vang = ctrlva(0)    ' base vangs
    
    If iasf <> 0# And IsArray(ctrlva(n)) Then
        tv = ctrlva(n)
        For i = 0 To UBound(vang, 1)
            vang(i) = vang(i) + tv(i) * iasf
        Next i
    End If
    
    For i = 1 To UBound(sp, 1)
        If sp(i, 1) <> 0# And IsArray(ctrlva(i)) Then
            tv = ctrlva(i)
            sf2 = sp(i, 1) / cmaxc(i, 1)
            For j = 0 To UBound(vang, 1)
                vang(j) = vang(j) + tv(j) * sf2
            Next j
        End If
    Next i
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

Public Function LineName(i As Long) As String
    LineName = n1str(i, 1) & "-" & n2str(i, 1) & ":" & lcstr(i, 1)
End Function

'Calc intact network sensitivities to max controls

Public Sub CtrlSensitivity(c As Long, cvang() As Variant)
    Dim tvec() As Double, vang() As Double
    Dim i As Long, j As Long
    Dim vupb As Long
    
    vupb = UBound(gen, 1) - 1      ' rebase
    ReDim tvec(vupb) As Double
    
    j = cbid(c)
    
    If bn1(j) <= nord.nn Or bn2(j) <= nord.nn Then
        tvec(bn1(j)) = -injmax(c)
        tvec(bn2(j)) = injmax(c)
        ufac.Solve tvec, vang
        cvang(c) = vang
    Else
        cvang(c) = Empty
    End If
End Sub

' Calculate cct free capacity

Public Sub CalcFree(flow() As Variant, free() As Double, ord() As Long)
    Dim i As Long, j As Long
    Dim n As Long, si As Double
    
    n = UBound(flow, 1)
    ReDim free(1 To n) As Double
    ReDim ord(1 To n) As Long
    
    For i = 1 To n
        If flow(i, 1) < 0# Then
            si = -1#
        Else
            si = 1#
        End If
        If bcap(i, 1) > SCAP Then
            free(i) = (bcap(i, 1) - flow(i, 1) * si)
        Else
            free(i) = 99999#
        End If
        ord(i) = i
    Next i
    MergeSortFlt free, ord, n
End Sub

Public Sub CalcFreeDir(flow() As Variant, dirn() As Variant, free() As Double, ord() As Long)
    Dim i As Long, j As Long
    Dim n As Long, si As Double
    
    n = UBound(flow, 1)
    ReDim free(1 To n) As Double
    ReDim ord(1 To n) As Long
    
    For i = 1 To n
        If dirn(i, 1) < 0# Then
            si = -1#
        Else
            si = 1#
        End If
        If bcap(i, 1) > SCAP Then
            free(i) = (bcap(i, 1) - flow(i, 1) * si)
        Else
            free(i) = 99999#
        End If
        ord(i) = i
    Next i
    MergeSortFlt free, ord, n
End Sub

' Optimise the planned transfer condition

Public Function RunPlannedTransfer(fname As String, cva() As Variant, pflow() As Variant, Optional save As Boolean = True) As Long
    Dim st As Long, i As Long
    Dim mi As Long, mm As Double, f As Double
    Dim r1 As Long, r2 As Long
    Dim csp() As Variant, tout() As Variant
    Dim mism() As Double, vang() As Double
    Dim nneg As Long, ord() As Long, free() As Double
    
    st = ControlForm.Newstage("Balance hvdc nodes")
    
    ResetLP
    r1 = ctrllp.SolveLP(r2)
    
    If r1 = lpOptimum Then
        ControlForm.StageResult st, STPASS, "Control cost = " & Format(ctrllp.Slack(bounzc.Id) - ctrllp.Objective, "0.00")
    Else
        If r1 = lpInfeasible Then
            ControlForm.StageResult st, STFAIL, "Unresolvable constraint " & ctrllp.cname(r2)
        Else
            ControlForm.StageResult st, STFAIL, "Unknown optimiser fail"
        End If
    End If
    
    st = ControlForm.Newstage("Minimum control load flow")
    
    ' Calc loadflow
    CalcSetPoints csp
    mism = btfr
    CalcVang csp, 0#, cva, vang
    CalcFlows vang, csp, pflow, mism
    CalcFree pflow, free, ord
    mi = MaxMismatch(mism)
    mm = mism(mi)
    If Abs(mm) < 0.1 Then
        ControlForm.StageResult st, STPASS, "Max mismatch " & Format(mm, "0.0e+00") & " at " & nstr(nord.NodeId(mi), 1)
    Else
        ControlForm.StageResult st, STWARN, "Max mismatch " & Format(mm, "0.0") & " at " & nstr(nord.NodeId(mi), 1)
    End If
    
    If r1 <> lpOptimum Then
    
        ' output diagnosis results
        
        ctab.PutColumn fname, csp
        btab.PutColumn fname, pflow
        If ntab.FindCol("Mismatch") >= 0 Then
            CalcNodeCol mism, tout
            ntab.PutColumn "Mismatch", tout
        End If
        If btab.FindCol("Free") >= 0 Then
            CalcBranchCol free, tout
            btab.PutColumn "Free", tout
        End If
        ReportOverloads free, ord
        
        RunPlannedTransfer = r1
        Exit Function
    End If
    
    st = ControlForm.Newstage("Resolve AC constraints")
    
    Do
        i = 1
        While free(ord(i)) < OVRLD
            Debug.Print LineName(ord(i)), Format(free(ord(i)), "0.0")
            PopulateConstraint ord(i), free(ord(i)), pflow(ord(i), 1), csp, 0#, cvang, True
            i = i + 1
        Wend
        
        If i = 1 Then
            Exit Do
        End If
        
        r1 = ctrllp.SolveLP(r2)
        
        CalcSetPoints csp
        mism = btfr
        CalcVang csp, 0#, cva, vang
        CalcFlows vang, csp, pflow, mism
        CalcFree pflow, free, ord
        
        If r1 <> lpOptimum Then
            If r1 = lpInfeasible Then
                ControlForm.StageResult st, STFAIL, "Unresolvable constraint " & ctrllp.cname(r2)
            Else
                ControlForm.StageResult st, STFAIL, "Unknown optimiser fail"
            End If
            
            ' output diagnosis results
            
            ctab.PutColumn fname, csp
            btab.PutColumn fname, pflow
            If ntab.FindCol("Mismatch") >= 0 Then
                CalcNodeCol mism, tout
                ntab.PutColumn "Mismatch", tout
            End If
            If ntab.FindCol("Vang") >= 0 Then
                CalcNodeCol vang, tout
                ntab.PutColumn "Vang", tout
            End If
            If btab.FindCol("Free") >= 0 Then
                CalcBranchCol free, tout
                btab.PutColumn "Free", tout
            End If
            ReportConstraints STWARN
            ReportOverloads free, ord
            RunPlannedTransfer = r1
            Exit Function
        End If
    Loop
    ControlForm.StageResult st, STPASS, "Control cost = " & Format(ctrllp.Slack(bounzc.Id) - ctrllp.Objective, "0.00")
        
    st = ControlForm.Newstage("Planned transfer load flow")
            
    If save Then
        ctab.PutColumn fname, csp
        btab.PutColumn fname, pflow
        If ntab.FindCol("Mismatch") >= 0 Then
            CalcNodeCol mism, tout
            ntab.PutColumn "Mismatch", tout
        End If
        If ntab.FindCol("Vang") >= 0 Then
            CalcNodeCol vang, tout
            ntab.PutColumn "Vang", tout
        End If
        If btab.FindCol("Free") >= 0 Then
            CalcBranchCol free, tout
            btab.PutColumn "Free", tout
        End If
    End If
    
    mi = MaxMismatch(mism)
    mm = mism(mi)
    If Abs(mm) < 0.1 Then
        ControlForm.StageResult st, STPASS, "Max mismatch " & Format(mm, "0.0e+00") & " at " & nstr(nord.NodeId(mi), 1)

    Else
        ControlForm.StageResult st, STWARN, "Max mismatch " & Format(mm, "0.0") & " at " & nstr(nord.NodeId(mi), 1)
    End If
    
    ReportConstraints
    ReportOverloads free, ord
    
    RunPlannedTransfer = r1
End Function

Public Sub RunLoadFlow(fname As String, cva() As Variant, csp() As Variant, pflow() As Variant, Optional save As Boolean = True)
    Dim mism() As Double, vang() As Double
    Dim tout() As Variant
    Dim mi As Long, mm As Double, st As Long
    Dim i As Long, free() As Double, ord() As Long
    
    st = ControlForm.Newstage("Run loadflow " & fname)
    
    mism = btfr
    CalcVang csp, 0#, cva, vang
    CalcFlows vang, csp, pflow, mism
    CalcFree pflow, free, ord
    
    mi = MaxMismatch(mism)
    mm = mism(mi)
    If Abs(mm) < 0.1 Then
        ControlForm.StageResult st, STPASS, "Max mismatch " & Format(mm, "0.0e+00") & " at " & nstr(nord.NodeId(mi), 1)
    Else
        ControlForm.StageResult st, STWARN, "Max mismatch " & Format(mm, "0.0") & " at " & nstr(nord.NodeId(mi), 1)
    End If
            
    If save Then
        ctab.PutColumn fname, csp
        btab.PutColumn fname, pflow
        If ntab.FindCol("Vang") >= 0 Then
            CalcNodeCol vang, tout
            ntab.PutColumn "Vang", tout
        End If
        If ntab.FindCol("Mismatch") >= 0 Then
            CalcNodeCol mism, tout
            ntab.PutColumn "Mismatch", tout
        End If
        If btab.FindCol("Free") >= 0 Then
            CalcBranchCol free, tout
            btab.PutColumn "Free", tout
        End If
    End If
    ReportOverloads free, ord
    
End Sub

Public Sub ReportOverloads(free() As Double, ord() As Long)
    Dim i As Long, st As Long
    
    i = 1
    While free(ord(i)) < OVRLD
        st = ControlForm.Newstage(LineName(ord(i)))
        ControlForm.StageResult st, STFAIL, "Overload " & Format(free(ord(i)), "0.0") & " on capacity " & Format(bcap(ord(i), 1), "0.0")
        i = i + 1
    Wend
End Sub

Public Sub DebugListOverloads(free() As Double, ord() As Long)
    Dim i As Long, st As Long
    
    i = 1
    While free(ord(i)) < 0#
        Debug.Print LineName(ord(i)), Format(free(ord(i)), "0.0") & " on capacity " & Format(bcap(ord(i), 1), "0.0")
        i = i + 1
    Wend
End Sub


' Process base case

Public Sub RunBaseCase(fname As String, setpnm As String)
    Dim ccts() As Long
    Dim n As Long, st As Long
    Dim pflow() As Variant
    
    st = ControlForm.Newstage("Check network")
    
    If Not NetCheck() Then
        ControlForm.StageResult st, STFAIL, "Base network not valid"
        Exit Sub
    Else
        ControlForm.StageResult st, STPASS, ""
    End If
    
    ReDim bout(UBound(xval, 1)) As Boolean
    
    InitCctList
    
    If setpnm = "Auto" Then
        RunPlannedTransfer fname, cvang, ipflow
    Else
        ctab.GetColumn setpnm, csp
        RunLoadFlow fname, cvang, csp, pflow
    End If
       
End Sub


' Process a trip case

Public Sub RunTrip(fname As String, setpnm As String)
    Dim ccts() As Long
    Dim n As Long, st As Long
    Dim sm() As Double, tcva() As Variant
    Dim pflow() As Variant, csp() As Variant
    
    st = ControlForm.Newstage("Check network")
    
    If Not NetCheck() Then
        ControlForm.StageResult st, STFAIL, "Base network not valid"
        Exit Sub
    Else
        ControlForm.StageResult st, STPASS, ""
    End If
    
    st = ControlForm.Newstage("Setup trip " & fname)
    
    n = SelectedCcts(ccts)
    
    If n = 0 Then
        ControlForm.StageResult st, STFAIL, "No trip circuits selected"
        Exit Sub
    End If
    
    If Not TripVectors(ccts, tcva) Then
        If Countac(ccts) > 0 Then
            ControlForm.StageResult st, STFAIL, "Invalid trip - node disconnected?"
            Exit Sub
        End If
    End If
    ControlForm.StageResult st, STPASS, CStr(n) & " circuits"
    
    If setpnm = "Auto" Then
        RunPlannedTransfer fname, tcva, pflow
    Else
        ctab.GetColumn setpnm, csp
        RunLoadFlow fname, tcva, csp, pflow
    End If
End Sub
