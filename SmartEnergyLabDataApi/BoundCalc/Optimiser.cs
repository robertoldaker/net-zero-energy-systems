
namespace SmartEnergyLabDataApi.BoundCalc
{
    public class Optimiser
    {
        private Boundary bo;
        private BoundCalc lf;
        private LPModel ctrlmodel;
        public LPVarDef bounvar;
        public LPConsDef[,] dceqc;
        public LPConsDef bounzc;
        public LPVarDef[,] ctrlvar;
        public LPConsDef[,] ctrlzc;
        public LPConsDef[,] ctrlmc;
        public LPConsDef[] brac;
        public LPConsDef[] cctlim;
        public int[] corder; // Save initial constraints
        public int lastcon;

        private const int NCCT = 100; // number of circuit constraints beyond minimum theoretical
        public const double XLRG = 50000; // Largest feasible transfer
        public Optimiser(BoundCalc loadflow, Boundary boundary) {
            lf = loadflow;
            bo = boundary;
        }

        public void BuildOptimiser()
        {
            int i, j, cupb, r2, ndc;
            int nn, n1,n2, na, nb;
            string ctn, dcn;
            double ctmax, ctmin, ctcst, mag;

            cupb = lf.Ctrls.Count;
            ctrlvar = new LPVarDef[cupb+1,2];
            ctrlzc = new LPConsDef[cupb+1,2];
            ctrlmc = new LPConsDef[cupb+1,2];
            cctlim = new LPConsDef[cupb+1 + NCCT];

            ctrlmodel = LPhdr.NewLPModel();

            bounvar = ctrlmodel.VarDef("bounv","bouncz",1); // Make boundary flow variable
            bounzc = ctrlmodel.ConsDef("bouncz", false, 0, new object[] { "bounv", -1});

            // Make an equality constraint for each hvdc node (<= + >= to ensure selection to basis) 
            nn = lf.Nord.nn;
            ndc = lf.Nodes.Count - nn - 2;
            dceqc = new LPConsDef[ndc+1,2];

            for(i=0;i<=ndc;i++) {
                j = lf.Nord.NodeId(lf.Nord.nn + 1 + i);
                var node = lf.Nodes.get(j).Obj;
                dcn = node.Code;
                mag = node.Generation - node.Demand;
                dceqc[i,0] = ctrlmodel.ConsDef($"{dcn}pc", false, mag, new object[0]);
                dceqc[i,1] = ctrlmodel.ConsDef($"{dcn}nc", false, -mag, new object[0]);                
            }

            i=1;
            foreach( var ctrl in lf.Ctrls.Objs) {
                var branch = ctrl.Branch;
                ctn = branch.Obj.Code;
                ctcst = -ctrl.Obj.Cost / ctrl.InjMax;
                ctmax = ctrl.InjMax;
                ctmin = -ctrl.Obj.MinCtrl / ctrl.Obj.MaxCtrl * ctmax;

                // Make positive and negative ctrl variables and constraints
                ctrlvar[i,0] = ctrlmodel.VarDef($"{ctn}pv",$"{ctn}pzc", ctcst);
                ctrlvar[i,1] = ctrlmodel.VarDef($"{ctn}nv",$"{ctn}nzc", ctcst);
                ctrlzc[i,0] = ctrlmodel.ConsDef($"{ctn}pzc", false, 0, new object[] {$"{ctn}pv",-1});
                ctrlzc[i,1] = ctrlmodel.ConsDef($"{ctn}nzc", false, 0, new object[] {$"{ctn}nv",-1});
                ctrlmc[i,0] = ctrlmodel.ConsDef($"{ctn}pmc", false, ctmax, new object[] {$"{ctn}pv",1});
                ctrlmc[i,1] = ctrlmodel.ConsDef($"{ctn}nmc", false, ctmin, new object[] {$"{ctn}nv",1});

                n1 = branch.Node1Index - nn - 1;
                n2 = branch.Node2Index - nn - 1;

                if ( n1 >= 0 ) {
                    dceqc[n1,0].augment($"{ctn}pv",1); // +ve flow is away from node
                    dceqc[n1,0].augment($"{ctn}nv",-1);
                    dceqc[n1,1].augment($"{ctn}pv",-1);
                    dceqc[n1,1].augment($"{ctn}nv",1);
                }
                if ( n2 >=0 ) {
                    dceqc[n2,0].augment($"{ctn}pv",-1); // +ve flow is towards nodes
                    dceqc[n2,0].augment($"{ctn}nv",1);
                    dceqc[n2,1].augment($"{ctn}pv",1);
                    dceqc[n2,1].augment($"{ctn}nv",-1);
                }
                i++;
            }

            cctlim[0] = ctrlmodel.ConsDef("cct0", false, XLRG, new object[]{"bounv",1});
            for(i=1;i<cctlim.Length;i++) {
                cctlim[i] = ctrlmodel.ConsDef($"cct{i}", false, 0, new object[]{});
            }

            bo.CtrlLp = ctrlmodel.MakeLP();
            for(i=1;i<cctlim.Length;i++) {
                bo.CtrlLp.SetSkip(cctlim[i].Id,true);
            }
            bo.CtrlLp.SaveCOrder(ref corder);
        }

        public void CalcSetPoints() {
            int i, n;
            double v;
            i=1;
            foreach( var ctrl in lf.Ctrls.Objs) {
                v = ctrl.Obj.MaxCtrl / ctrl.InjMax;
                ctrl.SetPoint = (bo.CtrlLp.Slack(ctrlzc[i,0].Id) - bo.CtrlLp.Slack(ctrlzc[i,1].Id)) * v;
                //?? This is commented out in the original
                //if ( ctrl.SetPoint > ctrl.Obj.MaxCtrl + LPhdr.lpEpsilon) {
                //    ctrl.SetPoint = ctrl.Obj.MaxCtrl;
                //} else if ( ctrl.SetPoint < ctrl.Obj.MinCtrl - LPhdr.lpEpsilon) {
                //    ctrl.SetPoint = ctrl.Obj.MinCtrl;
                //}
                i++;
            }            
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
                if ( bo.CtrlLp.GetSkip(i0) ) {
                    bo.CtrlLp.SetSkip(i0,false);
                    lastcon = j;
                    return i0;
                } else {
                    if ( bo.CtrlLp.Slack(i0)>ms ) {
                        ms = bo.CtrlLp.Slack(i0);
                        mi = i0;
                    }
                }
            }
            return mi;
        }

        public double BoundCap() {
            return bo.CtrlLp.Slack(bounzc.Id);
        }

        public void ResetLP() {
            int i;
            double ctmax, ctmin;

            bo.CtrlLp.TConsMat.ZeroRow(cctlim[0].Id);
            bo.CtrlLp.TConsMat.SetCell(cctlim[0].Id,bounvar.Id, 1);
            bo.CtrlLp.SetCname(cctlim[0].Id,cctlim[0].name);
            bo.CtrlLp.SetBvec(cctlim[0].Id,cctlim[0].Magnitude);

            for( i=1;i<cctlim.Length;i++) {
                bo.CtrlLp.SetSkip(cctlim[i].Id, true);
            }
            bo.CtrlLp.RestoreCOrder(corder);

            i=1;
            foreach(var ctrl in lf.Ctrls.Objs) {
                var branch = ctrl.Branch;
                if ( branch.Outaged ) {
                    ctmax = 0;
                    ctmin = 0;
                } else {
                    ctmax = ctrl.InjMax;
                    ctmin = -ctrl.Obj.MinCtrl / ctrl.Obj.MaxCtrl * ctmax;
                }
                bo.CtrlLp.SetBvec(ctrlmc[i,0].Id, ctmax);
                bo.CtrlLp.SetBvec(ctrlmc[i,1].Id, ctmin);
                i++;
            }
            lastcon = 0; // cct0 reserved
        }

        public string ReportConstraints(BoundCalcStageResultEnum result = BoundCalcStageResultEnum.Pass) {
            int i, c;
            string res="";

            for(i=0;i<cctlim.Length;i++) {
                c = cctlim[i].Id;
                if ( bo.CtrlLp.InBasis(c) ) {
                    res = res + bo.CtrlLp.GetCname(c) + ",";
                    var st = lf.StageResults.NewStage(bo.CtrlLp.GetCname(c));
                    lf.StageResults.StageResult(st,result,$"Shadow = {bo.CtrlLp.Shadow(c):F1}");
                }
            }
            return res.Substring(0,res.Length-1);
        }

        public List<string> LimitCcts() {
            int i,c;
            var results = new List<string>();
            for(i=0;i<cctlim.Length;i++) {
                c = cctlim[i].Id;
                if ( bo.CtrlLp.InBasis(c) ) {
                    results.Add(bo.CtrlLp.GetCname(c));
                }
            }
            return results;
        }
        /*
        ' Populate constraint
' pt means planned transfer constraint (independent of xfer)

Public Sub PopulateConstraint(cct As Long, ByVal freecap As Double, ByVal dir As Double, sp() As Variant, iasf As Double, ctrlva() As Variant, pt As Boolean)
    Dim i As Long
    Dim s As Double, fc As Double
    Dim cons As Long
    Dim si As Double, sf As Double
    Dim xa As Long
    
    cons = GetFreeCons()
    If cons = -1 Then
        Err.Raise vbError + 611, , "No free constraint slots"
    End If
    
    fc = freecap
    If dir < 0# Then
        si = -1#
    Else
        si = 1#
    End If
    
    xa = UBound(ctrlva, 1) ' boundary sensitivity is last entry
    
    With ctrllp
        If pt Then
            .cname(cons) = "pt" & LineName(cct)
        Else
            .cname(cons) = LineName(cct)
        End If
        
        With .TConsMat
            .ZeroRow cons
            If IsArray(ctrlva(xa)) And Not pt Then            ' Boundary sensitivity present
                s = CctSensitivity(cct, xa, ctrlva)
                .Cell(cons, bounvar.Id) = si * s / Abs(ia)   ' sensitivity to boundary xfer
                fc = fc + si * s * iasf
            End If
            
            For i = 1 To UBound(ctrlvar, 1)         ' sensistivity to +ve/-ve ctrl vars
                If IsArray(ctrlva(i)) Then
                    s = CctSensitivity(cct, i, ctrlva)
                    sf = si * s / injmax(i)
                    
'                    If Abs(sf) >= CSENS Then
                        .Cell(cons, ctrlvar(i, 0).Id) = sf
                        .Cell(cons, ctrlvar(i, 1).Id) = -sf
                        fc = fc + si * s * sp(i, 1) / cmaxc(i, 1)
'                    End If
                End If
            Next i
        End With
        .bvec(cons) = fc
        
    End With
End Sub
        */

        // Populate constraint
        // pt means planned transfer constraint (independent of xfer)
        public void PopulateConstraint(BranchWrapper branch, double freecap, double isaf, CVang cvang, bool pt) {
            int i;
            double s,fc;
            int cons;
            double si, sf;
            int xa;

            var ctrl = branch.Ctrl;
            var boundaryVang = lf.Ctrls.BoundaryCVang;

            cons = GetFreeCons();
            if ( cons == -1 ) {
                throw new Exception("No free constraint slots");
            }

            fc = freecap;

            var dir = branch.PowerFlow;
            if ( dir < 0 ) {
                si = -1;
            } else {
                si = 1;
            }
            if ( pt ) {
                bo.CtrlLp.SetCname(cons, $"pt{branch.LineName}");
            } else {
                bo.CtrlLp.SetCname(cons,branch.LineName);
            }

            var tConsMat = bo.CtrlLp.TConsMat;
            tConsMat.ZeroRow(cons);
            if ( cvang.Boundary!=null && !pt ) {                        // Boundary sensitivity present
                s = CctSensitivity(branch, cvang.Boundary, null );
                tConsMat.SetCell(cons, bounvar.Id, si * s / Math.Abs(bo.Ia));   // sensitivity to boundary xfer
                fc = fc + si * s * isaf;
            }
            i=1;

            foreach( var c in lf.Ctrls.Objs) {                                  // sensistivity to +ve/-ve ctrl vars
                var cv = cvang.Get(c);
                if ( cv!=null ) {
                    double? injMax = ( branch.Ctrl == c) ? c.InjMax : null;
                    s = CctSensitivity(branch, cv, injMax);
                    sf = si * s / c.InjMax;
//                    if ( Math.Abs(sf) >= CSENS) {
                        tConsMat.SetCell(cons, ctrlvar[i,0].Id, sf);
                        tConsMat.SetCell(cons, ctrlvar[i,1].Id, -sf);
                        fc = fc + si * s * (double) c.SetPoint / c.Obj.MaxCtrl;
//                    }
                }
                i++;
            }
            bo.CtrlLp.SetBvec(cons, fc);
        }

        /*
Public Function CctSensitivity(cct As Long, ctrl As Long, ctrlvang() As Variant) As Double
    Dim f As Double, v1 As Double, v2 As Double
        
    If xval(cct, 1) <> 0# And Not bout(cct) Then      ' ac branch
        v1 = ctrlvang(ctrl)(bn1(cct))
        v2 = ctrlvang(ctrl)(bn2(cct))
        f = (v1 - v2) * PUCONV / xval(cct, 1)
        
        If ctrl = bctrl(cct) Then       'is this circuit the qb?
            f = f + injmax(ctrl)                      ' remove effect of injection on branch flow
        End If
    Else    ' hvdc
'        f = 1#
    End If
    
    CctSensitivity = f
End Function
        */

        public double CctSensitivity(BranchWrapper branch, double[] cvang, double? injMax) {
            double f=0, v1, v2;

            if ( branch.Obj.X != 0 && !branch.Outaged ) { // ac branch
                v1 = (double) cvang[branch.Node1Index];
                v2 = (double) cvang[branch.Node2Index];
                f = (v1 - v2) * BoundCalc.PUCONV / branch.Obj.X;
                if ( injMax!=null) {            // is this circuit the qb?
                    f = f + (double) injMax;    // remove effect of injection on branch flow
                }
            } else {
                //Commented out in VB so done the same here
                //f = 1;
            }
            return f;
        }
    }
}