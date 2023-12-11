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