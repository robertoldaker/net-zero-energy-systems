Attribute VB_Name = "Optimiser"
' Optimise controls

Option Explicit
Option Base 0

Private ctrlmodel As LPModel
Public bounvar As LPVarDef
Public dceqc() As LPConsDef
Public bounzc As LPConsDef
Public ctrlvar() As LPVarDef
Public ctrlzc() As LPConsDef
Public ctrlmc() As LPConsDef
Public brac() As LPConsDef
Public cctlim() As LPConsDef
Public corder() As Long         'Save initial constraints
Public lastcon As Long

Private Const NCCT As Long = 100  ' number of circuit constraints beyond minimum theoretical
Public Const XLRG As Double = 50000# ' largest feasible transfer

Public Sub BuildOptimiser()
    Dim i As Long, j As Long
    Dim cupb As Long
    Dim ctn As String, ctmax As Double, ctmin As Double
    Dim ctcst As Double, r2 As Long
    Dim dcn As String, ndc As Long, mag As Double
    Dim nn As Long, n1 As Long, n2 As Long, na As Long, nb As Long
    
    cupb = UBound(cmaxc, 1)
    ReDim ctrlvar(1 To cupb, 1) As LPVarDef
    ReDim ctrlzc(1 To cupb, 1) As LPConsDef
    ReDim ctrlmc(1 To cupb, 1) As LPConsDef
    ReDim cctlim(cupb + NCCT) As LPConsDef
    
    Set ctrlmodel = NewLPModel()
    
    Set bounvar = ctrlmodel.VarDef("bounv", "bouncz", 1#)       'Make boundary flow variable
    Set bounzc = ctrlmodel.ConsDef("bouncz", False, 0#, Array("bounv", -1#))
    
    ' Make an equality constraint for each hvdc node (<= + >= to ensure selection to basis)
    nn = nord.nn
    ndc = UBound(nstr, 1) - nn - 2
    ReDim dceqc(ndc, 1) As LPConsDef
        
    For i = 0 To ndc
        j = nord.NodeId(nord.nn + 1 + i)
        dcn = nstr(j, 1)
        mag = gen(j, 1) - dem(j, 1)
        
        Set dceqc(i, 0) = ctrlmodel.ConsDef(dcn & "pc", False, mag, Array())
        Set dceqc(i, 1) = ctrlmodel.ConsDef(dcn & "nc", False, -mag, Array())
    Next i
    
    For i = 1 To cupb
        j = cbid(i)
        ctn = lcstr(j, 1)
        ctcst = -ccost(i, 1) / injmax(i)
        ctmax = injmax(i)
        ctmin = -cminc(i, 1) / cmaxc(i, 1) * ctmax
        
        
        ' Make positive and negative ctrl variables and constraints
        Set ctrlvar(i, 0) = ctrlmodel.VarDef(ctn & "pv", ctn & "pzc", ctcst)
        Set ctrlvar(i, 1) = ctrlmodel.VarDef(ctn & "nv", ctn & "nzc", ctcst)
        Set ctrlzc(i, 0) = ctrlmodel.ConsDef(ctn & "pzc", False, 0#, Array(ctn & "pv", -1#))
        Set ctrlzc(i, 1) = ctrlmodel.ConsDef(ctn & "nzc", False, 0#, Array(ctn & "nv", -1#))
        Set ctrlmc(i, 0) = ctrlmodel.ConsDef(ctn & "pmc", False, ctmax, Array(ctn & "pv", 1#))
        Set ctrlmc(i, 1) = ctrlmodel.ConsDef(ctn & "nmc", False, ctmin, Array(ctn & "nv", 1#))
        
        n1 = bn1(j) - nn - 1
        n2 = bn2(j) - nn - 1
        
        If n1 >= 0 Then
            dceqc(n1, 0).augment ctn & "pv", 1#  ' +ve flow is away from node
            dceqc(n1, 0).augment ctn & "nv", -1#
            dceqc(n1, 1).augment ctn & "pv", -1#
            dceqc(n1, 1).augment ctn & "nv", 1#
        End If
        If n2 >= 0 Then
            dceqc(n2, 0).augment ctn & "pv", -1#   ' +ve flow is towards node
            dceqc(n2, 0).augment ctn & "nv", 1#
            dceqc(n2, 1).augment ctn & "pv", 1#
            dceqc(n2, 1).augment ctn & "nv", -1#
        End If
    Next i
    
    Set cctlim(0) = ctrlmodel.ConsDef("cct0", False, XLRG, Array("bounv", 1#))
    For i = 1 To UBound(cctlim, 1)
        Set cctlim(i) = ctrlmodel.ConsDef("cct" & CStr(i), False, 0#, Array())
    Next i
    
    Set ctrllp = ctrlmodel.MakeLP
    For i = 1 To UBound(cctlim, 1)
        ctrllp.Skip(cctlim(i).Id) = True
    Next i
    ctrllp.SaveCOrder corder
End Sub

Public Sub CalcSetPoints(setp() As Variant)
    Dim i As Long, n As Long
    Dim v As Double
    
    n = UBound(ctrlzc, 1)
    
    ReDim setp(1 To n, 1 To 1) As Variant
    
    For i = 1 To UBound(ctrlzc, 1)
        v = cmaxc(i, 1) / injmax(i)
        setp(i, 1) = (ctrllp.Slack(ctrlzc(i, 0).Id) - ctrllp.Slack(ctrlzc(i, 1).Id)) * v
'        If setp(i, 1) > cmaxc(i, 1) + lpEpsilon Then
'            setp(i, 1) = cmaxc(i, 1)
'        ElseIf setp(i, 1) < cminc(i, 1) - lpEpsilon Then
'            setp(i, 1) = cminc(i, 1)
'        End If
    Next i
End Sub

Public Function GetFreeCons() As Long
    Dim i As Long, j As Long, i0 As Long, n As Long
    Dim ms As Double, mi As Long
    
    mi = -1
    n = UBound(cctlim, 1)
    
    For i = 1 To n
        j = lastcon + i
        If j > n Then
            j = j - n
        End If
        
        i0 = cctlim(j).Id
        
        If ctrllp.Skip(i0) Then
            ctrllp.Skip(i0) = False
            lastcon = j
            GetFreeCons = i0
            Exit Function
        Else
            If ctrllp.Slack(i0) > ms Then
                ms = ctrllp.Slack(i0)
                mi = i0
            End If
        End If
    Next i
    GetFreeCons = mi
End Function

Public Function BoundCap() As Double
    BoundCap = ctrllp.Slack(bounzc.Id)
End Function

Public Sub ResetLP()
    Dim i As Long, j As Long, k As Long
    Dim ctmax As Double, ctmin As Double
    
    With ctrllp
        .TConsMat.ZeroRow cctlim(0).Id
        .TConsMat.Cell(cctlim(0).Id, bounvar.Id) = 1#
        .cname(cctlim(0).Id) = cctlim(0).name
        .bvec(cctlim(0).Id) = cctlim(0).Magnitude
        
        For i = 1 To UBound(cctlim, 1)
            ctrllp.Skip(cctlim(i).Id) = True
        Next i
        .RestoreCOrder corder
    End With
    
    For i = 1 To UBound(cmaxc, 1)
        j = cbid(i)

        If bout(j) Then
            ctmax = 0#
            ctmin = 0#
        Else
            ctmax = injmax(i)
            ctmin = -cminc(i, 1) / cmaxc(i, 1) * ctmax
        End If
        ctrllp.bvec(ctrlmc(i, 0).Id) = ctmax
        ctrllp.bvec(ctrlmc(i, 1).Id) = ctmin
    Next i
    lastcon = 0 ' cct0 reserved
End Sub

Public Function ReportConstraints(Optional stres As Long = STPASS) As String
    Dim i As Long, c As Long
    Dim st As Long, res As String

    With ctrllp
        
        For i = 0 To UBound(cctlim, 1)
            c = cctlim(i).Id
            If .InBasis(c) Then
                res = res & .cname(c) & ","
                st = ControlForm.Newstage(.cname(c))
                ControlForm.StageResult st, stres, "Shadow = " & Format(.Shadow(c), "0.0")
            End If
        Next i
        
    End With
    ReportConstraints = left(res, Len(res) - 1)
End Function

Public Function LimitCcts() As String
    Dim i As Long, c As Long
    Dim res As String

    With ctrllp
        
        For i = 0 To UBound(cctlim, 1)
            c = cctlim(i).Id
            If .InBasis(c) Then
                res = res & .cname(c) & ","
            End If
        Next i
        
    End With
    LimitCcts = left(res, Len(res) - 1)
End Function

' Populate constraint
' pt means planned transfer constraint (independent of xfer)

Public Sub PopulateConstraint(cct As Long, ByVal freecap As Double, ByVal dir As Double, sp() As Variant, iasf As Double, ctrlva() As Variant, pt As Boolean)
    Dim i As Long
    Dim s As Double, fc As Double
    Dim cons As Long
    Dim si As Double, sf As Double
    Dim xa As Long
    
    cons = GetFreeCons()
    If cons = -1 Then
        Err.Raise vbError + 611, , "No free constraint slots"
    End If
    
    fc = freecap
    If dir < 0# Then
        si = -1#
    Else
        si = 1#
    End If
    
    xa = UBound(ctrlva, 1) ' boundary sensitivity is last entry
    
    With ctrllp
        If pt Then
            .cname(cons) = "pt" & LineName(cct)
        Else
            .cname(cons) = LineName(cct)
        End If
        
        With .TConsMat
            .ZeroRow cons
            If IsArray(ctrlva(xa)) And Not pt Then            ' Boundary sensitivity present
                s = CctSensitivity(cct, xa, ctrlva)
                .Cell(cons, bounvar.Id) = si * s / Abs(ia)   ' sensitivity to boundary xfer
                fc = fc + si * s * iasf
            End If
            
            For i = 1 To UBound(ctrlvar, 1)         ' sensistivity to +ve/-ve ctrl vars
                If IsArray(ctrlva(i)) Then
                    s = CctSensitivity(cct, i, ctrlva)
                    sf = si * s / injmax(i)
                    
'                    If Abs(sf) >= CSENS Then
                        .Cell(cons, ctrlvar(i, 0).Id) = sf
                        .Cell(cons, ctrlvar(i, 1).Id) = -sf
                        fc = fc + si * s * sp(i, 1) / cmaxc(i, 1)
'                    End If
                End If
            Next i
        End With
        .bvec(cons) = fc
        
    End With
End Sub


Public Function CctSensitivity(cct As Long, ctrl As Long, ctrlvang() As Variant) As Double
    Dim f As Double, v1 As Double, v2 As Double
        
    If xval(cct, 1) <> 0# And Not bout(cct) Then      ' ac branch
        v1 = ctrlvang(ctrl)(bn1(cct))
        v2 = ctrlvang(ctrl)(bn2(cct))
        f = (v1 - v2) * PUCONV / xval(cct, 1)
        
        If ctrl = bctrl(cct) Then       'is this circuit the qb?
            f = f + injmax(ctrl)                      ' remove effect of injection on branch flow
        End If
    Else    ' hvdc
'        f = 1#
    End If
    
    CctSensitivity = f
End Function
