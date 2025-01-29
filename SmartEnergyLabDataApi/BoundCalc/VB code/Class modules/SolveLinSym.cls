VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "SolveLinSym"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
' Solve A.X = B by factoring sparse A = L.U with partial pivoting
' Matrix A is symmetrical and includes only upper diagonal
' Matrix A is assumed diagonal dominant (e.g. network admittance matrix)
'
' L A Dale 5 Jul 2017

Option Explicit
Option Base 0

Const ZEROPIVOT As Long = vbObjectError + 600

Private amat As SparseMatrix        'Matrix A
Private umat As SparseMatrix        'U = upper diagonal factor of A
' Private rord() As Long            'Keep given Row ordering from non-zero counting
Private rmax As Long                'Order of amat
' Private scal() As Double            'Scaling factor for each row (for relative size pivot choice)


' factor A = L.U on a copy or optionally in place, L stored in leading diagonal of U
' a copy is required if Refine is to be used

Public Sub Init(smat As SparseMatrix, Optional Copy As Boolean = True)
    Dim r As Long, c As Long, t As Long, k As Long
    Dim piv As Double, v As Double, f As Double
   
    rmax = smat.Rupb
        
    If Copy Then
        Set amat = New SparseMatrix
        amat.Copy smat                  ' Make a local copy amat (because we will modify)
        Set umat = New SparseMatrix
        umat.Copy smat
        ' Include ldiag in amat
        For r = 0 To rmax - 1
            k = smat.FirstKey(r)        ' should be diag
            Do
                k = smat.NextKey(k, r)
                If k < 0 Then
                    Exit Do
                End If
                smat.Contents k, c, v
                amat.Addin c, r, v
            Loop
         Next r
    Else
        Set amat = Nothing
        Set umat = smat
    End If
    
    For c = 0 To rmax
            
        k = umat.FirstKey(c)             ' first element of row is pivot
        
        If k < 0 Then
            Err.Raise ZEROPIVOT, , "Suitable pivot not found at column " & str(c)
        End If
        umat.Contents k, t, piv
        
        If t <> c Or piv = 0# Then
            Err.Raise ZEROPIVOT, , "Suitable pivot not found at column " & str(c)
        End If
        
        Do
            k = umat.NextKey(k, c)
            If k < 0 Then
                Exit Do
            End If
                
            umat.Contents k, r, v
        
            If v <> 0# Then
                f = -v / piv
                    umat.AddRow r, c, f, r   ' eliminate non-zero element process only upper diagonal
            End If
        Loop
    Next c
End Sub

' Solve L.U.X = B
' X can replace contents of B

Public Sub Solve(bvec() As Double, xvec() As Double)
    Dim r As Long, rr As Long, k As Long
    Dim c As Long
    Dim v As Double, s As Double, piv As Double
    Dim yvec() As Double
    
    yvec = bvec
    
    For r = 0 To rmax - 1
        
        k = umat.FirstKey(r)
        umat.Contents k, rr, piv
        
        Do
            k = umat.NextKey(k, r)
            If k = -1 Then
                Exit Do
            End If
            
            umat.Contents k, rr, v
            
            yvec(rr) = yvec(rr) - v * yvec(r) / piv
        Loop
    Next r
    
    yvec(rmax) = yvec(rmax) / umat.Cell(rmax, rmax)
    For r = rmax - 1 To 0 Step -1
        
        s = yvec(r)
        
        k = umat.FirstKey(r)
        
        umat.Contents k, c, piv
        Do
            k = umat.NextKey(k, r)
            If k = -1 Then
                Exit Do
            End If
            umat.Contents k, c, v
            s = s - v * yvec(c)
        Loop
        yvec(r) = s / piv
    Next r
    
    For r = rmax + 1 To UBound(yvec)    ' zero unused entries (e.g. reference node)
        yvec(r) = 0#
    Next r
    xvec = yvec
End Sub


' Refine solution to A.X=B replacing X
' Returns biggest absolute correction

Public Function Refine(bvec() As Double, xvec() As Double) As Double
    Dim tvec() As Double, dxvec() As Double
    Dim largest As Double
    Dim i As Long
    
    ReDim tvec(rmax) As Double, dxvec(rmax) As Double
    
    amat.MultVec xvec, tvec   ' If X is Xtrue + deltaX then A.X gives B + deltaB
    
    For i = 0 To rmax
        tvec(i) = tvec(i) - bvec(i) ' calc deltaB
    Next i
    
    Solve tvec, dxvec
    
    For i = 0 To rmax
        If Abs(dxvec(i)) > Abs(largest) Then
            largest = dxvec(i)
        End If
        xvec(i) = xvec(i) - dxvec(i)    ' Improve solution estimate
    Next i
    Refine = largest
End Function

' Solve (A + UxV).X = B Using Sherman-Morrison
' Uses Y and Z already solved from A.Y = B and A.Z=U

Public Sub SMSolve(yvec() As Double, zvec() As Double, vvec() As Double, rvec() As Double)
    Dim vy As Double
    Dim vz As Double
    Dim v As Double
    Dim i As Long
    
    ReDim rvec(rmax) As Double
    
    For i = 0 To rmax
        v = vvec(i)
        If v <> 0# Then
            vy = vy + v * yvec(i)
            vz = vz + v * zvec(i)
        End If
    Next i
    
    v = vy / (1# + vz)
    
    For i = 0 To rmax
        rvec(i) = yvec(i) - v * zvec(i)
    Next i
End Sub

' Returns biggest cell error of A.invA - I

Private Function Check() As Double
    Dim tvec() As Double, rvec() As Double
    Dim i As Long, j As Long
    Dim res As Double, terr As Double
        
    For i = 0 To rmax
        ReDim tvec(rmax)
        tvec(i) = 1#
        
        Solve tvec, rvec
        amat.MultVec rvec, rvec
        
        For j = 0 To rmax
            terr = Abs(tvec(j) - rvec(j))
            If terr > res Then
                res = terr
            End If
        Next j
    Next i
    
    Check = res
End Function
    
    
' Uses check on simple test matrix

Public Function Test() As Boolean
    Dim tmat As SparseMatrix
    Dim res As Double
        
    On Error GoTo errorhandler
    
    Set tmat = New SparseMatrix
    tmat.Init 2, 2
    With tmat
        .Cell(0, 0) = 1#
        .Cell(1, 1) = 2#
        .Cell(2, 2) = 3#
        .Cell(0, 2) = 6#
        .Cell(1, 2) = 5#
    End With
    
    Init tmat
        
    res = Check
    
    Test = res < 0.0000000001
    
    Exit Function
errorhandler:
    Test = False
End Function
