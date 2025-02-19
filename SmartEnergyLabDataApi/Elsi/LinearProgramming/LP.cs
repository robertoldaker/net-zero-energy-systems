using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.Elsi.LinearProgramming
{
    // Linear Programme Object V2
    //
    // L A Dale 24 Jan 2017
    //
    // 1 Jun 2018 Support for multisegment variables
    // 4 Feb 2018 Updated use of MO object
    // 5 Feb 2018 Uses EnterBasis to swap constraints, read-only cmap remains for debug purposes
    // Dec 2020 Removed multisegment variables, added constraint names
    // 17 Nov 2023 Replaced basis sparseinverse object with lu factors of basis


    // This code solves an LP which in standard primal form is minimise Ct.X s.t. A.X <= B & X >= 0
    // where Ct is the cost of each variable X and A.X <= B represents the constraints on X
    // The code actually solves the dual form maximise Bt.Y such that At.Y = C & Y >= 0
    // Y (and At) will need to be augmented by slack variables Ys to achieve the equalities
    // The value of the primal variables X at the optimum can be derived from D (shadows of Y)
    // Primal equality constraints can be flagged using EQ() as bool (means constraint not removed from basis but does not force initialisation into basis)
    // Primal constraints can be skipped using Skip()as bool
    public class LP {
        private const int FASTCOUNT = -2; // the number of constraint candidates to be considered for basis entry.  Make -ve to consider all constraints
        //
        public int Id; // used to identify LP instance
        
        // Inputs
        private SparseMatrix atm; // Constraint matrix
        private double[] bv; // Primal constraint magnitudes
        private double[] ev; // Primal constraint magnitude modifiers
        private double[] cv; // Variable costs
        private bool[] eqc; // Primal constriant is equality
        private bool[] sk; // Sip this constraint
        private string[] vn; // Variable name
        private string[] cn; // Constraint name

        // Outputs
        private double[] dv; // Slack of contraints
        private double[] yv; // Dual variables
        
        // Internals
        private int maxiters;
        private int startsearch;
        private bool basisvalid;

        private double[] sv; // Sensitivity of dual variables to cost perturbations
        private double[] ssv; // Save of sv for the most infringed constraint
        private SparseMatrix bm;
        private SolveLin bmsolve;
        private int vmax; // number of variables-1 (column upb of Atm)
        private int cmax; // number of constraints-1 (row upb of Atm)
        private int[] cm; // Ordering of rows (constraints) in atm, bv, etc)
        private int[] vm; // map of constraints to basis variables
        private int fastsearch; // enable greedy column selection while >0

        public LP() {

        }
        
        public LP(SparseMatrix amat, double[] bvec, double[] cvec, string[] vname, string[] cname) {
            Init(amat, bvec, cvec, vname, cname);
        }

        public void Init(SparseMatrix amat, double[] bvec, double[] cvec, string[] vname, string[] cname) {

            atm = amat;
            bv = Utilities.CopyArray(bvec);
            cv = Utilities.CopyArray(cvec);
            vn = Utilities.CopyArray(vname);
            cn = Utilities.CopyArray(cname);
            vmax = atm.Cupb;
            cmax = atm.Rupb;
            sk = new bool[cmax+1];
            eqc = new bool[cmax+1];
            dv = new double[cmax+1];
            ev = new double[cmax+1];
            yv = new double[vmax+1];
            sv = new double[vmax+1];
            cm = new int[cmax+1];
            vm = new int[cmax+1];

            bm = new SparseMatrix();
            bmsolve = new SolveLin();

            maxiters = (vmax+1) * 2;

            InitCOrder();
        }

        public void InitCOrder() {
            int i;
            for(i=0;i<=cmax;i++) {
                cm[i] = i;
                vm[i] = i;
            }
            basisvalid = false;
        }

        public void SaveCOrder(ref int[] corder) {
            corder = Utilities.CopyArray(cm);
        }

        public void RestoreCOrder(int[] corder) {
            int i;
            Array.Copy(corder,cm,cm.Length);
            for( i=0;i<=cmax;i++) {
                vm[cm[i]] = i;
            }
            basisvalid = false;
        }

        // Make basis transpose from relevant rows of atm, transpose btm and make solver
        private void MakeBasis() {
            var btm = new SparseMatrix();
            btm.CopyMap(atm,cm,vmax);
            bm.Transpose(btm);
            bmsolve.Init(bm);
            basisvalid = true;
        }

        // Update basis matrix by replacing col v with constraint c, recalc bmsolve
        private void UpdateBasis(int v, int cons) {
            bm.ReplaceCol(v,atm,cons);
            bmsolve.Init(bm);
            basisvalid = true;
        }

        // Transposed constraint matrix
        public SparseMatrix TConsMat {
            get {
                return atm;
            }
        }

        // Mark inverse as invalid
        public void MatAltered() {
            basisvalid = false;
        }

        // Equality flag
        public void SetEquality(int constraint, bool flag) {
            eqc[constraint] = flag;
        }
        public bool GetEquality(int constraint) {
            return eqc[constraint];
        }

        // Skip flag
        public void SetSkip(int constraint, bool flag) {
            sk[constraint] = flag;
        }
        public bool GetSkip(int constraint) {
            return sk[constraint];
        }

        // Column map
        public int GetCmap(int column) {
            return cm[column];
        }

        public void SetCname(int constraint, string name) {
            cn[constraint] = name;
        }

        public string GetCname(int constraint) {
            return cn[constraint];
        }

        // Set a variable using a constraint
        public void EnterBasis(int var, int cons) {
            int oc, cp;
            if ( cm[var] != cons) {
                oc = cm[var]; // save old contents of var
                cp = vm[cons]; // current position of constraint
                cm[var] = cons;
                cm[cp] = oc;
                //??
                if (cons==119 && var==303) {
                    int xyz=1;
                }
                vm[cons] = var;
                if (oc==119 && cp==303) {
                    int xyz=1;
                }
                vm[oc] = cp;
            }
            basisvalid = false; // MArk basis invalid even if already in basis
        }

        // Costs
        public void SetCvec(int column, double cost) {
            cv[column] = cost;
        }
        public double GetCvec(int column) {
            return cv[column];
        }

        // Magnitudes (NB adjusts original by values)
        public void SetBvec( int column, double magnitude) {
            bv[column] = magnitude;
        }
        public double GetBvec(int column) {
            return bv[column];
        }

        // Magnitude modifiers (NB adjusts original bv values)
        public void SetEvec(int column, double magnitude) {
            ev[column] = magnitude;
        }
        public double GetEvec(int column) {
            return ev[column];
        }

        // Compute objective
        public double Objective() {
            int i, r;
            double res=0;

            for(i=0;i<=vmax;i++) {
                r = cm[i];
                res = res + (bv[r] + ev[r]) * yv[i];
                //??Console.WriteLine($"i={i,6},r={r,6},bv={bv[r],16:f6},ev={ev[r],16:f6},yv={yv[i],16:f6},res={res,16:f6}");
            }
            return res;
        }

        // Lookup constraint shadow cost
        public double Shadow(int constraint) {
            int r;

            r = vm[constraint];
            double result;
            if ( r>vmax) {
                result = 0;
            } else {
                result = yv[r];
            }
            return result;            
        }

        // Constraint in basis
        public bool InBasis(int constraint) {
            return vm[constraint] <= vmax;
        }

        // Lookup constraint slack
        public double Slack(int constraint) {
            int r;

            r = vm[constraint];

            double result;
            if ( r > vmax) {
                if ( sk[constraint]) {
                    CalcSlack(constraint);
                }
                result = dv[constraint];
            } else {
                result = 0;
            }
            return result;
        }

        // 1/Sensitivity of infringed constraint to relaxation of c
        public double ISENS(int c) {
            double result;
            if ( vm[c] > vmax) {
                result =0;
            } else {
                result = ssv[vm[c]];
            }
            return result;
        }

        // Calculate slack of primal constraint represented at Atm row r
        // returns slack, stores in dv(r), returns dual variable sensitivities in sv()
        private double CalcSlack(int r) {
            double res;
            int i,j;

            res = bv[r] + ev[r]; // capacity of constraint


            atm.RowToVec(r,ref sv);
            bmsolve.Solve(sv,ref sv);  // sensistivity of variables to constraint

            for(i=0;i<=vmax;i++) {
                j = cm[i];
                res = res - (bv[j] + ev[j]) * sv[i]; // effect objective
            }
            dv[r] = res;
            
            return res;
        }

        // Find most negative dual variable
        // Returns -1 if all positive
        private int MostNegativeVar() {
            int i, r;
            double m;

            r = -1;
            m= -LPhdr.lpEpsilon;

            for(i=0;i<=vmax;i++) {
                if ( sk[cm[i]]) {
                    Console.WriteLine("Skipped constraint in basis");
                }
                if ( yv[i] < m ) {
                    m = yv[i];
                    r = i;
                }
            }
            return r;
        }

        // Check all dual vars positive
        private bool Negprices() {
            int i;

            for(i=0;i<vmax;i++) {
                if ( yv[i] < -LPhdr.lpEpsilon) {
                    return true;
                }
            }
            return false;
        }

        // Find best replacement constraint for negative variable v
        // Returns -1 if none available
        private int BestReplacementVar( int v) {
            int i, m, r;
            double s, ms=0;

            m = -1;
            for( i=vmax+1;i<=cmax;i++) {
                r = cm[i];
                if ( !sk[r]) {
                    s = CalcSlack(r);                   //  Calc slack and sensitivities
                    if ( sv[v] < -LPhdr.lpEpsilon ) {    // Sensitivity to v must be negative
                        s = -s*sv[v];                   // Calc rate that v replacement will improve objective
                        if ( m<0 || s>ms ) {
                            ms = s;
                            m = i;
                        }
                    }
                }
            }
            return m;
        }

        // Find most negative slack
        private int MostInfringed() {
            int i,j,m,r;
            double s,ms=0;

            m = -1;
            ms = -LPhdr.lpEpsilon;
            for( i=vmax+1;i<=cmax;i++) {
                j = i+startsearch;
                if ( j>cmax) {
                    j = j - cmax + vmax;
                }
                r = cm[j];
                if ( !sk[r] ) {                    
                    s = CalcSlack(r);
                    if ( s < -LPhdr.lpEpsilon) {
                        fastsearch = fastsearch - 1;
                    }
                    if ( s < ms ) {
                        ms = s;
                        m = j;
                        ssv = Utilities.CopyArray(sv);
                    }
                    if ( fastsearch == 0 ) {
                        startsearch = m - vmax;
                        return m;
                    }
                }
            }
            return m;
        }

        // Find most constraining basis var
        // Uses saved sensitivities in ssv()
        private int FindExitVar() {
            int i, m, r;
            double f, mf=0;

            m = -1;

            for (i=0;i<=vmax;i++) {
                if ( eqc[cm[i]]) {

                } else if ( ssv[i] > LPhdr.lpEpsilon) {
                    f = yv[i] / ssv[i];

                    if ( m<0 || f<mf) {
                        m = i;
                        mf = f;
                    }
                }
            }
            return m;
        }

        // Solve the linear program starting with current column order
        public int SolveLP(ref int Return2) {
            int ev=0, xv, i;
            int iter=0, t;
            int res;
            double chk, bchk;
            
            try {
                startsearch =0;
                fastsearch = FASTCOUNT;
                res = LPhdr.lpIters;
                if ( !basisvalid) {
                    MakeBasis();
                } 

                do {
                    bmsolve.Solve(cv, ref yv); // Calc dual variables

                    xv = MostNegativeVar();

                    if ( xv>-1 ) {
                        ev = BestReplacementVar(xv);

                        if ( ev==-1) {
                            Console.WriteLine($" fail: basis variable {vn[xv]} negative");
                            Return2 = xv;
                            return LPhdr.lpUnbounded;
                        } 
                    } else {

                        ev = MostInfringed();

                        if ( ev!=-1) {
                            xv = FindExitVar();

                            if ( xv==-1 ) {
                                Console.WriteLine($" fail: unsolvable infringed constraint {cn[cm[ev]]}");
                                Return2 = cm[ev];
                                return LPhdr.lpInfeasible;
                            }
                        } else {
                            #if DEBUG
                                PrintFile.PrintVars($"Solved {iter} iterations");
                            #endif
                            return LPhdr.lpOptimum;
                        }
                    }

                    UpdateBasis(xv, cm[ev]);
                    dv[cm[ev]] = 0;
                    EnterBasis(xv, cm[ev]);
                    basisvalid = true;


                    fastsearch = FASTCOUNT;
                    iter = iter + 1;

                    //if ( iter % LPhdr.InvIters == 0 ) {
                    //    invbm.Check2(LPhdr.lpEpsilon);
                    //}

                } while( iter<=maxiters);

                Console.WriteLine($"LP {Id} fail: maximum iterations exceeded");
                Return2 = cm[ev];
                return LPhdr.lpIters;
            } catch(ZeroPivotException) {
                Console.WriteLine($"LP {Id} fail due to zero pivot");
                PrintBasis();
                return LPhdr.lpZeroPivot;
            } catch(Exception e) {
                Console.WriteLine($"LP {Id} Error {e.Message}");
                return LPhdr.lpUnknown;
            }
        }

        private void printDiag(string prepend) {
            if ( vm.Length >119 && vm[119]==303) {
                Console.WriteLine($"{prepend} vm[119]={vm[119]}");
            }
        }

        private void PrintBasis() {
            int i;

            for(i=0;i<=vmax;i++) {
                Console.Write($"{cn[i]}\t");
                if ( (i+1) % 8 ==0 ) {
                    Console.WriteLine();
                }
            }
        }


        // Test a simple LP problem
        // 3 producers with max output and match demand constraints
        public bool Test1() {
            SparseMatrix amat = new SparseMatrix();
            double[] bv = new double[7], cv = new double[3];
            string[] cn = new string[7], vn = new string[3];
            int rc1, rc2=0;
            double expected;

            amat.Init(6,2);
            cn[0] = "G1z";
            amat.SetCell(0,0,1); // Zero constraint
            cn[1] = "G1m";
            amat.SetCell(1,0,-1); // Capacity constraint

            cn[2] = "G2z";
            amat.SetCell(2,1,1);
            cn[3] = "G2m";
            amat.SetCell(3,1,-1);

            cn[4] = "G3z";
            amat.SetCell(4,2,1);
            cn[5] = "G3m";
            amat.SetCell(5,2,-1);

            cn[6] = "Dem";
            amat.SetCell(6,0,1); // Demand constraint
            amat.SetCell(6,1,1);
            amat.SetCell(6,2,1);

            vn[0] = "G1";
            bv[1] = 200;    // cap
            cv[0] = 10;     // cost

            vn[1] = "G2";
            bv[3] = 300;
            cv[1] = 20;

            vn[2] = "G3";
            bv[5] = 400;
            cv[2] = 30;

            bv[6] = -600; // demand

            Init(amat, bv, cv, vn, cn);

            // place zero constraints in basis
            EnterBasis(0,0);
            EnterBasis(1,2);
            EnterBasis(2,4);

            expected = bv[1]*cv[0] + bv[3]*cv[1] + cv[2] * ( -bv[6] - bv[1] - bv[3]);

            rc1 = SolveLP(ref rc2);
            bool result = (rc1 == LPhdr.lpOptimum) && (Objective() == -expected);
            return result;
        }

        public void PrintState() {
            int i;
            PrintFile.PrintVars("atm");
            atm.PrintState();
            // btm
            PrintFile.PrintVars("btm");
            bm.PrintState();
                
            for(i=0;i<=cmax;i++) {
                PrintFile.PrintVars("i", i, "sk", sk[i], "eqc", eqc[i], "dv", dv[i], "ev", ev[i], "cm", cm[i], "vm", vm[i], "bv", bv[i], "cn", cn[i]);
            }

            for(i=0;i<=vmax;i++) {
                PrintFile.PrintVars("i", i, "yv", yv[i], "sv", sv[i], "cv", cv[i], "vn", vn[i]);
            }

        }

    }
}