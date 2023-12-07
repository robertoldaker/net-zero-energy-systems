/*
' Public definitions for LP, LPModel, etc

Option Explicit
Option Base 0

Public Const lpEpsilon = 0.000001
Public Const lpOptimum As Long = 0    'Optimum found
Public Const lpUnbounded As Long = 1   'Unresolvable cost reducing constraint (see return2 for id)
Public Const lpInfeasible As Long = 2  'Unresolvable negative basis variable  (see return 2 for id)
Public Const lpZeroPivot As Long = 3   'Unable to Build or Update Basis matrix
Public Const lpIters As Long = 4       'Exceeded maximum iterations
Public Const lpUnknown As Long = 5     'Unknown error
Public Const InvIters As Long = 8      'Number of iters before rebuild

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
*/
namespace SmartEnergyLabDataApi.Loadflow
{

    public static class LPhdr {
        public const double lpEpsilon = 0.000001;
        public const int lpOptimum = 0;                 // Optimum found
        public const int lpUnbounded = 1;               // Unresolvable cost reducing constraint (see return2 for id)
        public const int lpInfeasible = 2;              // Unresolvable negative basis variable  (see return 2 for id)
        public const int lpZeroPivot = 3;               // Unable to Build or Update Basis matrix
        public const int lpIters = 4;                   // Exceeded maximum iterations
        public const int lpUnknown = 5;                 // Unknown error
        public const int InvIters = 8;                  // Number of iters before rebuild

        public static LPModel NewLPModel() {
            return new LPModel();
        }

        public static SparseMatrix NewSparseMatrix() {
            return new SparseMatrix();
        }

        public static SolveLin NewSolveLin() {
            return new SolveLin();
        }

        public static LP NewLP() {
            return new LP();
        }

        public static MO NewMO() {
            return new MO();
        }

        // Merge sort - stable (i.e. leaves sorted lists unchanged)
        // Sort first m items, optionally starting n items in, of c() by altering ord()
        // Non recursive, uses aux memory, stable
        public static void MergeSortFlt( double[] c, int[] ord, int m, int n=0) {
            int w, b, upb, sz, i;
            int[] tmp;
            int il, im, Id;
            int first, last;

            //??b = 0;            
            b=1;
            upb = ord.Length-1;

            if ( m>upb + 1 -b ) {
                m = upb+1-b;
            }

            if ( n>=m ) {
                return;
            }

            sz = m-n;
            first = b+n;
            last = m-1+b;
            tmp = new int[last+1];
            w=1;
            while( w < sz ) {
                for(i=first;i<=last;i+=2*w) {
                    int left, middle, right;
                    left = i;
                    middle = i+w;
                    right = i + 2*w;

                    if ( right > last+1) {
                        right = last+1;
                    }
                    if ( middle<=right) {
                        il = left;
                        im = middle;
                        Id = left;
                        while( il < middle || im < right) {
                            if ( il < middle && im < right ) {
                                if ( c[ord[il]] <= c[ord[im]]) {
                                    tmp[Id] = ord[il];
                                    il = il + 1;
                                } else {
                                    tmp[Id] = ord[im];
                                    im = im + 1;
                                }
                            } else if ( il < middle ) {
                                tmp[Id] = ord[il];
                                il = il + 1;
                            } else {
                                tmp[Id] = ord[im];
                                im = im + 1;
                            }
                            Id = Id + 1;
                        }
                    }
                }
                for(i=first;i<=last;i++) {
                    ord[i] = tmp[i];
                }
                w = w*2;
            }
        }
    }

    public class ZeroPivotException : Exception {
        public ZeroPivotException(string msg) : base(msg) {
            
        }
    }
}