Attribute VB_Name = "Trips"
Option Explicit
Option Base 0

' Creat n-1 and n-2 Trips from boundary circuits

Public Sub InitTripList()
    Dim ln As Variant, ln2 As Variant
    Dim i As Long, j As Long
    Dim tn As Variant
    Dim pnode As Node
    
    With ControlForm.TreeView1
        .Nodes.Clear
        tripmax = 0
        
        For Each ln In boundccts
            tn = NewTrip("S")
            Newcct tn, ln
        Next ln
        
        For i = 1 To boundccts.Count - 1
            For j = i + 1 To boundccts.Count
                tn = NewTrip("D")
                
                ln = boundccts.Item(i)
                ln2 = boundccts.Item(j)
                Newcct tn, ln
                Newcct tn, ln2
            Next j
        Next i
    End With
End Sub

' Initialise cct listbox

Public Sub InitCctList()
    Dim i As Long
    Dim ln As String
    
    ControlForm.CctListBox.Clear
    
    For i = 1 To UBound(n1str, 1)
        
        ln = LineName(i)
        
        ControlForm.CctListBox.AddItem ln
    Next i
End Sub


Public Function SelectedCcts(ccts() As Long) As Long
    Dim i As Long
    Dim n As Long
    Dim j As Long
    Dim ln As String
    
    n = 0
    ReDim bout(1 To UBound(xval, 1)) As Boolean
    With ControlForm.CctListBox
        For i = 0 To UBound(n1str, 1) - 1
            If .Selected(i) Then
                ln = .List(i)
                j = bind.Lookup(ln)
                ReDim Preserve ccts(n) As Long
                ccts(n) = j
                bout(j) = True
                n = n + 1
            End If
        Next i
    End With
    SelectedCcts = n
End Function

Public Function NewTrip(Optional ttype As String = "T") As Variant
    Dim pnode As Node
    Dim tn As Variant
    
    tripmax = tripmax + 1
    tn = ttype & CStr(tripmax)
    
    With ControlForm.TreeView1
        Set pnode = .Nodes.Add(key:=tn, Text:=tn)
        pnode.Expanded = True
'        pnode.Selected = True
    End With
    
    NewTrip = tn
End Function

Public Function Newcct(tripname As Variant, cctname) As Variant

    On Error GoTo errorhandler

    With ControlForm.TreeView1
        .Nodes.Add relative:=tripname, relationship:=tvwChild, key:=tripname & cctname, Text:=cctname
    End With
    
    Newcct = tripname & cctname
    Exit Function
    
errorhandler:
    MsgBox "Duplicate circuit " & cctname & " in " & tripname
End Function


Public Sub SaveTrips()
    Dim i As Long
    Dim mnodes As Nodes
    Dim pnode As Node
    Dim cnode As Node
    Dim tview As TreeView
    Dim rn As Variant
    
    If Not boundchk Then
        Exit Sub
    End If
    
    rn = ""
    Set tview = ControlForm.TreeView1
    Set mnodes = tview.Nodes
    If mnodes.Count > 0 Then
        Set pnode = mnodes.Item(1).Root.FirstSibling
        While pnode.Children <= 0 And Not pnode.Next Is Nothing
            Set pnode = pnode.Next
        Wend
        If Not pnode Is Nothing Then
            rn = left(pnode.Text, 1) & ">" & ListCcts(pnode)
        End If
        
        While Not pnode.Next Is Nothing
            Set pnode = pnode.Next
            If pnode.Children > 0 Then
                rn = rn & ";" & left(pnode.Text, 1) & ">" & ListCcts(pnode)
            End If
        Wend
    End If
    triptab.PutCell 1, boundnm, rn
End Sub

' Find first non-empty trip

Public Function firsttrip() As Node
    Dim tview As TreeView
    Dim mnodes As Nodes
    Dim pnode As Node
    
    Set tview = ControlForm.TreeView1
    Set mnodes = tview.Nodes
    
    If mnodes.Count > 0 Then
        Set pnode = mnodes.Item(1).Root.FirstSibling
        While pnode.Children <= 0 And Not pnode.Next Is Nothing
            Set pnode = pnode.Next
        Wend
    End If
    Set firsttrip = pnode
End Function

' Find next non-empty trip

Public Function NextTrip(trip As Node) As Node
    Dim pnode As Node
    
    Set pnode = trip.Next
    
    If Not pnode Is Nothing Then
        While pnode.Children <= 0 And Not pnode.Next Is Nothing
            Set pnode = pnode.Next
        Wend
    End If
    
    Set NextTrip = pnode
End Function



Public Function ListCcts(pnode As Node) As Variant
    Dim rn As Variant
    Dim cnode As Node
    
    Set cnode = pnode.Child
    rn = cnode.Text
    
    While Not cnode.Next Is Nothing
        Set cnode = cnode.Next
        rn = rn & "," & cnode.Text
    Wend
    ListCcts = rn
End Function

' Load string with trips separated by ;
' First char of trip is type and ccts separated by ,

Public Sub LoadTripList(tripnames As Variant)
    Dim triplist As Variant, cctlist As Variant
    Dim i As Long, j As Long, tn As Variant, t As String
      
    tripmax = 0
    ControlForm.TreeView1.Nodes.Clear
    
    triplist = Split(tripnames, ";")
    
    For i = 0 To UBound(triplist, 1)
        t = left(triplist(i), 1)
        tn = NewTrip(t)
        cctlist = Split(right(triplist(i), Len(triplist(i)) - 2), ",")
        For j = 0 To UBound(cctlist, 1)
            Newcct tn, cctlist(j)
        Next j
    Next i
    
End Sub


' A single circuit outage is simulated by paired injections at cct ends where
' injection = newflow = baseflow + deltaflow
' deltaflow = injection . vsens / x
' so injection = bflow / (1 - vsens/x)
'
' Multicct outage creates and inverts a matrix (I - Fsens)
' Inj = (I - Fsens)^-1 . Baseflow
'
' New vang = bvang + ufac.solve Inj
'
' Calculate Matrix (I-Fsens)^-1

' Find ccts in trip, flag all outages

Public Function FindTripCcts(trip As Node, ccts() As Long)
    Dim cnode As Node
    Dim n As Long
    Dim cct As Long
    
    ReDim bout(1 To UBound(xval, 1)) As Boolean
    ReDim ccts(trip.Children - 1) As Long
    
    Set cnode = trip.Child.FirstSibling
    While Not cnode Is Nothing
        cct = bind.Lookup(cnode.Text)
        bout(cct) = True
        ccts(n) = cct
        n = n + 1
        Set cnode = cnode.Next
    Wend
    FindTripCcts = n
End Function

' Count number of ac circuits

Public Function Countac(ccts() As Long) As Long
    Dim i As Long, n As Long
    
    For i = 0 To UBound(ccts)               ' count ac ccts
        If xval(ccts(i), 1) <> 0# Then
            n = n + 1
        End If
    Next i
    Countac = n
End Function


' Calc injection matrix = (I - Fsens)^-1 for ac elements
' Returns false if (I-Fsens) is singular or no ac ccts

Private Function CalcSensMat(ccts() As Long, sensmat() As Double) As Boolean
    Dim tvec() As Double
    Dim mat() As Double
    Dim res As Variant
    Dim i As Long, j As Long, n As Long
    Dim c As Long, d As Double
    
    n = Countac(ccts)
    
    If n = 0 Then
        CalcSensMat = False
        Exit Function
    End If
    
    ReDim mat(n - 1, n - 1) As Double
    ReDim sensmat(n - 1, n - 1) As Double
    
    For j = 0 To n - 1
        ReDim tvec(UBound(gen, 1) - 1) As Double
        c = ccts(j)
        tvec(bn1(c)) = 1#
        tvec(bn2(c)) = -1#
        ufac.Solve tvec, tvec
        For i = 0 To n - 1
            c = ccts(i)
            If i = j Then
                mat(i, j) = 1#
            End If
            mat(i, j) = mat(i, j) - (tvec(bn1(c)) - tvec(bn2(c))) * PUCONV / xval(c, 1)
        Next i
    Next j
    d = Excel.WorksheetFunction.MDeterm(mat)
    If Abs(d) <= lpEpsilon Then
        CalcSensMat = False
        Exit Function
    End If
    
    res = Excel.WorksheetFunction.MInverse(mat)
    
    If n = 1 Then
        sensmat(0, 0) = res(1)
    Else
        For i = 0 To n - 1
            For j = 0 To n - 1
                sensmat(i, j) = res(i + 1, j + 1)
            Next j
        Next i
    End If
    CalcSensMat = True
End Function

' Calculate trip tvang from intact ovang

Private Sub TripSolve(ccts() As Long, sensmat() As Double, ovang() As Double, tvang() As Double)
    Dim nupb As Long, n As Long
    Dim c As Long, i As Long, j As Long
    Dim f() As Double, inj() As Double
    Dim tvec() As Double
    
    
    nupb = UBound(sensmat, 1)
    ReDim f(nupb) As Double
    ReDim inj(nupb) As Double
    ReDim tvec(UBound(ovang)) As Double
    
    ' Calc original flows
    n = 0
    For i = 0 To UBound(ccts)
        c = ccts(i)
        If xval(c, 1) <> 0# Then
            f(n) = (ovang(bn1(c)) - ovang(bn2(c))) * PUCONV / xval(c, 1)
            n = n + 1
        End If
    Next i
    
    ' Calc injections
    n = 0
    For i = 0 To UBound(ccts)
        c = ccts(i)
        If xval(c, 1) <> 0# Then
            For j = 0 To nupb
                inj(n) = inj(n) + sensmat(n, j) * f(j)
            Next j
            tvec(bn1(c)) = tvec(bn1(c)) + inj(n)
            tvec(bn2(c)) = tvec(bn2(c)) - inj(n)
            n = n + 1
        End If
    Next i
    
    ufac.Solve tvec, tvec
    
    tvang = ovang
    
    For i = 0 To UBound(ovang)
        tvang(i) = tvang(i) + tvec(i)
    Next i
End Sub

' Calculate Trip base and contrl vectors from intact versions
' Return true if vectors different from base case

Public Function TripVectors(ccts() As Long, tcvang() As Variant) As Boolean
    Dim i As Long
    Dim tv() As Double, ntv() As Double
    Dim sensmat() As Double
    
    ReDim tcvang(UBound(cvang, 1)) As Variant
    
    If Not CalcSensMat(ccts, sensmat) Then ' Might be dc ccts
                
        For i = 0 To UBound(cvang, 1)
            tcvang(i) = cvang(i)
        Next i
        TripVectors = False
        Exit Function
        
    Else
        For i = 0 To UBound(cvang, 1)
            If IsArray(cvang(i)) Then
                tv = cvang(i)
                TripSolve ccts, sensmat, tv, ntv
                tcvang(i) = ntv
            Else
                tcvang(i) = cvang(i)
            End If
        Next i
    End If
    TripVectors = True
End Function
