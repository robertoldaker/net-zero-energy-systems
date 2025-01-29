VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "TopNTab"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' Record Top N boundary capacity cases

Option Explicit
Option Base 0

' Public name As String           ' I=intact, S=Single trip, D=Double trip
Private outtab As DTable
Public nsz As Long              ' Number of rows
Private fnames() As String
Dim nmap() As Long
Private results() As Variant
Private bnd As Boundary         'Current active boundary
Private rtfer As Double         'required transfer for calculating surplus


Public Function Init(outtable As DTable, n As Long) As Boolean
    Dim i As Long
    Dim ct As Control
    
    ReDim fnames(controls.Count + 3)
    fnames(0) = "BoundCap"
    fnames(1) = "Surplus"
    fnames(2) = "Trip"
    fnames(3) = "Limit"
    For i = 4 To controls.Count + 3
        Set ct = controls.item(i - 3)
        fnames(i) = ct.name
    Next i
    
    Set outtab = outtable
    nsz = n
    
    If Not outtab.FieldMap(nmap, fnames, True) Then
        Init = False
        Exit Function
    End If
    
    outtab.Resize n
    outtab.GetData results
    Clear
    
    Init = True
End Function

Public Sub Clear()
    Dim r As Long, c As Long
    
    r = UBound(results, 1)
    c = UBound(results, 2)
    
    ReDim results(1 To r, 1 To c)
    
    outtab.PutData results
End Sub

Public Sub UpdateTable()
    outtab.PutData results
End Sub

' Set boundary and required transfer

Public Sub SetBoundary(bound As Boundary, reqdtfer As Double)
    Set bnd = bound
    rtfer = reqdtfer
End Sub

' Shuffle down the rows in results to make a blank row

Private Sub Shuffle(row As Long)
    Dim i As Long, j As Long
    
    For i = nsz - 1 To row Step -1
        For j = LBound(results, 2) To UBound(results, 2)
            results(i + 1, j) = results(i, j)
        Next j
    Next i
End Sub

' Inserts rows in results
' Takes bc(), mord() as the list of potential constraints, limitccts is the comma separated list of optimiser limits
' Places all active branch constrants (optimised loadflow) in first qualifying row
' Places control set points on first qualifying row

Public Sub Insert(tr As Trip, bndcap() As Double, mord() As Long, Limitccts As String, Optional Update As Boolean = False)
    Dim i As Long, j As Long, m As Long
    Dim first As Boolean, spm As Long, limits As Long, limitsu As Double
    Dim br As Branch, bn As String
    Dim su As Double
    
    first = True            'signals first row of trip  must be output with setpoints
    j = 0
    If Limitccts <> "" Then
        spm = SPAuto
        limits = 1          ' signals limitccts must be output, 2 = check if cct in limitccts, 3 = ignore
    Else
        spm = SPMan
    End If
    
    For i = 1 To nsz
        m = mord(j)
        su = Abs(bndcap(m)) - Abs(rtfer)
        If limits = 2 Then
            If Abs(su - limitsu) < 50# Then           ' if close to limits surplus check for branches in limitccts to skip
                Set br = branches.item(m + 1)
                bn = br.name
                If InStr(Limitccts, bn) > 0 Then
                        j = j + 1
                        m = mord(j)
                        su = bndcap(m) - Abs(rtfer)
                End If
            Else
                limits = 3
            End If
        End If
        If IsEmpty(results(i, nmap(1))) Or su < results(i, nmap(1)) Then
            Shuffle i
            
            results(i, nmap(0)) = bndcap(m) * Sgn(rtfer)
            results(i, nmap(1)) = su
            If tr Is Nothing Then
                results(i, nmap(2)) = "Intact"
            Else
                results(i, nmap(2)) = tr.name & ":" & tr.TripDescription
            End If
            
            FillControls i, first, spm              ' Copy control setpoints
            first = False
            
            Set br = branches.item(m + 1)
            bn = br.name
            If limits = 1 Then
                If InStr(Limitccts, bn) > 0 Then
                    results(i, nmap(3)) = Limitccts     ' output all limiting ccts
                    limits = 2
                    limitsu = su
                    
                    Do
                        j = j + 1                       ' skip over current branch and any following braches that are in limitccts
                        If j > UBound(mord) Then
                            Exit For
                        End If
                        Set br = branches.item(mord(j) + 1)
                        bn = br.name
                    Loop While InStr(Limitccts, bn) > 0
                Else
                    results(i, nmap(3)) = bn
                    j = j + 1
                End If
            Else
                results(i, nmap(3)) = bn            ' output branchname
                j = j + 1
            End If
        End If
        If j > UBound(mord) Then
            Exit For
        End If
    Next i
    If Update Then
        UpdateTable
    End If
End Sub

Private Sub FillControls(row As Long, first As Boolean, spm As Long)
    Dim k As Long
    Dim ct As Control
    
    If first Then
        For k = 4 To controls.Count + 3
            Set ct = controls.item(k - 3)
            results(row, nmap(k)) = ct.SetPoint(spm)
        Next k
    
    Else
        For k = 4 To controls.Count + 3
            results(row, nmap(k)) = Empty
        Next k
    End If
End Sub

' Place error message at top of table

Public Sub Error(tr As Trip, rc1 As Long, msg As String)
    Dim k As Long

    Shuffle 1
    results(1, nmap(0)) = "Fail"
    results(1, nmap(1)) = -99999
    If tr Is Nothing Then
        results(1, nmap(2)) = "Intact"
    Else
        results(1, nmap(2)) = tr.name & ":" & tr.TripDescription
    End If
    Select Case rc1
        Case lpInfeasible
            results(1, nmap(3)) = "LP Infeasible"
            
        Case lpUnbounded
            results(1, nmap(3)) = "LP Unbounded"
            
        Case lpIters
            results(1, nmap(3)) = "LP Iterations exceeded"
            
        Case lpUnknown
            results(1, nmap(3)) = "LP Unknown fail cause"
            
        Case lpZeroPivot
            results(1, nmap(3)) = "Zero pivot or split network"
            
        Case Else
            results(1, nmap(3)) = msg
    End Select
    For k = 4 To controls.Count + 3
        results(1, nmap(k)) = Empty
    Next k
    UpdateTable
End Sub
