
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class Optimiser
    {
        private BoundCalc bc;
        private LPModel ctrlmodel;
        public LP ctrllp;
        public LPVarDef boun;          // boundary transfer variable
        public LPConsDef[,] dceqc;     // dcnode equality contraints
        public LPConsDef[] cctlim;     // circuit capacity contraints
        public int[] corder; // Save initial constraints
        public int lastcon;

        private const int NCCT = 100; // number of circuit constraints beyond minimum theoretical
        public const double XLRG = 50000; // Largest feasible transfer
        public const double CSENS = 1; // smallest recognised MW flow for max control action

        private Optimiser(BoundCalc loadflow) {
            bc = loadflow;
        }

        public static Optimiser BuildOptimiser(BoundCalc bc)
        {
            var optimiser = new Optimiser(bc);
            optimiser.buildOptimiser();
            return optimiser;
        }


        private void buildOptimiser()
        {
            int i, j, cupb, ndc;
            int nn, n1,n2;
            string ctn, dcn;
            double ctmax, ctmin, ctcst, mag;
            Node node;

            cupb = bc.Ctrls.Count;
            cctlim = new LPConsDef[cupb + NCCT + 1];

            ctrlmodel = LPhdr.NewLPModel();

            boun = ctrlmodel.PairDef("boun", pWelfare:1, nWelfare:-1,maxValue:XLRG);

            // Make an equality constraint for each hvdc node (<= + >= to ensure selection to basis)
            nn = bc.Nord.nn;                            // last ac node
            ndc = bc.Nodes.Count - nn - 2;              // number of dcnodes - 1
            if ( ndc >= 0 ) {
                dceqc = new LPConsDef[ndc+1,2];
            }

            for(i=0;i<=ndc;i++) {
                j = bc.Nord.NodeId(bc.Nord.nn + 1 + i);
                node = bc.Nodes.get(j).Obj;
                dcn = node.Code;
                mag = node.GetGeneration(bc.TransportModel) - node.Demand;
                dceqc[i,0] = ctrlmodel.ConsDef($"{dcn}pc", LPhdr.CTLTE, mag, new object[0]);
                dceqc[i,1] = ctrlmodel.ConsDef($"{dcn}nc", LPhdr.CTGTE, mag, new object[0]);                
            }

            foreach( var ct in bc.Ctrls.Objs) {
                var br = ct.Branch;
                ctn = br.Obj.Code;
                ctcst = ct.Obj.Cost / ct.InjMax;
                ctmax = ct.InjMax;
                ctmin = ct.Obj.MinCtrl / ct.Obj.MaxCtrl * ctmax;

                // Make positive and negative ctrl variables and constraints
                ct.CtVar = ctrlmodel.PairDef(ctn, pWelfare:-ctcst,nWelfare:-ctcst, maxValue:ctmax,minValue:ctmin);

                n1 = br.pn1 - nn - 1;
                n2 = br.pn2 - nn - 1;

                if ( n1 >= 0 ) {
                    dceqc[n1,0].augment(ctn, 1); // +ve flow is away from node
                    dceqc[n1,1].augment(ctn, 1);
                }
                if ( n2 >=0 ) {
                    dceqc[n2,0].augment(ctn, -1); // +ve flow is towards node
                    dceqc[n2,1].augment(ctn, -1);
                }
            }

            for( i=0; i<cctlim.Length;i++) {
                cctlim[i] = ctrlmodel.ConsDef($"cct{i}", LPhdr.CTLTE, 0, new object[0]);
            }

            ctrllp = ctrlmodel.MakeLP();
            for ( i=0;i<cctlim.Length;i++) {
                ctrllp.SetSkip(cctlim[i].Id, true);
            }

            ctrllp.SaveCOrder(ref corder);
        }

        public int GetFreeCons() {
            int i, j, i0, n, mi;
            double ms=0;

            mi = -1;
            n=cctlim.Length-1;

            for(i=1;i<=n;i++) {
                j = lastcon + i;
                if ( j > n ) {
                    j = j-n;
                }
                i0 = cctlim[j].Id;
                if ( ctrllp.GetSkip(i0) ) {
                    ctrllp.SetSkip(i0,false);
                    lastcon = j;
                    return i0;
                } else {
                    if ( ctrllp.Slack(i0)>ms ) {
                        ms = ctrllp.Slack(i0);
                        mi = i0;
                    }
                }
            }
            return mi;
        }

        public double BoundCap() {
            var pfer = bc.ActiveBound.PlannedTransfer;
            return pfer + Math.Sign(pfer) * boun.Value(ctrllp);
        }

        public double ControlCost() {
            return boun.Value(ctrllp) - ctrllp.Objective();
        }
        

        public void ResetLP() {
            int i, cvarpmc, cvarnmc;
            double ctmax, ctmin;

            for( i=0;i<cctlim.Length;i++) {
                ctrllp.SetSkip(cctlim[i].Id, true);
                ctrllp.RestoreCOrder(corder);
            }

            foreach(var ct in bc.Ctrls.Objs) {
                var br = ct.Branch;
                if ( br.BOut ) {
                    ctmax = 0;
                    ctmin = 0;
                } else {
                    ctmax = ct.InjMax;
                    ctmin = -ct.Obj.MinCtrl / ct.Obj.MaxCtrl * ctmax;
                }
                cvarpmc = ct.CtVar.Vpv.Vmc.Id;
                cvarnmc = ct.CtVar.Vnv.Vmc.Id;
                ctrllp.SetBvec(cvarpmc, ctmax);
                ctrllp.SetBvec(cvarnmc, ctmin);
            }
        }

        public string ReportConstraints(out double [,]shadows) {
            int i, c;
            string res="";
            BranchWrapper br;

            shadows = new double[bc.Branches.Count+1,2];

            if ( ctrllp == null ) {
                return "";
            }

            for( i=0;i<cctlim.Length;i++) {
                c = cctlim[i].Id;
                if ( ctrllp.InBasis(c)) {
                    var cname = ctrllp.GetCname(c);
                    res+= cname + ",";
                    if ( cname.Substring(0,2) == "pt" ) {
                        var key = cname.Substring(2);
                        br = bc.Branches.get(key);
                    } else {
                        br = bc.Branches.get(cname);
                    }
                    shadows[br.Index, 1] = ctrllp.Shadow(c);
                }
            }

            if ( res == "") {
                return res;
            } else {
                return res.Substring(0,res.Length-1);
            }
        }

        public List<string> Limitccts() {
            int i,c;
            var results = new List<string>();
            for(i=0;i<cctlim.Length;i++) {
                c = cctlim[i].Id;
                if ( ctrllp.InBasis(c) ) {
                    results.Add(ctrllp.GetCname(c));
                }
            }
            return results;
        }

        // Populate constraint
        // pt means planned transfer constraint (independent of xfer)
        public void PopulateConstraint(BranchWrapper branch, double freecap, double dir, double isaf, double[]?[] ctrlva, bool pt) {
            int i;
            double s,fc;
            int cons;
            double si, sf;
            int xa;

            cons = GetFreeCons();
            if ( cons == -1 ) {
                throw new Exception("No free constraint slots");
            }

            fc = freecap;

            if ( dir < 0 ) {
                si = -1;
            } else {
                si = 1;
            }

            xa = ctrlva.Length-1;

            if ( pt ) {
                ctrllp.SetCname(cons, $"pt{branch.LineName}");
            } else {
                ctrllp.SetCname(cons,branch.LineName);
            }

            var tConsMat = ctrllp.TConsMat;
            tConsMat.ZeroRow(cons);
            if ( ctrlva[xa]!=null && !pt ) {                // Boundary sensitivity present
                s = CctSensitivity(branch, xa, ctrlva );
                sf = si * s / Math.Abs(bc.ActiveBound.InterconAllowance);
                tConsMat.SetCell(cons, boun.Vpv.Id, sf);    // sensitivity to boundary +xfer
                tConsMat.SetCell(cons, boun.Vnv.Id, -sf);   // sensitivity to boundary +xfer
                fc = fc + si * s * isaf;
            }

            foreach( var ct in bc.Ctrls.Objs) {              // sensistivity to +ve/-ve ctrl vars
                i = ct.Index;
                if ( ctrlva[i]!=null ) {
                    s = CctSensitivity(branch, i, ctrlva);
                    sf = si * s / ct.InjMax;
                    if ( Math.Abs(s) >= CSENS) { 
                        tConsMat.SetCell(cons,ct.CtVar.Vpv.Id, sf);
                        tConsMat.SetCell(cons,ct.CtVar.Vnv.Id, -sf);
                        fc = fc + si * s * ct.GetSetPoint(BoundCalc.SPAuto) / ct.Obj.MaxCtrl;
                    }
                }
            }
            ctrllp.SetBvec(cons, fc);
        }

        public double CctSensitivity(BranchWrapper br, int ctnum, double[]?[] ctrlvang) {
            double f=0, v1, v2;
            double[] cvang;

            cvang = ctrlvang[ctnum];

            f = br.flow(cvang, BoundCalc.SPZero, true);

            var ct = br.Ctrl;
            if ( !(ct==null) ) {
                if ( ct.Index == ctnum ) {
                    f = f + ct.InjMax;
                }

            }

            return f;
        }
    }
}