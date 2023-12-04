VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "SparseMatrix"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
' Sparse matrix object
'
' Stores non-zero values and indices in storage arrays
'
' L A Dale 5 Jan 2017
' Binary search added 19 Jan 2017
' Access via keys added 20Jan 2017
' AddRow routines avoid searching. Also shuffle of storage centralised  22 Jan 2017
' Addrow has optional argumnt to process upper diagonal elements only 11 Feb 2017
' Test routines added 2 Jan 2019

Option Explicit
Option Base 0

Private val() As Double ' storage for element values
Private ind() As Long   ' storage for element indices
Private lst() As Long   ' storage for row pointers
Private maxr As Long    ' number of rows-1
Private maxs As Long    ' store upb
Private maxc As Long    ' number of columns-1
Private last As Long    ' last used store
Private myepr As Double ' elements per row storage estimate

' Initialise an empty matrix
' rupb row upper bound, epr elements per row assumption for storage sizing

Public Sub Init(Rupb As Long, cupb As Long, Optional epr As Double = 2.5)
    
    myepr = epr
    maxr = Rupb
    maxc = cupb
    maxs = Int((maxr + 1) * epr + 0.5)
    
    ReDim lst(maxr + 1) As Long
    ReDim val(maxs) As Double
    ReDim ind(maxs) As Long
  
End Sub

Public Property Get Rupb() As Long
    Rupb = maxr
End Property

Public Property Get cupb() As Long
    cupb = maxc
End Property

Public Property Get size() As Long
    size = lst(maxr + 1) - lst(0)
End Property

Public Property Get Rsize(r As Long) As Long
    Rsize = lst(r + 1) - lst(r)
End Property

' Become a copy of an existing matrix or part of (no spare storage)

Public Sub Copy(sm As SparseMatrix, Optional rs As Long = 0, Optional re As Long = -1)
    Dim r As Long, i As Long
    Dim k As Long, sz As Long
    
    With sm
        If re < 0 Then
            re = .Rupb
        End If
        
        For r = rs To re
            sz = sz + .Rsize(r)
        Next r
        
        maxs = sz - 1
        maxr = re - rs
        maxc = .cupb
        myepr = (maxs + 1) / (maxr + 1)
    End With
    
    ReDim lst(maxr + 1) As Long
    ReDim val(maxs) As Double
    ReDim ind(maxs) As Long
      
    For r = rs To re
        lst(r - rs) = i
        k = sm.FirstKey(r)
        While k <> -1
            sm.Contents k, ind(i), val(i)
            i = i + 1
            k = sm.NextKey(k, r)
        Wend
    Next r
    
    lst(maxr + 1) = i
End Sub

' Become a copy of an existing matrix with rows selected by map (no spare storage)

Public Sub CopyMap(sm As SparseMatrix, map() As Long, Optional rmax As Long = -1)
    Dim r As Long, i As Long, rm As Long
    Dim k As Long, sz As Long
    
    With sm
        If rmax < 0 Then
            rmax = UBound(map)
        End If
        
        For r = 0 To rmax
            sz = sz + .Rsize(map(r))
        Next r
        
        maxs = sz - 1
        maxr = rmax
        maxc = .cupb
        myepr = (maxs + 1) / (maxr + 1)
    End With
    
    ReDim lst(maxr + 1) As Long
    ReDim val(maxs) As Double
    ReDim ind(maxs) As Long
      
    For r = 0 To rmax
        lst(r) = i
        rm = map(r)
        
        k = sm.FirstKey(rm)
        While k <> -1
            sm.Contents k, ind(i), val(i)
            i = i + 1
            k = sm.NextKey(k, rm)
        Wend
    Next r
    
    lst(maxr + 1) = i
End Sub

' Become a copy of an existing matrix row

Public Sub CopyRow(sm As SparseMatrix, smr As Long)
    Dim i As Long, r As Long
    Dim k As Long
    
    With sm
        maxs = .Rsize(smr) - 1
        maxr = 0
        maxc = .cupb
        myepr = maxs + 1
    End With
    
    ReDim lst(maxr + 1) As Long
    ReDim val(maxs) As Double
    ReDim ind(maxs) As Long
      
    lst(0) = i
    k = sm.FirstKey(smr)
    While k <> -1
        sm.Contents k, ind(i), val(i)
        i = i + 1
        k = sm.NextKey(k, smr)
    Wend
    
    lst(1) = i
End Sub

' Become a transpose of an existing matrix

Public Sub Transpose(sm As SparseMatrix)
    Dim r As Long, c As Long
    Dim k As Long
    Dim v As Double
    
    With sm
        maxs = .size - 1
        maxr = .cupb
        maxc = .Rupb
        myepr = (maxs + 1) / (maxr + 1)
    End With
    
    ReDim lst(maxr + 1) As Long
    ReDim val(maxs) As Double
    ReDim ind(maxs) As Long
      
    For r = 0 To sm.Rupb
        
        k = sm.FirstKey(r)
        While k <> -1
            sm.Contents k, c, v
            Insert c, r, v
            k = sm.NextKey(k, r)
        Wend
    Next r
End Sub


' Linear search for element on row r with column index c
' If found returns true with key of element
' If not found returns false with key of next element

Public Function LFindKey(ByVal r As Long, ByVal c As Long, ByRef key As Long) As Boolean
    Dim i As Long, ii As Long
    
    For i = lst(r) To lst(r + 1) - 1
        ii = ind(i)
        If ii = c Then
            LFindKey = True
            key = i
            Exit Function
        ElseIf ii > c Then
            LFindKey = False
            key = i
            Exit Function
        End If
    Next i
    
    LFindKey = False
    key = i
End Function

' Binary search for element on row r with column index c
' If found returns true with key of element
' If not found returns false with key of next element

Public Function FFindKey(ByVal r As Long, ByVal c As Long, ByRef key As Long) As Boolean
    Dim i As Long, ii As Long
    Dim fi As Long, li As Long
    Dim fii As Long, lii As Long

    fi = lst(r)
    li = lst(r + 1) - 1
    
    If fi > li Then     ' List empty
        FFindKey = False
        key = fi
        Exit Function
    End If
    
    fii = ind(fi)
    
    If fii = c Then     ' First in list match
        FFindKey = True
        key = fi
        Exit Function
    ElseIf fii > c Then ' First in list too high
        FFindKey = False
        key = fi
        Exit Function
    ElseIf fi = li Then ' First also last in list and too low
        FFindKey = False
        key = li + 1
        Exit Function
    End If
        
    lii = ind(li)
        
    If lii = c Then     'Last in list match
        FFindKey = True
        key = li
        Exit Function
    ElseIf lii < c Then 'Last in list too low
        FFindKey = False
        key = li + 1
        Exit Function
    End If
            
    While li - fi > 1
        i = (li + fi) / 2   ' Split list in middle
        ii = ind(i)
        
        If ii = c Then
            FFindKey = True
            key = i
            Exit Function
        ElseIf ii < c Then
            fi = i
            fii = ii
        Else
            li = i
            lii = i
        End If
    Wend
    
    FFindKey = False
    key = li
End Function

' Get first key of row r
' Returns -1 if no keys
' Warning: key may become invalid if new elements inserted into this object

Public Function FirstKey(ByVal r As Long) As Long
    If lst(r) = lst(r + 1) Then
        FirstKey = -1
    Else
        FirstKey = lst(r)
    End If
End Function

' Get next key after k on row r
' Warning: key may become invalid if new elements inserted into this object

Public Function NextKey(ByVal k As Long, ByVal r As Long) As Long
    k = k + 1
    If k = lst(r + 1) Then
        NextKey = -1
    Else
        NextKey = k
    End If
End Function

' Get contents at key k
' Warning: key may become invalid if new elements inserted into this object

Public Sub Contents(ByVal k As Long, ByRef c As Long, ByRef v As Double)
    c = ind(k)
    v = val(k)
End Sub

'Lookup element on row r column c

Public Function Lookup(ByVal r As Long, ByVal c As Long) As Double
    Dim k As Long
    
    If FFindKey(r, c, k) Then
        Lookup = val(k)
    Else
        Lookup = 0#
    End If
End Function

' Shuffle storage from i up n cells for use on row r, resize store if necessary
'
Private Sub Shuffle(ByVal i As Long, ByVal n As Long, ByVal r As Long)
    Dim nmax As Long
    Dim j As Long
    'i points to destination cell
    'lst(maxr + 1) points to next free store
        
    nmax = lst(maxr + 1) - 1 + n
    If nmax > maxs Then
        ' extend store
        If nmax > maxs + maxs / 2 Then
            maxs = nmax
        Else
            maxs = maxs + maxs / 2
        End If
        
        ReDim Preserve ind(maxs) As Long
        ReDim Preserve val(maxs) As Double
    End If
    
    If n >= 0 Then
        For j = nmax To i + n Step -1
            ind(j) = ind(j - n)
            val(j) = val(j - n)
        Next j
    Else
        For j = i - n To lst(maxr + 1) - 1
            ind(j + n) = ind(j)
            val(j + n) = val(j)
        Next j
    End If

    For j = r + 1 To maxr + 1
        lst(j) = lst(j) + n
    Next j
End Sub

'Best called in ascending order of c then r

Public Sub Insert(ByVal r As Long, ByVal c As Long, ByVal v As Double)
    Dim i As Long, j As Long
    
    If FFindKey(r, c, i) Then
        val(i) = v
        Exit Sub
    End If
    
    'i points to destination cell
    
    Shuffle i, 1, r
               
    ind(i) = c
    val(i) = v
End Sub

' Removes element (and sets to zero)

Public Sub Zero(r As Long, c As Long)
    Dim i As Long, j As Long
    
    If Not FFindKey(r, c, i) Then
        Exit Sub            ' item not present
    End If
    
    Shuffle i, -1, r
End Sub

' Removes all elements in row

Public Sub ZeroRow(r As Long)
    Dim i As Long, k As Long, n As Long
    
    k = FirstKey(r)
    n = Rsize(r)
    
    If k >= 0 And n > 0 Then
        Shuffle k, -n, r
    End If
End Sub

Public Function dopurge(Optional eps As Double = 0.00000001) As Long
    Dim r As Long, c As Long, i As Long
    Dim s As Long, e As Long, t As Long
    
    For r = maxr To 0 Step -1
        s = -1
        e = 0
        For i = lst(r + 1) - 1 To lst(r) Step -1
            If Abs(val(i)) < eps Then
                s = i
                e = e - 1
                t = t + 1
            Else
                If s >= 0 Then
                    Shuffle s, e, r
                    s = -1
                    e = 0
                End If
            End If
        Next i
        If s >= 0 Then
            Shuffle s, e, r
        End If
    Next r
    dopurge = t
End Function


Public Sub Addin(ByVal r As Long, ByVal c As Long, ByVal v As Double)
    Dim i As Long, j As Long
    
    If FFindKey(r, c, i) Then
        val(i) = val(i) + v     ' Add to existing cell
        Exit Sub
    End If
    
    'i points to destination cell
    
    Shuffle i, 1, r
    
    ind(i) = c
    val(i) = v      ' Populate new cell
End Sub


' Replace a row with a row from another sparse matrix

Public Sub ReplaceRow(ra As Long, sm As SparseMatrix, smr As Long)
    Dim k As Long, bi As Long, v As Double
    Dim i As Long, aa As Long, ai As Long
    Dim n As Long, kc As Long
    Dim la As Long, lb As Long
    
    la = lst(ra + 1) - lst(ra)
    lb = sm.Rsize(smr)
    
    If lb > la Then
        aa = lst(ra + 1)        ' point at cell beyond end of row
        Shuffle aa, lb - la, ra ' make extra room
    ElseIf lb < la Then
        aa = lst(ra) + lb - 1
        Shuffle aa, lb - la, ra
    End If
    
    aa = lst(ra)
    k = sm.FirstKey(smr)
    
    While k <> -1
        sm.Contents k, bi, v
        val(aa) = v
        ind(aa) = bi
        aa = aa + 1
        k = sm.NextKey(k, smr)
    Wend
End Sub

Public Property Let Cell(r As Long, c As Long, v As Double)
    Insert r, c, v
End Property

Public Property Get Cell(r As Long, c As Long) As Double
    Cell = Lookup(r, c)
End Property

' Row r of A . V

Public Function RowDotVec(r As Long, vec() As Double) As Double
    Dim i As Long, res As Double
    
    For i = lst(r) To lst(r + 1) - 1
        res = res + val(i) * vec(ind(i))
    Next i
        
    RowDotVec = res
End Function


' R = A.V  R can be same as V

Public Sub MultVec(vec() As Double, rvec() As Double)
    Dim r As Long
    Dim tvec() As Double
    
    ReDim tvec(maxr) As Double
    
    For r = 0 To maxr
        tvec(r) = RowDotVec(r, vec)
    Next r
    
    rvec = tvec
End Sub


' Add scaled row b to row a:   rowa = rowa + sf.rowb
' Uses Find to locate row a position
'
'Public Sub AddRowI(ra As Long, rb As Long, sf As Double)
'    Dim b As Long, bb As Long
'
'    For b = 0 To lst(rb + 1) - 1 - lst(rb)  ' modifications of rowa may change lst(rb)
'        bb = lst(rb) + b
'        Addin ra, ind(bb), sf * val(bb)
'    Next b
'End Sub

' Add scaled row b to row a:   rowa = rowa + sf.rowb
' Avoids Find to locate rowa position
' cb permits elements in rowb to be skipped where ind<cb (i.e. upper diagonal only)

Public Sub AddRow(ra As Long, rb As Long, sf As Double, Optional cb As Long = 0)
    Dim i As Long, aa As Long, ai As Long
    Dim b As Long, bb As Long, bi As Long
    Dim v As Double
    
    aa = lst(ra)
    ' note: mods of rowa may change lst(rb)
    
    If cb > 0 Then
        bb = lst(rb)
        Do
            If bb >= lst(rb + 1) Then
                Exit Sub
            End If
            If ind(bb) >= cb Then
                Exit Do
            End If
            bb = bb + 1
        Loop
        b = bb - lst(rb)
    End If
    
    Do
        If b > lst(rb + 1) - 1 - lst(rb) Then
            Exit Sub
        End If
                
        If aa > lst(ra + 1) - 1 Then
            Exit Do
        End If
        
        bb = lst(rb) + b
        ai = ind(aa)
        bi = ind(bb)

        If ai < bi Then
            aa = aa + 1
            
        ElseIf ai = bi Then
            val(aa) = val(aa) + sf * val(bb)
            aa = aa + 1
            b = b + 1
            
        Else
            v = sf * val(bb)
            Shuffle aa, 1, ra
            val(aa) = v
            ind(aa) = bi
            aa = aa + 1
            b = b + 1
        End If
    Loop
    
    Shuffle aa, lst(rb + 1) - lst(rb) - b, ra   ' make space for remainder of rowb
    
    bb = lst(rb) + b
    
    For i = 0 To lst(rb + 1) - 1 - bb
        val(aa + i) = sf * val(bb + i)
        ind(aa + i) = ind(bb + i)
    Next i
End Sub


' Add scaled row of another sparse matrix: rowa = rowa + sf.sm.row_smr
' Uses Find to locate rowa position
' Warning: errors likely if sm is the same as myself
'
'Public Sub AddMatRowI(ra As Long, sm As SparseMatrix, smr As Long, sf As Double)
'    Dim k As Long, c As Long, v As Double
'
'    k = sm.FirstKey(smr)
'    While k <> -1
'        sm.Contents k, c, v
'        Addin ra, c, sf * v
'        k = sm.NextKey(k, smr)
'    Wend
'End Sub
'
' Add scaled row of another sparse matrix: rowa = rowa + sf.smrowr
' Avoids Find to locate rowa position
' Warning: errors likely if sm is the same as myself

Public Sub AddMatRow(ra As Long, sm As SparseMatrix, smr As Long, sf As Double)
    Dim k As Long, bi As Long, v As Double
    Dim i As Long, aa As Long, ai As Long
    Dim n As Long, kc As Long
    
    aa = lst(ra)
    k = sm.FirstKey(smr)
    
    Do
        If k = -1 Then
            Exit Sub
        End If
        
        If aa > lst(ra + 1) - 1 Then
            Exit Do
        End If
        
        sm.Contents k, bi, v
        ai = ind(aa)
        
        If ai < bi Then
            aa = aa + 1
            
        ElseIf ai = bi Then
            val(aa) = val(aa) + sf * v
            aa = aa + 1
            k = sm.NextKey(k, smr)
            
        Else
            Shuffle aa, 1, ra
            val(aa) = sf * v
            ind(aa) = bi
            aa = aa + 1
            k = sm.NextKey(k, smr)
        End If
    Loop
    
    ' Count elements remaining in smrow
    kc = k
    Do
        n = n + 1
        kc = sm.NextKey(kc, smr)
    Loop Until kc = -1
    
    Shuffle aa, n, ra   ' Make space
    
    Do
        sm.Contents k, bi, v
        val(aa) = sf * v
        ind(aa) = bi
        aa = aa + 1
        k = sm.NextKey(k, smr)
    Loop Until k = -1
End Sub

'
' Lead diagonal Row Dot Product (c is position of diagonal)
'
'Public Function LeadRowDotVec(r As Long, c As Long, vec() As Double) As Double
'    Dim i As Long, j As Long
'    Dim res As Double
'
'    For i = lst(r) To lst(r + 1) - 1
'        j = ind(i)
'        If j < c Then
'            res = res + val(i) * vec(j)
'        Else
'            Exit For
'        End If
'    Next i
'
'    LeadRowDotVec = res
'End Function
'
' Lag diagonal Row Dot Product
'
'Public Function LagRowDotVec(r As Long, c As Long, vec() As Double) As Double
'    Dim i As Long, j As Long
'    Dim res As Double
'
'    For i = lst(r) To lst(r + 1) - 1
'        j = ind(i)
'        If j > c Then
'            res = res + val(i) * vec(j)
'        End If
'    Next i
'
'    LagRowDotVec = res
'End Function
'
'
' Calculate dot product of row and another sparse row
'
Public Function RowDotRow(r As Long, sm As SparseMatrix, smr As Long) As Double
    Dim i As Long, e As Long, res As Double
    Dim k As Long, c As Long, v As Double
    
    i = lst(r)
    e = lst(r + 1) - 1
    k = sm.FirstKey(smr)
    
    Do
        If i > e Then
            Exit Do
        End If
        
        If k = -1 Then
            Exit Do
        End If
        
        sm.Contents k, c, v
        
        If ind(i) = c Then
            res = res + val(i) * v
            i = i + 1
            k = sm.NextKey(k, smr)
        ElseIf ind(i) < c Then
            i = i + 1
        Else
            k = sm.NextKey(k, smr)
        End If
    Loop
    
    RowDotRow = res

End Function

' Calculate dot product of row and a column of another sparsematrix

Public Function RowDotCol(r As Long, sm As SparseMatrix, smc As Long) As Double
    Dim i As Long, res As Double
    
    For i = lst(r) To lst(r + 1) - 1
        res = res + val(i) * sm.Cell(ind(i), smc)
    Next i
        
    RowDotCol = res
End Function

' Get the largest element in row r

Public Function Rmaxel(r As Long) As Double
    Dim i As Long, res As Double
    
    For i = lst(r) To lst(r + 1) - 1
        If Abs(val(i)) > Abs(res) Then
            res = val(i)
        End If
    Next i
    Rmaxel = res
End Function

' Read sparsematrix from spreadsheet range

Public Sub RangeGet(name As String)
    Dim i As Long, j As Long
    Dim Rupb As Long, cupb As Long
    Dim rng As Range
    Dim v As Double
    
    Set rng = Application.Range(name)
    Rupb = rng.Rows.Count - 1
    cupb = rng.Columns.Count - 1
    
    Init Rupb, cupb
    
    For i = 0 To Rupb
        For j = 0 To cupb
            v = rng.Cells(i + 1, j + 1)
            If v <> 0# Then
                Insert i, j, v
            End If
        Next j
    Next i
End Sub

' Read sparsematrix from spreadsheet range

Public Sub RangeTransposeGet(name As String)
    Dim i As Long, j As Long
    Dim Rupb As Long, cupb As Long
    Dim rng As Range
    Dim v As Double
    
    Set rng = Application.Range(name)
    cupb = rng.Rows.Count - 1
    Rupb = rng.Columns.Count - 1
    
    Init Rupb, cupb
    
    For i = 0 To Rupb
        For j = 0 To cupb
            v = rng.Cells(j + 1, i + 1)
            If v <> 0# Then
                Insert i, j, v
            End If
        Next j
    Next i
End Sub

' Write sparse matrix to spreadsheet range

Public Sub RangeSet(name As String)
    Dim i As Long, j As Long, k As Long
    Dim rng As Range
    Dim v As Double
    Dim ov() As Double
    
    Set rng = Application.Range(name).Resize(maxr + 1, maxc + 1)
    ReDim ov(1 To maxr + 1, 1 To maxc + 1) As Double
    
    For i = 0 To maxr
        For j = lst(i) To lst(i + 1) - 1
            k = ind(j)
            ov(i + 1, k + 1) = val(j)
        Next j
    Next i
    
    rng = ov        'One write
End Sub

' Debug print matrix

Public Sub PrintSM()
    Dim r As Long
    
    For r = 0 To maxr
        PrintSMrow r
    Next r
End Sub

' Debug print sparse row

Public Sub PrintSMrow(r As Long)
    Dim i As Long
    
    Debug.Print r; ":";
    For i = lst(r) To lst(r + 1) - 1
        Debug.Print "("; ind(i); ")"; val(i);
    Next i
    Debug.Print
End Sub

Public Function Test() As Boolean
    
    On Error GoTo errorhandler
    
    ' Check create, shuffle down and shuffle up
    
    Init 9, 9
    
    Cell(0, 0) = 101#
    Cell(0, 5) = 105#
    Cell(0, 9) = 110#
    Cell(9, 9) = 199#
'    PrintSM
    Zero 0, 0
    AddRow 5, 0, 2#
'    PrintSM
    
    Test = (size = 5) And (Cell(5, 5) = 210#)
    Exit Function
    
errorhandler:
    Test = False
End Function
