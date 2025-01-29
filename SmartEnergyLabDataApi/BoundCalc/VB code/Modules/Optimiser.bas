Attribute VB_Name = "Optimiser"
' Optimise controls
' 20 Nov 23 Changed boundary var to permit +ve and -ve
' 9 Jan 24 Modified to take data from collections

Option Explicit
Option Base 0

Private ctrlmodel As LPModel
Public ctrllp As LP
Public boun As LPVarDef         ' boundary transfer variable
Public dceqc() As LPConsDef     ' dcnode equality constraints
' Public ctrlvar() As LPVarDef    ' control variables now stored in control objects
Public cctlim() As LPConsDef    ' circuit capacity constraints
Public corder() As Long         ' Save initial constraints
Public lastcon As Long

Private Const NCCT As Long = 100     ' number of circuit constraints beyond minimum theoretical
Public Const XLRG As Double = 50000# ' largest feasible transfer
Public Const CSENS As Double = 1#    ' smallest recognised MW flow for max control action

Public Sub BuildOptimiser()
    Dim i As Long, j As Long
    Dim cupb As Long
    Dim ct As Control, nd As Node, br As Branch
    Dim ctn As String, ctmax As Double, ctmin As Double
    Dim ctcst As Double
    Dim dcn As String, ndc As Long, mag As Double
    Dim nn As Long, n1 As Long, n2 As Long
    
    cupb = controls.Count
    ReDim cctlim(cupb + NCCT) As LPConsDef
    
    Set ctrlmodel = NewLPModel()
    
    Set boun = ctrlmodel.PairDef("boun", Pwelfare:=1#, Nwelfare:=-1#, MaxValue:=XLRG)           'Make boundary flow variables
    
    ' Make an equality constraint for each hvdc node (<= + >= to ensure selection to basis)
    nn = nord.nn                        ' last ac node
    ndc = nodes.Count - nn - 2          ' number of dc nodes - 1
    If ndc >= 0 Then
        ReDim dceqc(ndc, 1) As LPConsDef
    End If
        
    For i = 0 To ndc
        j = nord.NodeId(nord.nn + 1 + i)
        Set nd = nodes.item(j)
        dcn = nd.name
        mag = nd.Generation - nd.Demand
        
        Set dceqc(i, 0) = ctrlmodel.ConsDef(dcn & "pc", CTLTE, mag, Array())
        Set dceqc(i, 1) = ctrlmodel.ConsDef(dcn & "nc", CTGTE, mag, Array())
    Next i
    
    For Each ct In controls
        With ct
        Set br = .CBranch
        ctn = .name
        ctcst = .Cost / .Injmax
        ctmax = .Injmax
        ctmin = .MinCtrl / .MaxCtrl * ctmax
         
        ' Make positive and negative ctrl variables and constraints
        Set .CtVar = ctrlmodel.PairDef(ctn, Pwelfare:=-ctcst, Nwelfare:=-ctcst, MaxValue:=ctmax, MinValue:=ctmin)
       
        n1 = br.pn1 - nn - 1
        n2 = br.pn2 - nn - 1
        
        If n1 >= 0 Then
            dceqc(n1, 0).augment ctn, 1#   ' +ve flow is away from node
            dceqc(n1, 1).augment ctn, 1#
        End If
        If n2 >= 0 Then
            dceqc(n2, 0).augment ctn, -1#    ' +ve flow is towards node
            dceqc(n2, 1).augment ctn, -1#
        End If
        End With
    Next ct
    
    For i = 0 To UBound(cctlim, 1)
        Set cctlim(i) = ctrlmodel.ConsDef("cct" & CStr(i), CTLTE, 0#, Array())
    Next i
    
    Set ctrllp = ctrlmodel.MakeLP
    For i = 0 To UBound(cctlim, 1)
        ctrllp.Skip(cctlim(i).Id) = True
    Next i
    ctrllp.SaveCOrder corder
End Sub

' Find a free circuit constraint slot in LP

Public Function GetFreeCons() As Long
    Dim i As Long, j As Long, i0 As Long, n As Long
    Dim ms As Double, mi As Long
    
    mi = -1
    n = UBound(cctlim, 1)
    
    For i = 0 To n
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
    Dim pfer As Double
    
    pfer = ActiveBound.PlannedTransfer
    BoundCap = pfer + Sgn(pfer) * boun.Value(ctrllp)
End Function

Public Function ControlCost() As Double
    ControlCost = boun.Value(ctrllp) - ctrllp.Objective
End Function

Public Sub ResetLP()
    Dim i As Long, j As Long, k As Long
    Dim ctmax As Double, ctmin As Double
    Dim cvarpmc As Long, cvarnmc As Long
    Dim ct As Control, br As Branch
    
    With ctrllp
        For i = 0 To UBound(cctlim, 1)
            .Skip(cctlim(i).Id) = True
        Next i
        .RestoreCOrder corder
    End With
    
    For Each ct In controls
        With ct
        Set br = .CBranch

        If br.BOut Then
            ctmax = 0#
            ctmin = 0#
        Else
            ctmax = .Injmax
            ctmin = -.MinCtrl / .MaxCtrl * ctmax
        End If
        cvarpmc = .CtVar.Vpv.Vmc.Id
        cvarnmc = .CtVar.Vnv.Vmc.Id
'        If ctrllp.bvec(cvarpmc) <> ctmax Then
            ctrllp.bvec(cvarpmc) = ctmax
'        End If
'        If ctrllp.bvec(cvarnmc) <> ctmin Then
            ctrllp.bvec(cvarnmc) = ctmin
'        End If
        End With
    Next ct
    lastcon = 0
End Sub

' Places action branch constraint shadows in variant array for output in branch table

Public Function ReportConstraints(shadows() As Variant) As String
    Dim i As Long, c As Long
    Dim st As Long, res As String
    Dim br As Branch
    
    ReDim shadows(1 To branches.Count, 1 To 1)
    
    If ctrllp Is Nothing Then
        ReportConstraints = ""
        Exit Function
    End If
    
    With ctrllp
        
        For i = 0 To UBound(cctlim, 1)
            c = cctlim(i).Id
            If .InBasis(c) Then
                res = res & .cname(c) & ","
                If left(.cname(c), 2) = "pt" Then
                    Set br = branches.item(Mid(.cname(c), 3))
                Else
                    Set br = branches.item(.cname(c))
                End If
                shadows(br.Index, 1) = .Shadow(c)
            End If
        Next i
        
    End With
    If res = "" Then
        ReportConstraints = res
    Else
        ReportConstraints = left(res, Len(res) - 1)
    End If
End Function

Public Function Limitccts() As String
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
    If res = "" Then
        Limitccts = ""
    Else
        Limitccts = left(res, Len(res) - 1)
    End If
End Function

' Populate constraint
' pt means planned transfer constraint (independent of xfer)

Public Sub PopulateConstraint(br As Branch, ByVal freecap As Double, ByVal dir As Double, iasf As Double, ctrlva() As Variant, pt As Boolean)
    Dim i As Long
    Dim s As Double, fc As Double
    Dim cons As Long
    Dim si As Double, sf As Double
    Dim xa As Long
    Dim ct As Control
    
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
            .cname(cons) = "pt" & br.name
        Else
            .cname(cons) = br.name
        End If
        
        With .TConsMat
            .ZeroRow cons
            If IsArray(ctrlva(xa)) And Not pt Then              ' Boundary sensitivity present
                s = CctSensitivity(br, xa, ctrlva)
                sf = si * s / Abs(ActiveBound.InterconAllowance)
                .Cell(cons, boun.Vpv.Id) = sf                   ' sensitivity to boundary +ve xfer
                .Cell(cons, boun.Vnv.Id) = -sf                  ' sensitivity to boundary -ve xfer
                fc = fc + si * s * iasf
            End If
            
            For Each ct In controls                             ' sensistivity to +ve/-ve ctrl vars
                i = ct.Index
                If IsArray(ctrlva(i)) Then
                    s = CctSensitivity(br, i, ctrlva)
                    sf = si * s / ct.Injmax
                    
                    If Abs(s) >= CSENS Then
                        .Cell(cons, ct.CtVar.Vpv.Id) = sf
                        .Cell(cons, ct.CtVar.Vnv.Id) = -sf
                        fc = fc + si * s * ct.SetPoint(SPAuto) / ct.MaxCtrl
                    End If
                End If
            Next ct
        End With
        .bvec(cons) = fc
        
    End With
End Sub


Public Function CctSensitivity(br As Branch, ctnum As Long, ctrlvang() As Variant) As Double
    Dim f As Double, v1 As Double, v2 As Double
    Dim ct As Control
    Dim cvang() As Double
    
    cvang = ctrlvang(ctnum)
        
    f = br.flow(cvang, SPZero, True)
    
    Set ct = br.BCtrl
    If Not ct Is Nothing Then
        If ct.Index = ctnum Then                ' is this circuit the qb?
            f = f + ct.Injmax                   ' remove effect of injection on branch flow
        End If
    End If
    
    CctSensitivity = f
End Function
