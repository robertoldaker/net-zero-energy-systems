Attribute VB_Name = "Boundary"
'Methods of changing flow

Option Explicit
Option Base 0

Public ztab As DTable
Public zind As DIndex
Public ia As Double
Public itfr() As Double     ' interconnection nodal transfers
Public iflow() As Variant   ' interconnection ac branch flows
Public ord() As Long        ' sorted order of cct free capacities
Public boundccts As Collection
Public triptab As DTable
Public tripmax As Long
Public boundnm As String
Public boundchk As Boolean
Public pfer As Double
Public ctrllp As LP
Public bnd() As Variant
Public topx() As Double
Public topns() As Variant      ' storage for Topn single trip results
Public topnd() As Variant      ' storage for Topn dual trip results

Public Const MAXCPI As Long = 20 ' Maximum cct constraints added per iteration
Public Const DIAGNOSE As Boolean = False



' Setup boundary calc and run intact network case

Public Sub SetBound(boundref As String, boundname As String, tripref As String)
    Dim bzstr() As Variant, ivang() As Double
    Dim dup As Long, i As Long, st As Long
    Dim inout() As Variant, tout() As Variant
    Dim p As Long, z As Long, r As Long
    Dim mism() As Double, vang() As Double
    Dim kgin As Double, kdin As Double
    Dim kgout As Double, kdout As Double
    Dim nmax As Long, mi As Long, mm As Double
    Dim gin As Double, gout As Double, gins As Double, gouts As Double
    Dim din As Double, dout As Double, dins As Double, douts As Double
    Dim z1 As Long, z2 As Long, tc As Double
    Dim tn As Variant, mflow() As Variant, fname As String
    
    fname = ControlForm.Filenm()
    boundnm = boundname
    boundchk = False
        
'    On Error GoTo errorhandler
    
    st = ControlForm.Newstage("Check base load flow")
    
    If Not NetCheck() Then
        ControlForm.StageResult st, STFAIL, ""
        Exit Sub
    End If
    
    InitCctList
    
    ControlForm.StageResult st, STPASS, ""
    
    nmax = UBound(nstr, 1)
    
    st = ControlForm.Newstage("Link to boundary table")
    
    If Not ControlForm.RangeExists(boundref) Then
        ControlForm.StageResult st, STFAIL, "Range not found: " & boundref
        Exit Sub
    End If
    
    Set ztab = New DTable
    ztab.Init boundref, Array("Zone")
    ztab.GetColumn "Zone", bzstr
    
    Set zind = New DIndex
    dup = zind.MkIndex(bzstr, 1)
    
    ControlForm.StageResult st, STPASS, "Count = " & Format(UBound(bzstr, 1), "0") & " zones"
    
    st = ControlForm.Newstage("Link to triplist table")
    
    If Not ControlForm.RangeExists(tripref) Then
        ControlForm.StageResult st, STFAIL, "Range not found: " & tripref
        Exit Sub
    End If
    
    Set triptab = New DTable
    triptab.Init tripref, Array(boundname)
    ControlForm.StageResult st, STPASS, ""
    
    st = ControlForm.Newstage("Calc boundary contents")
    
    ztab.GetColumn boundname, inout
    
    gin = 0#
    gout = 0#
    din = 0#
    dout = 0#
    
    For i = 1 To nmax
        z = zind.Lookup(zstr(i, 1))
        If inout(z, 1) <> 0 Then
            gin = gin + gen(i, 1)
            din = din + dem(i, 1)
            If Not ext(i, 1) Then
                gins = gins + gen(i, 1)
                dins = dins + dem(i, 1)
            End If
        Else
            gout = gout + gen(i, 1)
            dout = dout + dem(i, 1)
            If Not ext(i, 1) Then
                gouts = gouts + gen(i, 1)
                douts = douts + dem(i, 1)
            End If
        End If
    Next i
    
    pfer = gin - din
    ia = InterconAllowance(gin, din, din + dout) ' Calc IA including external transfers (+ve number)
    If pfer < 0 Then
        ia = -ia
    End If
    
    ControlForm.StageResult st, STPASS, boundname & " base transfer = " & Format(pfer, "0.0") & " IA = " & Format(ia, "0.0")
    
    st = ControlForm.Newstage("Calc interconnection sensitivities")
    
    ReDim itfr(nmax - 1) As Double
    
    ' Scale internal gen & dem only
    
    kgin = ia / (gins + dins)
    kdin = -kgin
    kdout = ia / (gouts + douts)
    kgout = -kdout
    
    For i = 1 To nmax
        p = nord.NodePos(i)
        z = zind.Lookup(zstr(i, 1))
        
        If Not ext(i, 1) Then
            If inout(z, 1) <> 0 Then
                itfr(p) = kgin * gen(i, 1) - kdin * dem(i, 1)
            Else
                itfr(p) = kgout * gen(i, 1) - kdout * dem(i, 1)
            End If
        End If
    Next i
    
    ' Calc iflows
    
    mism = itfr
    ufac.Solve itfr, ivang
    cvang(UBound(cvang)) = ivang
    CalcACFlows ivang, iflow, mism
    mi = MaxMismatch(mism)
    
    If btab.FindCol("IFlow") >= 0 Then
        btab.PutColumn "IFlow", iflow
    End If
    
    If ntab.FindCol("IVang") >= 0 Then
        CalcNodeCol ivang, tout
        ntab.PutColumn "IVang", tout
    End If
    
    If ntab.FindCol("IMismatch") >= 0 Then
        CalcNodeCol mism, tout
        ntab.PutColumn "IMismatch", tout
    End If
    
    With ControlForm.LabBound
        .Caption = "Inside   : G " & Format(gin, "####0") & "  D " & Format(din, "####0") & Chr(10)
        .Caption = .Caption & "Outside  : G " & Format(gout, "####0") & "  D " & Format(dout, "####0") & Chr(10)
        .Caption = .Caption & "Transfer : " & Format(gin - din, "####0") & "  IA " & Format(ia, "####0")
    End With
    
    If Abs(mism(mi)) < 0.1 Then
        ControlForm.StageResult st, STPASS, "Max mismatch " & Format(mism(mi), "0.0e+00") & " at " & nstr(nord.NodeId(mi), 1)
    Else
        ControlForm.StageResult st, STWARN, "Max mismatch " & Format(mism(mi), "0.0") & " at " & nstr(nord.NodeId(mi), 1)
    End If
    
    
    st = ControlForm.Newstage("Identify boundary circuits")
    
    Set boundccts = New Collection
    tc = 0#
    ReDim bnd(1 To UBound(xval, 1), 1 To 1) As Variant
    
    For i = 1 To UBound(n1str, 1)
        z1 = zind.Lookup(zstr(nind.Lookup(n1str(i, 1)), 1))
        z2 = zind.Lookup(zstr(nind.Lookup(n2str(i, 1)), 1))
        
        If inout(z1, 1) Xor inout(z2, 1) Then
            boundccts.Add LineName(i)
            tc = tc + bcap(i, 1)
            If inout(z1, 1) <> 0 Then
                bnd(i, 1) = 1#
            Else
                bnd(i, 1) = -1#
            End If
        End If
    Next i
    If btab.FindCol("Bound") >= 0 Then
        btab.PutColumn "Bound", bnd
    End If
    ControlForm.StageResult st, STPASS, Format(boundccts.Count, "0") & " boundary circuits with total cap " & Format(tc, "0")
    
    st = ControlForm.Newstage("Get trip list")
    
     If triptab.FindCol(boundnm) = -1 Then
        InitTripList
        ControlForm.StageResult st, STWARN, "Saved triplist not found, default list of " & CStr(tripmax) & " trips created"
    Else
        tn = triptab.GetCell(1, boundnm)
        If tn = Empty Then
            InitTripList
            ControlForm.StageResult st, STWARN, "Saved triplist not found, default list of " & CStr(tripmax) & " trips created"
        Else
            LoadTripList tn
            ControlForm.StageResult st, STPASS, CStr(tripmax) & " saved trips loaded. "
        End If
    End If
    
    st = ControlForm.Newstage("Intact network " & boundnm)
    
    ' Run intact boundary
    
    ReDim bout(UBound(xval, 1)) As Boolean

    
    RunBoundaryOptimiser fname & boundnm, cvang, mflow
    
    boundchk = True
    Exit Sub
    
errorhandler:
    ControlForm.StageResult st, STFAIL, "Unexpected error num=" & Err.Number
    boundchk = False
    
End Sub

Private Sub InitTopN(n As Long)
    Dim i As Long, j As Long, c As Long
       
    c = UBound(cbid, 1)
    

    ReDim topns(1 To n + 1, 1 To 4 + c) As Variant
    ReDim topnd(1 To n + 1, 1 To 4 + c) As Variant

    TitleTopN topns
    TitleTopN topnd
End Sub

Private Sub TitleTopN(topn() As Variant)
    Dim i As Long, n As Long
    
    n = UBound(topn, 1)
    
    topn(1, 1) = "Surplus"
    topn(1, 2) = "Capacity"
    topn(1, 3) = "Trip"
    topn(1, 4) = "Lim Cct"
    
    For i = 1 To UBound(cbid, 1)
        topn(1, i + 4) = lcstr(cbid(i), 1)
    Next i
    
    For i = 2 To n
        topn(i, 1) = XLRG
    Next i
End Sub

Public Sub RecordTop(topn() As Variant, surplus As Double, bfer As Double, tripnm As String, limcct As String)
    Dim i As Long, j As Long, k As Long
    Dim t As Long, n As Long
    
    n = UBound(topn, 1)
    
    For i = 2 To n
        If surplus < topn(i, 1) Then
            Exit For
        End If
    Next i
    
    If i <= n Then
        For j = n To i + 1 Step -1
            For k = 1 To UBound(topn, 2)
                topn(j, k) = topn(j - 1, k)
            Next k
        Next j
        topn(i, 1) = surplus
        topn(i, 2) = bfer
        topn(i, 3) = tripnm
        topn(i, 4) = limcct
        For k = 1 To UBound(csp, 1)
            topn(i, k + 4) = csp(k, 1)
        Next k
    End If
End Sub


Public Sub SaveTopn(fname As String)
    Dim ws As Worksheet
    Dim sr As Range, dr As Range
    
    Set ws = GetWS(fname)
    
    Set sr = ws.Range("A3")
    
    sr.Value = "Single circuit trips"
    
    sr.Offset(2).Resize(UBound(topns, 1), UBound(topns, 2)) = topns
    
    Set dr = sr.Offset(4 + UBound(topns, 1))
    
    dr.Value = "Dual circuit trips"
    
    dr.Offset(2).Resize(UBound(topnd, 1), UBound(topnd, 2)) = topnd
    
End Sub


Public Function GetWS(fname As String) As Worksheet
    Dim ws As Worksheet
    
    On Error GoTo errorhandler
    
    Set ws = Excel.Worksheets(fname)

    Set GetWS = ws
    Exit Function
    
errorhandler:
    Set ws = Excel.Worksheets.Add
    ws.name = fname
    
    Set GetWS = ws
End Function

Private Function InterconAllowance(gin As Double, din As Double, dtot As Double) As Double
    Dim x As Double, t As Double, y As Double
    
    If din < 0# Then
        x = gin - din
    Else
        x = gin + din
    End If
    
    x = 0.5 * x / dtot
    t = 1# - ((x - 0.5) / 0.5415) ^ 2
    y = Sqr(t) * 0.0633 - 0.0243
    
    InterconAllowance = y * dtot
End Function



' Calculate base transfer plus interconnection
' sf scales interconnection allowance used in itfr

Private Sub CalcTransfers(sf As Double, tfr() As Double)
    Dim i As Long
    
    tfr = btfr
    
    For i = 0 To UBound(tfr)
        tfr(i) = tfr(i) + sf * itfr(i)
    Next i
End Sub


Public Function RunBoundaryOptimiser(fname As String, cva() As Variant, mflow() As Variant, Optional save As Boolean = True) As Long
    Dim st As Long, i As Long, cct As Long, iter As Long
    Dim mi As Long, mm As Double, f As Double
    Dim r1 As Long, r2 As Long, rs As Long
    Dim pflow() As Variant, tout() As Variant
    Dim mism() As Double, vang() As Double
    Dim nneg As Long, ord() As Long, free() As Double
    Dim xfer As Double, sf As Double, xn As Long
    Dim st1 As Long, ccts As String
    
    st = ControlForm.Newstage("Run planned transfer")

    r1 = RunPlannedTransfer(fname, cva, pflow, False)
    
    If r1 = lpOptimum Then
        ControlForm.StageResult st, STPASS, ""
    Else
        ControlForm.StageResult st, STFAIL, "Planned transfer failed"
        RunBoundaryOptimiser = r1
        Exit Function
    End If
    
    st = ControlForm.Newstage("Optimise " & boundnm & " boundary cct limits only")
    
    ' Add boundary circuit constraints (mflow is boundary sensitivity)
    
    xn = UBound(cva, 1)
    
    CalcSetPoints csp
    mism = itfr
    vang = cva(UBound(cva))
    CalcACFlows vang, mflow, mism
    CalcFreeDir pflow, mflow, free, ord
    
    For i = 1 To boundccts.Count
        cct = bind.Lookup(boundccts.Item(i))
        If CctSensitivity(cct, xn, cva) <> 0# Then  ' sensitive to interconnection?
            PopulateConstraint cct, free(cct), mflow(cct, 1), csp, 0#, cva, False
        End If
    Next i
    
    r1 = ctrllp.SolveLP(r2)
    
    ' Calc loadflow
    xfer = BoundCap()
    sf = Abs(xfer / ia)
    CalcSetPoints csp
    CalcTransfers sf, mism
    CalcVang csp, sf, cva, vang
    CalcFlows vang, csp, mflow, mism
    CalcFree mflow, free, ord
    
    If r1 = lpOptimum Then
        ControlForm.StageResult st, STPASS, "Boundcap = " & Format(pfer + Sgn(pfer) * xfer, "0.0") & " Control cost = " & Format(ctrllp.Slack(bounzc.Id) - ctrllp.Objective, "0.00")
    ElseIf r1 = lpInfeasible Then
        If ctrllp.Slack(r2) < -0.1 Then
            ControlForm.StageResult st, STFAIL, "Unresolvable constraint " & ctrllp.cname(r2)
        Else
            r1 = lpOptimum
        End If
    Else
        ControlForm.StageResult st, STFAIL, "Unknown optimiser fail"
    End If
           
    If r1 <> lpOptimum Or DIAGNOSE Then

        st = ControlForm.Newstage("Diagnosis results")
        
        ' output diagnosis results
        
        ctab.PutColumn fname, csp
        btab.PutColumn fname, mflow
        If ntab.FindCol("Mismatch") >= 0 Then
            CalcNodeCol mism, tout
            ntab.PutColumn "Mismatch", tout
        End If
        If btab.FindCol("Free") >= 0 Then
            CalcBranchCol free, tout
            btab.PutColumn "Free", tout
        End If
        mi = MaxMismatch(mism)
        mm = mism(mi)
        If Abs(mm) < 0.1 Then
            ControlForm.StageResult st, rs, "Max mismatch " & Format(mm, "0.0e+00") & " at " & nstr(nord.NodeId(mi), 1)
        Else
            ControlForm.StageResult st, rs, "Max mismatch " & Format(mm, "0.0") & " at " & nstr(nord.NodeId(mi), 1)
        End If
        ReportConstraints STWARN
        ReportOverloads free, ord
    End If
        
    If r1 <> lpOptimum Then
        RunBoundaryOptimiser = r1
        Exit Function
    End If
    iter = 0
    
    st = ControlForm.Newstage("Optimise " & boundnm & " all cct limits")
    
    Do
        iter = iter + 1
        i = 1
        While free(ord(i)) < OVRLD And i <= MAXCPI
            cct = ord(i)
            Debug.Print LineName(cct), Format(free(cct), "0.0")
             PopulateConstraint cct, free(cct), mflow(cct, 1), csp, sf, cva, False
            i = i + 1
        Wend
        
        If i = 1 Then
            Exit Do
        End If
        
        If i = 2 Then
            st1 = ControlForm.Newstage("Iter " & CStr(iter) & " Ovrld " & LineName(cct))
        Else
            st1 = ControlForm.Newstage("Iter " & CStr(iter) & " Ovrlds " & CStr(i - 1))
        End If
    
        r1 = ctrllp.SolveLP(r2)
        
        xfer = BoundCap()
        sf = Abs(xfer / ia)
        CalcSetPoints csp
        CalcTransfers sf, mism
        CalcVang csp, sf, cva, vang
        CalcFlows vang, csp, mflow, mism
        CalcFree mflow, free, ord
        
        If r1 = lpOptimum Then
            ControlForm.StageResult st1, STPASS, "Boundcap = " & Format(pfer + Sgn(pfer) * xfer, "0.0") & " Control cost = " & Format(ctrllp.Slack(bounzc.Id) - ctrllp.Objective, "0.00")

        ElseIf r1 = lpInfeasible Or r1 = lpIters Then
            If ctrllp.Slack(r2) < -0.1 Then
                    ControlForm.StageResult st, STFAIL, "Unresolvable constraint " & ctrllp.cname(r2)
                Else
                    ctrllp.MatAltered
                    r1 = lpOptimum
                    ControlForm.StageResult st1, STPASS, "Boundcap = " & Format(pfer + Sgn(pfer) * xfer, "0.0") & " Control cost = " & Format(ctrllp.Slack(bounzc.Id) - ctrllp.Objective, "0.00")
            End If
        Else
            ControlForm.StageResult st1, STFAIL, "Unknown optimiser fail"
        End If
            
        If r1 <> lpOptimum Or DIAGNOSE Then
            st1 = ControlForm.Newstage("Diagnosis results")
            ' output diagnosis results

            ControlForm.StageResult st1, STWARN, "Boundcap = " & Format(pfer + Sgn(pfer) * xfer, "0.0")
            ctab.PutColumn fname, csp
            btab.PutColumn fname, mflow
            If ntab.FindCol("Mismatch") >= 0 Then
                CalcNodeCol mism, tout
                ntab.PutColumn "Mismatch", tout
            End If
            If btab.FindCol("Free") >= 0 Then
                CalcBranchCol free, tout
                btab.PutColumn "Free", tout
            End If
            ReportConstraints STWARN
            ReportOverloads free, ord
        End If
            
        If r1 <> lpOptimum Then
            RunBoundaryOptimiser = r1
            Exit Function
        End If
    Loop
    
    ControlForm.StageResult st, STPASS, "Control cost = " & Format(ctrllp.Slack(bounzc.Id) - ctrllp.Objective, "0.00")
        
    st = ControlForm.Newstage(boundnm & " max transfer load flow")
            
    If save Then
        ctab.PutColumn fname, csp
        btab.PutColumn fname, mflow
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
        
        PutFormula fname
    End If
    
    mi = MaxMismatch(mism)
    mm = mism(mi)
    If Abs(mm) < 0.1 Then
        ControlForm.StageResult st, STPASS, "Boundcap = " & Format(pfer + Sgn(pfer) * xfer, "0.0") & " Max mismatch " & Format(mm, "0.0e+00") & " at " & nstr(nord.NodeId(mi), 1)
    Else
        ControlForm.StageResult st, STWARN, "Boundcap = " & Format(pfer + Sgn(pfer) * xfer, "0.0") & " Max mismatch " & Format(mm, "0.0") & " at " & nstr(nord.NodeId(mi), 1)
    End If
    
    ccts = ReportConstraints()
    ReportOverloads free, ord
    
    RunBoundaryOptimiser = r1
End Function


' Process a trip case

Public Function RunBoundaryTrip(trip As Node, Optional save As Boolean = True) As Long
    Dim ccts() As Long, r1 As Long
    Dim n As Long, st As Long
    Dim sm() As Double, tbva() As Double, tcva() As Variant
    Dim mflow() As Variant, csp() As Variant
    Dim fname As String
     
    r1 = lpUnknown
    
    If boundnm = "" Then
        RunBoundaryTrip = r1
        Exit Function
    End If
    fname = ControlForm.Filenm()
    
    st = ControlForm.Newstage("Setup " & boundnm & trip.Text)
    
    n = FindTripCcts(trip, ccts)
    
    If n = 0 Then
        ControlForm.StageResult st, STFAIL, "No trip circuits selected"
        RunBoundaryTrip = r1
        Exit Function
    End If
    
    If Not TripVectors(ccts, tcva) Then
        If Countac(ccts) > 0 Then
            ControlForm.StageResult st, STFAIL, "Invalid trip - node disconnected?"
            RunBoundaryTrip = r1
            Exit Function
        End If
    End If
    
    ControlForm.StageResult st, STPASS, CStr(n) & " circuits"
    
    RunBoundaryTrip = RunBoundaryOptimiser(fname & boundnm & trip.Text, tcva, mflow, save)
       
End Function

Public Sub PutFormula(fname As String)
    Dim c As Long, i As Long
    Dim frm As String
    Dim drange As Range, frange As Range
    Dim targ As Range, tnm As String
    
    Set drange = btab.GetDataRange(fname)
    frm = "="
    
    For i = 1 To UBound(xval, 1)
        If bnd(i, 1) <> Empty Then
            Set targ = drange.Cells(i, 1)
            tnm = targ.AddressLocal
            If bnd(i, 1) < 0 Then
                frm = frm & "-" & tnm
            Else
                frm = frm & "+" & tnm
            End If
        End If
    Next i
    
    Set frange = drange.Cells(1, 1).Offset(-3, 0)
    frange.Formula = frm
End Sub

Public Sub RunAllBoundTrips()
    Dim trip As Node
    Dim tripnm As String, tripccts As String
    Dim r As Long, sing As Boolean
    Dim xfer As Double
    Dim fname As String
    Dim ntrips As Long, i As Long
    
    
    fname = ControlForm.Filenm
    
    InitTopN ControlForm.TopNBox.Value
    
    Set trip = firsttrip()
    ntrips = CountTrip(trip)
    
    While Not trip Is Nothing
    
        i = i + 1
        ControlForm.Progress 100 * i / ntrips
        
        tripnm = trip.Text
        sing = left(tripnm, 1) = "S"
        
        tripccts = tripnm & ":" & ListCcts(trip)
        
        r = RunBoundaryTrip(trip, False)
        
        If r = lpOptimum Then
            xfer = BoundCap()
            
            If sing Then
                RecordTop topns, xfer - Abs(ia), pfer + Sgn(ia) * xfer, tripccts, LimitCcts()
            Else
                RecordTop topnd, xfer - Abs(ia) * 0.5, pfer + Sgn(ia) * xfer, tripccts, LimitCcts()
            End If
        Else
            
            If sing Then
                RecordTop topns, -XLRG, 0#, tripccts, "Fail"
            Else
                RecordTop topnd, -XLRG, 0#, tripccts, "Fail"
            End If
        End If
        
        Set trip = NextTrip(trip)
    Wend
    
    SaveTopn fname & boundnm
    
End Sub

Private Function CountTrip(firsttrip As Node) As Long
    Dim n As Long
    Dim trip As Node
    
    Set trip = firsttrip
    
    While Not trip Is Nothing
        n = n + 1
        Set trip = NextTrip(trip)
    Wend
    CountTrip = n
End Function
