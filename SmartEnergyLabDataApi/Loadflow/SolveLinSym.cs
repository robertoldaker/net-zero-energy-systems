namespace SmartEnergyLabDataApi.Loadflow
{
    // Solve A.X = B by factoring sparse A = L.U with partial pivoting
    // Matrix A is symmetrical and includes only upper diagonal
    // Matrix A is assumed diagonal dominant (e.g. network admittance matrix)
    //
    // L A Dale 5 Jul 2017

    public class SolveLinSym {
        private SparseMatrix? amat;       // Matrix A
        private SparseMatrix umat;        // U = upper diagonal factor of A
        private int[] rord;               //Keep given Row ordering from non-zero counting
        private int rmax;                 // Order of amat
        private double[] scal;            // Scaling factor for each row (for relative size pivot choice)

        public SolveLinSym(SparseMatrix smat, bool copy=true) {
            int r=0, c=0, t=0, k;
            double piv=0, v=0, f;
            rmax =smat.Rupb;
            if ( copy ) {
                amat = new SparseMatrix();
                amat.Copy(smat);
                umat = new SparseMatrix();
                umat.Copy(smat);
                // include ldiag in amat
                for( r=0; r<=rmax-1;r++) {
                    k = smat.FirstKey(r); // should be diag
                    while(true) {
                        k = smat.NextKey(k,r);
                        if ( k<0 ) {
                            break;
                        }
                        smat.Contents(k,ref c,ref v);
                        amat.Addin(c,r,v);
                    }
                }
            } else {
                amat = null;
                umat = smat;
            }

            for( c=0; c<=rmax;c++) {

                k = umat.FirstKey(c); // First element of row is pivot

                if ( k<0 ) {
                    throw new ZeroPivotException($"Suitable pivot not found at column {c}");
                }
                umat.Contents(k,ref t,ref piv);

                if ( ( t!=c ) || (piv == 0) ) {
                    throw new ZeroPivotException($"Suitable pivot not found at column {c}, t={t}, piv={piv}");
                }

                while(true) {
                    k = umat.NextKey(k, c);
                    if ( k<0) {
                        break;
                    }
                    umat.Contents(k, ref r, ref v);
                    if ( v!=0) {
                        f = -v / piv;
                        umat.AddRow(r, c, f, r); // eliminate non-zero element process only upper diagonal
                    }
                }
            }
        }

        // Solve L.U.X = B
        // X can replace contents of B  
        public void Solve( double[] bvec, ref double[]? xvec) {
            int r, rr=0, k;
            int c=0;
            double v=0, s, piv=0;
            double[] yvec;

            yvec = bvec.ToArray();

            for( r=0; r<=rmax-1;r++) {
                k = umat.FirstKey(r);
                umat.Contents(k, ref rr, ref piv);

                while( true ) {
                    k = umat.NextKey(k, r);
                    if ( k==-1) {
                        break;
                    }
                    umat.Contents(k,ref rr,ref v);
                    yvec[rr] = yvec[rr] - v*yvec[r] / piv;
                }
            }

            yvec[rmax] = yvec[rmax] / umat.GetCell(rmax, rmax);

            for ( r=rmax-1; r>=0; r--) {
                s = yvec[r];
                k = umat.FirstKey(r);
                umat.Contents(k, ref c, ref piv);
                while(true) {
                    k = umat.NextKey(k, r);
                    if ( k==-1) {
                        break;
                    }
                    umat.Contents(k, ref c, ref v);
                    s = s - v*yvec[c];
                }
                yvec[r] = s / piv;
            }

            for ( r = rmax+1; r<yvec.Length; r++) {
                yvec[r] = 0;
            }
            xvec = yvec;
        }

        // Refine solution to A.X=B replacing X
        // Returns biggest absolute correction
        public double Refine( double[] bvec, double[] xvec) {
            double[] tvec, dxvec;
            double largest=0;
            int i;

            tvec = new double[rmax+1];
            dxvec = new double[rmax+1];
            amat.MultVec(xvec, ref tvec); // If X is Xtrue + deltaX then A.X gives B + deltaB
            for( i=0; i<=rmax;i++) {
                tvec[i] = tvec[i] - bvec[i]; // calc deltaB
            }

            Solve( tvec, ref dxvec);

            for ( i=0; i<=rmax; i++) {
                if ( Math.Abs(dxvec[i]) > Math.Abs(largest)) {
                    largest = dxvec[i];
                }
                xvec[i] = xvec[i] - dxvec[i]; // improve solution estimate
            }
            return largest;
        }

        // Solve (A + UxV).X = B Using Sherman-Morrison
        // Uses Y and Z already solved from A.Y = B and A.Z=U
        public void SMSolve( double[] yvec, double[] zvec, double[] vvec, double[] rvec) {
            double vy=0, vz=0, v;
            int i;

            rvec = new double[rmax+1];

            for( i=0; i<=rmax;i++) {
                v = vvec[i];
                if ( v!=0 ) {
                    vy = vy + v*yvec[i];
                    vz = vz + v*zvec[i];
                }
            }

            v = vy/ (1+vz);

            for( i=0; i<=rmax;i++) {
                rvec[i] = yvec[i] - v*zvec[i];
            }
        }

        private double Check() {
            double[] tvec, rvec=null;
            int i, j;
            double res=0, terr;

            for(i=0;i<=rmax;i++) {
                tvec = new double[rmax+1];
                tvec[i] = 1;
                Solve(tvec, ref rvec);
                amat.MultVec(rvec, ref rvec);

                for( j=0;j<=rmax;j++) {
                    terr = Math.Abs(tvec[j] - rvec[j]);
                    if ( terr > res) {
                        res = terr;
                    }
                }
            }
            return res;
        }

        public static bool Test() {
            SparseMatrix tmat;
            double res;
            tmat = new SparseMatrix(2,2);
            tmat.SetCell(0,0,1);
            tmat.SetCell(1,1,2);
            tmat.SetCell(2,2,3);
            tmat.SetCell(0,2,6);
            tmat.SetCell(1,2,5);
            SolveLinSym sls = new SolveLinSym(tmat);
            res = sls.Check();
            return res < 0.0000000001;
        }
    }
} 