Attribute VB_Name = "LPhdr"
' Public definitions for LP, LPModel, etc

Option Explicit
Option Base 0

Public Const lpEpsilon As Double = 0.000001
Public Const lpOptimum As Long = 0     'Optimum found
Public Const lpUnbounded As Long = 1   'Unresolvable cost reducing constraint (see return2 for id)
Public Const lpInfeasible As Long = 2  'Unresolvable negative basis variable  (see return 2 for id)
Public Const lpZeroPivot As Long = 3   'Unable to Build or Update Basis matrix
Public Const lpIters As Long = 4       'Exceeded maximum iterations
Public Const lpUnknown As Long = 5     'Unknown error
Public Const InvIters As Long = 8      'Number of iters before rebuild

'Constraint types
Public Const CTLTE As Long = 0         'Less than or equal
Public Const CTGTE As Long = 1         'Greater than or equal
Public Const CTEQ As Long = 2          'Equal

' Creates instances of add-in classes

Public Function NewLPModel() As LPModel
    Set NewLPModel = New LPModel
End Function

Public Function NewSparseMatrix() As SparseMatrix
    Set NewSparseMatrix = New SparseMatrix
End Function

Public Function NewSolveLin() As SolveLin
    Set NewSolveLin = New SolveLin
End Function

Public Function NewLP() As LP
    Set NewLP = New LP
End Function

Public Function NewMO() As MO
    Set NewMO = New MO
'    NewMO.Init
End Function

' Merge sort - stable (i.e. leaves sorted lists unchanged)
' Sort first m items, optionally starting n items in, of c() by altering ord()
' Non recursive, uses aux memory, stable

Public Sub MergeSortFlt(c() As Double, ord() As Long, m As Long, Optional n As Long = 0)
    Dim w As Long, b As Long, upb As Long
    Dim sz As Long, i As Long
    Dim tmp() As Long
    Dim il As Long, im As Long, Id As Long
    Dim first As Long, last As Long

    b = LBound(ord)
    upb = UBound(ord)
        
    If m > upb + 1 - b Then
        m = upb + 1 - b
    End If

    If n >= m Then
        Exit Sub
    End If
    sz = m - n
    first = b + n
    last = m - 1 + b

    ReDim tmp(first To last)

    w = 1
    While w < sz

        For i = first To last Step 2 * w
            Dim left As Long, middle As Long, right As Integer
            left = i
            middle = i + w
            right = i + 2 * w

            If right > last + 1 Then
                right = last + 1
            End If

            If middle <= right Then
                il = left
                im = middle
                Id = left
                
                While il < middle Or im < right
                    If il < middle And im < right Then
                        If c(ord(il)) <= c(ord(im)) Then
                            tmp(Id) = ord(il)
                            il = il + 1
                        Else
                            tmp(Id) = ord(im)
                            im = im + 1
                        End If
                        
                    ElseIf il < middle Then
                            tmp(Id) = ord(il)
                            il = il + 1
                    Else
                            tmp(Id) = ord(im)
                            im = im + 1
                    End If
                    Id = Id + 1
                Wend
            End If
        Next

        For i = first To last
            ord(i) = tmp(i)
        Next

        w = w * 2
    Wend
End Sub

Public Sub MergeSortInt(c() As Long, ord() As Long, m As Long, Optional n As Long = 0)
    Dim w As Long, b As Long, upb As Long
    Dim sz As Long, i As Long
    Dim tmp() As Long
    Dim il As Long, im As Long, Id As Long
    Dim first As Long, last As Long

    b = LBound(ord)
    upb = UBound(ord)
        
    If m > upb + 1 - b Then
        m = upb + 1 - b
    End If

    If n >= m Then
        Exit Sub
    End If
    sz = m - n
    first = b + n
    last = m - 1 + b

    ReDim tmp(first To last)

    w = 1
    While w < sz

        For i = first To last Step 2 * w
            Dim left As Long, middle As Long, right As Integer
            left = i
            middle = i + w
            right = i + 2 * w

            If right > last + 1 Then
                right = last + 1
            End If

            If middle <= right Then
                il = left
                im = middle
                Id = left
                
                While il < middle Or im < right
                    If il < middle And im < right Then
                        If c(ord(il)) <= c(ord(im)) Then
                            tmp(Id) = ord(il)
                            il = il + 1
                        Else
                            tmp(Id) = ord(im)
                            im = im + 1
                        End If
                        
                    ElseIf il < middle Then
                            tmp(Id) = ord(il)
                            il = il + 1
                    Else
                            tmp(Id) = ord(im)
                            im = im + 1
                    End If
                    Id = Id + 1
                Wend
            End If
        Next

        For i = first To last
            ord(i) = tmp(i)
        Next

        w = w * 2
    Wend
End Sub

Public Sub TestLP()
    Dim a As New LP
    
    Debug.Print a.Test1
    
End Sub
