using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.BoundCalc
{

    // Solve A.X = B by factoring sparse A = L.U with partial pivoting
    //
    // L A Dale 7 Jan 2017
    // 11 Feb 2017 Modified to store L in empty parts of U
    // 2 Jan 2019 Test routine added

    public class SolveLin 
    {
        private SparseMatrix amat; // Matrix A
        private SparseMatrix umat; // U = upper diagonal factor of A
        private int[] rord; // row ordering from partial pivoting
        private int rmax; // Order of amat

        // Private scal() As Double            'Scaling factor for each row (for relative size pivot choice)
        // factor A = L.U on a copy or optionally in place, L stored in leading diagonal of U
        // a copy is required if Refine is to be used
        public void Init(SparseMatrix smat, bool Copy = true)
        {
            int r, c, p, rr;
            double piv, v, f;
            double spiv;
            int t;

            rmax = smat.Rupb;

            rord = new int[rmax+1];
            for(r=0;r<=rmax;r++) {
                rord[r] = r;
            }

            if ( Copy ) {
                amat = smat;
                umat = new SparseMatrix();
                umat.Copy(amat);
            } else {
                amat = null;
                umat = smat;
            }

            for( c=0;c<=rmax-1;c++) { // Don't need to search last column
                // Find pivot row with the biggest relative non-zero element in this column
                p = -1;
                piv = 0; // spiv=0

                for( r=c;r<=rmax;r++) { // Find biggest pivot
                    v = umat.Lookup(rord[r],c);
                    if ( Math.Abs(v) > Math.Abs(piv) ) { // Abs(spiv * scal(r)) Then ' NB relative pivot
                        p = r;
                        piv = v;
                        // spiv = v/ scal(r)
                    }
                }

                if ( p<0 ) {
                    throw new ZeroPivotException($"Suitable pivot not found at column {c}");
                }

                if ( p!=c ) {      // if pivot not on diagonal alter rord
                    t = rord[c];
                    rord[c] = rord[p];
                    rord[p] = t;
                }

                for(r=c+1;r<=rmax;r++) {
                    rr = rord[r];
                    v = umat.Lookup(rr,c);
                    if ( v!=0 ) {
                        f = -v / piv;
                        umat.AddRow(rr,rord[c], f, c); // eliminate non-zero element on upper diagonal
                        umat.Insert(rr,c,f);           // store lower diag element in eliminated umat slot
                    }
                }
            }

            if ( umat.Lookup(rord[rmax], rmax)==0 ) { // Check last pivot
                throw new ZeroPivotException("Suitable pivot not found in triangulisation");
            }
        }
        // Solve L.U.X = B
        // X can replace contents of B
        public void Solve(double[] bvec, ref double[] xvec ) {
            int r, rr, k=0;
            int c=0;
            double v=0, s, piv=0;
            double[] yvec;

            yvec = new double[rmax+1];

            yvec[0] = bvec[rord[0]];

            for(r=1;r<=rmax;r++) {
                rr = rord[r];
                s = bvec[rr];

                k = umat.FirstKey(rr);

                do {
                    if ( k==-1) {
                        break;
                    }
                    umat.Contents(k,ref c,ref v);
                    if ( c>=r ) {
                        break;
                    }
                    s = s+ v*yvec[c];
                    k = umat.NextKey(k, rr);
                } while(true);
                yvec[r] = s;
            }

            yvec[rmax] = yvec[rmax] / umat.GetCell(rord[rmax], rmax);

            for( r=rmax-1;r>=0;r--) {
                rr = rord[r];
                s = yvec[r];
                if ( !umat.FFindKey(rr, r, ref k)) {
                    throw new ZeroPivotException("Suitable pivot not found in solve");
                }

                umat.Contents(k, ref c, ref piv);
                do {
                    k = umat.NextKey(k,rr);
                    if ( k == -1) {
                        break;
                    }
                    umat.Contents( k, ref c, ref v);
                    s = s - v * yvec[c];
                } while(true);
                yvec[r] = s/piv;
            }
            // Can do this since yvec is allocated at top of function
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

            amat.MultVec(xvec, ref tvec); //  If X is Xtrue + deltaX then A.X gives B + deltaB

            for(i=0;i<=rmax;i++) {
                tvec[i] = tvec[i] - bvec[i]; // Calc deltaB
            }

            Solve(tvec, ref dxvec);

            for(i=0;i<=rmax;i++) {
                if ( Math.Abs(dxvec[i]) > Math.Abs(largest)) {
                    largest = dxvec[i];
                }
                xvec[i] = xvec[i] - dxvec[i]; // Improve solution estimate
            }
            return largest;
        }

        // Solve (A + UxV).X = B Using Sherman-Morrison
        // Uses Y and Z already solved from A.Y = B and A.Z=U
        public void SMSolve( double[] yvec, double[] zvec, double[] vvec, double[] rvec) {
            double vy=0, vz=0, v;
            int i;

            rvec = new double[rmax+1];

            for(i=0;i<=rmax;i++) {
                v = vvec[i];
                if ( v!=0 ) {
                    vy = vy + v*yvec[i];
                    vz = vz + v*zvec[i];
                }
            }

            v = vy / (1+vz);

            for(i=0;i<=rmax;i++) {
                rvec[i] = yvec[i] - v * zvec[i];
            }
        }

        // Returns biggest cell error of A.invA - I
        private double Check() {
            double[] tvec, rvec=null;
            int i,j;
            double res=0, terr;

            for(i=0;i<=rmax;i++) {
                tvec = new double[rmax+1];
                tvec[i] = 1;

                Solve(tvec, ref rvec);
                amat.MultVec(rvec,ref rvec);

                for(j=0;j<=rmax;j++) {
                    terr = Math.Abs(tvec[j] - rvec[j]);
                    if (terr > res) {
                        res = terr;
                    }
                }
            }
            return res;
        }

        public bool Test() {
            SparseMatrix tmat;
            double res;

            tmat = new SparseMatrix();
            tmat.Init(2,2);
            tmat.SetCell(0,0,1);
            tmat.SetCell(1,1,2);
            tmat.SetCell(2,2,3);
            tmat.SetCell(0,2,6);
            tmat.SetCell(2,1,5);
            //
            Init(tmat);

            res = Check();
            bool result = res < 0.0000000001;
            return result;
        }



    }
}