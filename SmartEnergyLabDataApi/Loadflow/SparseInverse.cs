/*
' Basis inverse manager
' Encapsulates Initialisation, Column update and Use of a sparse matrix
' L Dale 19 Jan 2017
' Routine to purge zero elements 11 Feb 2017
' Test routine added 2 Jan 2019

Option Explicit
Option Base 0

#Const debugmsg = False         'True gives some minor memory management info
Const ZEROPIVOT As Long = vbObjectError + 600

Private btmat As SparseMatrix   'transpose of the matrix (sparse columns)
Private lusolve As SolveLin     'solver of BT.X=B
Public binv As SparseMatrix    'the sparse basis inverse
Private rmax As Long            'order of A - 1
Private Const epsilon As Double = 0.00000001

' initialise the basis matrix inverse
' solves Bt.X = V where V is successive unit vectors
' Each X is a non sparse column of Btinverse (or row of Binverse)

Public Sub Init(mtranspose As SparseMatrix)
    Dim i As Long, j As Long
    Dim tvec() As Double
    Dim sz As Long
        
    rmax = mtranspose.Cupb
    Set btmat = mtranspose
    sz = btmat.size
    
    Set binv = New SparseMatrix
    binv.Init rmax, rmax, sz / (rmax + 1) ' assume inverse at least as big as btmat
    
    Set lusolve = New SolveLin
    lusolve.Init btmat, True
    
    For i = 0 To rmax
        ReDim tvec(rmax) As Double
        tvec(i) = 1#
        lusolve.Solve tvec, tvec        ' tvec column of basis transpose = row of inverse basis
        
        For j = 0 To rmax
            If Abs(tvec(j)) > epsilon Then
                binv.Insert i, j, tvec(j)   ' Find might be avoided here
            End If
        Next j
    Next i
End Sub

' X = Binv.B
' X can replace B

Public Sub MultVec(bvec() As Double, xvec() As Double)
    
    binv.MultVec bvec, xvec
    
End Sub

' Refine inverse multvec
' Returns largest correction

Public Function Refine(bvec() As Double, xvec() As Double) As Double
    Dim tvec() As Double, dxvec() As Double
    Dim largest As Double
    Dim i As Long, k As Long, c As Long, v As Double
    
    ReDim tvec(rmax) As Double, dxvec(rmax) As Double
    
    ' Calc basis.xvec (given we have the basis transpose btmat)
    For i = 0 To rmax
        k = btmat.FirstKey(i)
        While k <> -1
            btmat.Contents k, c, v
            tvec(c) = tvec(c) + v * xvec(i)
            k = btmat.NextKey(k, i)
        Wend
    Next i
    
   ' If X is Xtrue + deltaX then Basis.X gives B + deltaB
    
    For i = 0 To rmax
        tvec(i) = tvec(i) - bvec(i) ' calc deltaB
    Next i
    
    binv.MultVec tvec, dxvec
    
    For i = 0 To rmax
        If Abs(dxvec(i)) > Abs(largest) Then
            largest = dxvec(i)
        End If
        xvec(i) = xvec(i) - dxvec(i)    ' Improve solution estimate
    Next i
    Refine = largest
End Function

' R = Binv.AT

Public Sub MultMat(atmat As SparseMatrix, smr As Long, rvec() As Double)
    Dim i As Long
    
    ReDim rvec(rmax) As Double
    
    For i = 0 To rmax
        rvec(i) = binv.RowDotRow(i, atmat, smr)
    Next i
End Sub

' Adjust basis inverse to reflect row change in basis transpose
' Uses Sherman-Morrison Lema (A+UxV)^-1 = A^-1 - ZxW/sf
' Where Z = A^-1.U, W = V.A-1, sf = 1 + V.Z
' In this implementation V is a unit vector
' Purge flushes zero elements from the sparse inverse

Public Sub Update(br As Long, atmat As SparseMatrix, ar As Long, Optional purge As Boolean = True)
    Dim u As SparseMatrix
    Dim w As SparseMatrix
    Dim z() As Double
    Dim sf As Double, i As Long
    
    Set u = New SparseMatrix
    u.CopyRow atmat, ar                 ' Get new row of basis transpose
    u.AddMatRow 0, btmat, br, -1#       ' Subtract existing basis row
    
    Set w = New SparseMatrix
    w.CopyRow binv, br                  ' Get row of sparse inverse
    
    ReDim z(rmax) As Double
    
    For i = 0 To rmax
        z(i) = binv.RowDotRow(i, u, 0)  ' Z = Binv.U
    Next i
    
    sf = 1# + z(br)
    If Abs(sf) < epsilon Then
        Err.Raise ZEROPIVOT, , "Zero pivot found in basis update"
    End If
    
    For i = 0 To rmax
        If z(i) <> 0# Then
            binv.AddMatRow i, w, 0, -z(i) / sf
        End If
    Next i
    
    btmat.ReplaceRow br, atmat, ar      ' Update btmat
    
'    binv.PrintSM

    If purge Then
        i = binv.dopurge()
    End If
#If debugmsg Then
        Debug.Print i; "purged"
#End If
End Sub

' Returns biggest cell error of B.invB - I

Public Function Check() As Double
    Dim i As Long, j As Long
    Dim res As Double, terr As Double
    
    For i = 0 To rmax
        For j = 0 To rmax
            terr = btmat.RowDotRow(i, binv, j)
            If i = j Then
                terr = terr - 1#
            End If
            If Abs(terr) > res Then
                res = terr
            End If
        Next j
    Next i
    
    Check = res
End Function

' Efficient check

Public Sub Check2(threshold As Double)
    Dim i As Long, j As Long
    Dim terr As Double
    
    For i = 0 To rmax
        For j = 0 To rmax
            terr = btmat.RowDotRow(i, binv, j)
            If i = j Then
                terr = terr - 1#
            End If
            If Abs(terr) > threshold Then
                Init btmat
                Exit Sub
            End If
        Next j
    Next i
End Sub

' Test update on simple test case

Public Function Test() As Boolean
    Dim tmat As SparseMatrix
    Dim bmat As SparseMatrix
    
    On Error GoTo errorhandler
    
    Set tmat = New SparseMatrix
    Set bmat = New SparseMatrix
    
    tmat.Init 3, 2
    
    With tmat
        .Cell(0, 0) = 1#
        .Cell(1, 1) = 1#
        .Cell(2, 2) = 1#
        .Cell(3, 0) = 1#
        .Cell(3, 1) = 2#
        .Cell(3, 2) = 3#
    End With
    
    bmat.Copy tmat, 0, 2
    
    Init bmat
    
    Update 2, tmat, 3, True
    
    Test = (Check < 0.0000000001)
    Exit Function
    
errorhandler:
    Test = False
End Function
*/
namespace SmartEnergyLabDataApi.Loadflow
{
    // Basis inverse manager
    // Encapsulates Initialisation, Column update and Use of a sparse matrix
    // L Dale 19 Jan 2017
    // Routine to purge zero elements 11 Feb 2017
    // Test routine added 2 Jan 2019
    public class SparseInverse {

        private SparseMatrix btmat;     // transpose of the matrix (sparse columns)
        private SolveLin lusolve;       // Solver of BT.X =B
        public SparseMatrix binv;      // the sparse basis inverse
        private int rmax;               // order of A - 1
        private const double epsilon = 0.00000001;

        public SparseInverse() {
            
        }

        // initialise the basis matrix inverse
        // solves Bt.X = V where V is successive unit vectors
        // Each X is a non sparse column of Btinverse (or row of Binverse)
        public void Init(SparseMatrix mtranspose) {
            int i, j;
            double[] tvec;
            int sz;

            rmax = mtranspose.Cupb;
            btmat = mtranspose;
            sz = btmat.size;

            binv = new SparseMatrix(rmax, rmax, (double) sz / (double) (rmax+1)); // assume inverse at least as big as btmat
            lusolve = new SolveLin();
            lusolve.Init(btmat, true);

            for(i=0;i<=rmax;i++) {
                tvec = new double[rmax+1];
                tvec[i] = 1;
                lusolve.Solve(tvec, ref tvec); // tvec column of basis transpose = row of inverse basis

                for(j=0;j<=rmax;j++) {
                    if ( Math.Abs(tvec[j]) > epsilon) {
                        binv.Insert(i, j, tvec[j]); //  Find might be avoided here
                    }
                }
            }            
        }

        public void ReInit() {
            Init(btmat);
        }

        // X = Binv.B
        // X can replace B
        public void MultVec( double[] bvec, ref double[] xvec) {
            binv.MultVec(bvec, ref xvec);
        }

        // Refine inverse multvec
        // Returns largest correction
        public double Refine( double[] bvec, double[] xvec) {
            double[] tvec, dxvec;
            double largest=0, v=0;
            int i,k, c=0;

            tvec = new double[rmax+1];
            dxvec = new double[rmax+1];

            // Calc basis.xvec (given we have the basis transpose btmat)
            for(i=0; i<=rmax; i++) {
                k = btmat.FirstKey(i);
                while( k!=-1) {
                    btmat.Contents(k,ref c,ref v);
                    tvec[c] = tvec[c] + v*xvec[i];
                    k = btmat.NextKey(k,i);
                }
            }

            // If X is Xtrue + deltaX then Basis.X gives B + deltaB
            for(i=0;i<=rmax;i++) {
                tvec[i] = tvec[i] - bvec[i]; // calc deltaB
            }

            binv.MultVec(tvec, ref dxvec);

            for(i=0;i<=rmax;i++) {
                if ( Math.Abs(dxvec[i]) > Math.Abs(largest) )  {
                    largest = dxvec[i];
                }
                xvec[i] = xvec[i] - dxvec[i]; // Improve solution estimate
            }
            return largest;
        }

        // R = Binv.AT
        public void MultMat(SparseMatrix atmat, int smr, ref double[] rvec) {
            int i;
            rvec = new double[rmax+1];

            for(i=0;i<=rmax;i++) {
                rvec[i] = binv.RowDotRow(i, atmat, smr);
            }
        }

        // Adjust basis inverse to reflect row change in basis transpose
        // Uses Sherman-Morrison Lema (A+UxV)^-1 = A^-1 - ZxW/sf
        // Where Z = A^-1.U, W = V.A-1, sf = 1 + V.Z
        // In this implementation V is a unit vector
        // Purge flushes zero elements from the sparse inverse
        public void Update(int br, SparseMatrix atmat, int ar, bool purge = true) {
            SparseMatrix u, w;
            double[] z;
            double sf;
            int i;

            u = new SparseMatrix();
            u.CopyRow(atmat, ar);           // Get new row of basis transpose
            u.AddMatRow(0, btmat, br, -1);  // Subtract existing basis row

            w = new SparseMatrix();
            w.CopyRow(binv, br);            // Get row of sparse inverse

            z = new double[rmax+1];

            for( i=0;i<=rmax;i++) {
                z[i] = binv.RowDotRow(i,u,0);  // Z = Binv.U
            }

            sf = 1 + z[br];
            if ( Math.Abs(sf) < epsilon) {
                throw new ZeroPivotException("Zero pivot found in basis update");
            }

            for(i=0;i<=rmax;i++) {
                if ( z[i]!=0) {
                    binv.AddMatRow(i,w,0,-z[i]/sf);
                }
            }

            btmat.ReplaceRow(br, atmat, ar);    // Update btmat

            // binv.PrintSM()
            if ( purge ) {
                i = binv.dopurge();
            }
        }

        // Returns biggest cell error of B.invB - I
        public double Check() {
            int i, j;
            double res=0, terr;

            for(i=0;i<=rmax;i++) {
                for(j=0;j<=rmax;j++) {
                    terr = btmat.RowDotRow(i, binv, j);
                    if ( i==j) {
                        terr = terr - 1;
                    }
                    if ( Math.Abs(terr) > res) {
                        res = terr;
                    }
                }
            }
            return res;
        }

        // Efficient check
        public void Check2(double threshold) {
            int i, j;
            double terr;

            for(i=0;i<=rmax;i++) {
                for( j=0;j<=rmax;j++) {
                    terr = btmat.RowDotRow(i, binv, j);
                    if ( i==j) {
                        terr = terr-1;
                    }
                    if ( Math.Abs(terr) > threshold) {
                        Init(btmat);
                        Console.WriteLine("Reform inverse");
                        return;
                    }
                }
            }
        }

        // Test update on simple test case
        public bool Test() {
            SparseMatrix tmat, bmat;

            tmat = new SparseMatrix();
            bmat = new SparseMatrix();

            tmat.Init(3,2);

            tmat.SetCell(0,0,1);
            tmat.SetCell(1,1,1);
            tmat.SetCell(2,2,1);
            tmat.SetCell(3,0,1);
            tmat.SetCell(3,1,2);
            tmat.SetCell(3,2,3);

            bmat.Copy(tmat, 0, 2);
            Init(bmat);
            Update( 2, tmat, 3, true);
            bool result = Check() < 0.0000000001;
            return result;
        }

        public void PrintState() {
            PrintFile.PrintVars("rmax", rmax, "epsilon", epsilon);
            PrintFile.PrintVars("btmat");
            if ( btmat!=null ) {
                btmat.PrintState();
            }
            PrintFile.PrintVars("binv");
            if ( binv!=null) {
                binv.PrintState();
            }
        }

    }


}