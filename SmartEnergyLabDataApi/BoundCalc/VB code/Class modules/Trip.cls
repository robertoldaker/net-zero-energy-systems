VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Trip"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' A trip is a list of circuits that form an outage for security assessment
' Object sorts branches into ac and dc circuits - only ac circuits need to be reflected in trip voltages
' Activate sets outage flag in relevant branches so CalcFlows shows zero flow on circuits which have been simulated as tripped in vang calculations
' TripVectors calculates base and control voltage angles for outage case
'

#Const LF = True            ' If true customise for loadflow else customise for short circuit calc

Option Explicit
Option Base 0

Public name As String       ' S = single circuit trip, D = double circuit or 2 circuit trip, T = unspecified multi-circuit
Public nbr As Long          ' upperbound of branches in trip
Public nac As Long          ' upperbound of ac branches (xval<>0)

Private ccts() As Branch

Public Property Get Circuit(n As Long) As Branch
    Set Circuit = ccts(n)
End Property

' Identify branches in tripstring (comma delimited longnames)
' Order branches so ac circuits first

Public Function Init(tripname As String, tripstring As String) As Boolean
    Dim strip As Variant
    Dim i As Long, b As Long, t As Long
    Dim bname As String, br As Branch
    
    Init = True
    name = tripname
    strip = Split(tripstring, ",")
    nbr = UBound(strip)
    ReDim ccts(nbr)
    
    b = 0
    t = nbr
    
    For i = 0 To nbr
        bname = strip(i)
        If Not Exists(bname, branches) Then
            MsgBox "Branch " & bname & " not found in trip " & tripstring
            Init = False
        Else
            Set br = branches.item(bname)
            If br.Xval <> 0# Then
                Set ccts(b) = br
                b = b + 1
            Else
                Set ccts(t) = br
                t = t - 1
            End If
        End If
    Next i
    nac = b - 1
End Function

' Creator for single circuit trip

Public Function OneBranch(tripname As String, br As Branch) As Boolean
    name = tripname
    nbr = 0
    ReDim ccts(nbr)
    Set ccts(0) = br
    If br.Xval <> 0# Then
        nac = 0
    Else
        nac = -1
    End If
    OneBranch = True
End Function

' Creator which joins 2 trips

Public Function Join(tripname As String, trip1 As Trip, trip2 As Trip) As Boolean
    Dim i As Long
    Dim b As Long, t As Long
    Dim br As Branch
    
    name = tripname
    nbr = trip1.nbr + trip2.nbr + 1
    ReDim ccts(nbr)
    b = 0
    t = nbr
    
    For i = 0 To trip1.nbr
        Set br = trip1.Circuit(i)
        If br.Xval <> 0# Then
            Set ccts(b) = br
            b = b + 1
        Else
            Set ccts(t) = br
            t = t - 1
        End If
    Next i
    For i = 0 To trip2.nbr
        Set br = trip2.Circuit(i)
        If br.Xval <> 0# Then
            Set ccts(b) = br
            b = b + 1
        Else
            Set ccts(t) = br
            t = t - 1
        End If
    Next i
    nac = b - 1
    Join = True
End Function

' Provide trip description (list of branch longnames)

Public Function TripDescription() As String
    Dim i As Long
    
    TripDescription = ccts(0).name
    
    For i = 1 To nbr
        TripDescription = TripDescription & "," & ccts(i).name
    Next i

End Function

' Deactivate

Public Sub Deactivate()
    Dim i As Long
    
    For i = 0 To nbr
        ccts(i).BOut = False
    Next i
End Sub

' Activate trip by computing trip sensmat and flagging branches as outaged
' returns false if trip no ac circuits in trip or trip splits ac network

Public Function Activate(sensmat() As Double) As Boolean
    Dim i As Long
    
    Activate = CalcSensMat(sensmat)
    
    For i = 0 To nbr
        ccts(i).BOut = True
    Next i
End Function

' Calc branch end injection sensitivity to branch flow = (I - Fsens)^-1
' Returns false if (I-Fsens) is singular

Private Function CalcSensMat(sensmat() As Double) As Boolean
    Dim tvec() As Double
    Dim mat() As Double
    Dim res As Variant
    Dim i As Long, j As Long
    Dim d As Double
    
    If nac < 0 Then
        CalcSensMat = False
        Exit Function
    End If
    
    ReDim mat(nac, nac) As Double
    ReDim sensmat(nac, nac) As Double
    
    For j = 0 To nac
        ReDim tvec(nodes.Count - 1) As Double
        tvec(ccts(j).pn1) = 1#
        tvec(ccts(j).pn2) = -1#
        ufac.Solve tvec, tvec ' calculate vang caused by unit injections at branch ends
                
        For i = 0 To nac    ' calculate flow impact on each trip branch of injections
        With ccts(i)
            If i = j Then
                mat(i, j) = 1#
            End If
            mat(i, j) = mat(i, j) - (tvec(.pn1) - tvec(.pn2)) * PUCONV / .Xval
        End With
        Next i
    Next j
    
    d = Excel.WorksheetFunction.MDeterm(mat)
    If Abs(d) <= lpEpsilon Then
'        MsgBox "Trip " & TripDescription() & " splits AC network?"
        CalcSensMat = False
        Exit Function
    End If
    
    res = Excel.WorksheetFunction.MInverse(mat)
    
    If nac = 0 Then
        sensmat(0, 0) = res(1)
    Else
        For i = 0 To nac
            For j = 0 To nac
                sensmat(i, j) = res(i + 1, j + 1)
            Next j
        Next i
    End If
    CalcSensMat = True
End Function

' Calculate trip tvang from intact ovang

Public Sub TripSolve(sensmat() As Double, ovang() As Double, tvang() As Double)
    Dim i As Long, j As Long
    Dim f() As Double, inj() As Double
    Dim tvec() As Double
    
    ReDim f(nac) As Double
    ReDim inj(nac) As Double
    ReDim tvec(UBound(ovang)) As Double
    
    ' Calc original flows on tripped branches
    
    For i = 0 To nac
        With ccts(i)
            f(i) = (ovang(.pn1) - ovang(.pn2)) * PUCONV / .Xval
        End With
    Next i
    
    ' Calc required injections
   
    For i = 0 To nac
        With ccts(i)
            For j = 0 To nac
                inj(i) = inj(i) + sensmat(i, j) * f(j)
            Next j
            tvec(.pn1) = tvec(.pn1) + inj(i)
            tvec(.pn2) = tvec(.pn2) - inj(i)
        End With
    Next i
    
    ufac.Solve tvec, tvec
    
    tvang = ovang
    
    For i = 0 To UBound(ovang)
        tvang(i) = tvang(i) + tvec(i)
    Next i
End Sub

#If LF Then

' Calculate Trip base and contrl vectors from intact versions
' Return true if vectors different from base case

Public Function TripVectors(civang() As Variant, tcvang() As Variant) As Boolean
    Dim i As Long, nc As Long
    Dim tv() As Double, ntv() As Double
    Dim ct As Control, br As Branch
    Dim sensmat() As Double
    
    nc = UBound(civang, 1)   ' upb of controls + ia sensitivity
    
    ReDim tcvang(nc) As Variant
    
    If Not Activate(sensmat) Then ' Might be dc ccts
        If nac >= 0 Then
            TripVectors = False      ' Some ac ccts present so ac trip splits network
            Exit Function
        End If
        
        ' no ac circuits in trip so must be hvdc trip
        
        tcvang(0) = civang(0)            ' base vang unchanged
        tcvang(nc) = civang(nc)          ' ia sensitivity (if present) unchanged
        
        For i = 1 To nc - 1
            Set ct = controls.item(i)
            Set br = ct.CBranch
            If Not br.BOut Then         ' control vang unchanged if hvdc not tripped (empty if control outaged)
                tcvang(i) = civang(i)
            End If
        Next i
        
    Else
        ' update base vang
        tv = civang(0)
        TripSolve sensmat, tv, ntv
        tcvang(0) = ntv
        'update ia sensitivity (if present)
        If IsArray(civang(nc)) Then
            tv = civang(nc)
            TripSolve sensmat, tv, ntv
            tcvang(nc) = ntv
        End If
        For i = 1 To nc - 1
            Set ct = controls.item(i)
            Set br = ct.CBranch
            If Not br.BOut Then
                If IsArray(civang(i)) Then
                    tv = civang(i)
                    TripSolve sensmat, tv, ntv
                    tcvang(i) = ntv
                Else
                    tcvang(i) = civang(i)
                End If
            Else
                ' Leave tcvang(i) empty
            End If
        Next i
    End If
    TripVectors = True
End Function

#End If
