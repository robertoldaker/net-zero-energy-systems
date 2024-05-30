using System.Diagnostics;
using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class Boundary {
        private double _ia;
        private double[] _itfr;     // interconnection nodal transfers
        private string _boundnm;
        private double _pfer;
        private LP _ctrllp;
        public const int MAXCPI = 20;  // Maximum cct constraints added per iteration
        public const bool DIAGNOSE = false; 

        public delegate void AllTripsProgressHandler(BoundaryTrips.BoundaryTrip trip, int percent);
        public event AllTripsProgressHandler AllTripsProgress;

        private Loadflow _lf;
        private NodeBoundaryData _nbd;
        private BoundaryTrips _bts;

        public Boundary(Loadflow lf) {
            _lf = lf;
        }

        public double Ia {
            get {
                return _ia;
            }
        }

        public LP CtrlLp {
            get {
                return _ctrllp;
            }
            set {
                _ctrllp = value;
            }
        }

        public BoundaryTrips BoundaryTrips {
            get {
                return _bts;
            }
        }


        // Setup boundary calc and run intact network case
        public BoundaryFlowResult? SetBound(string boundname) {
            double[] ivang=null;
            int i, p;
            double[] mism;
            double kgin, kdin;
            double kgout, kdout;
            int nmax, mi;
            double gin=0, gout=0, gins=0, gouts=0;
            double din=0, dout=0, dins=0, douts=0;
            BoundaryFlowResult? bfr = null;

            _boundnm = boundname;

            var sr = _lf.StageResults.NewStage("Check base load flow");

            if ( !_lf.NetCheck()) {
                _lf.StageResults.StageResult(sr, StageResultEnum.Fail,"");
                return null;
            }

            _lf.StageResults.StageResult(sr, StageResultEnum.Pass,"");

            nmax = _lf.Nodes.Count;

            sr = _lf.StageResults.NewStage("Link to boundary table");

            _nbd = _lf.GetNodeBoundaryData(boundname);

            _lf.StageResults.StageResult(sr,StageResultEnum.Pass,$"Zones in boundary = {_nbd.Count}");

            sr = _lf.StageResults.NewStage("Calc boundary contents");

            foreach( var nw in _lf.Nodes.Objs) {
                if ( _nbd.IsInBoundary(nw) ) {
                    gin+=nw.Obj.Generation;
                    din+=nw.Obj.Demand;
                    if ( !nw.Obj.Ext ) {
                        gins+=nw.Obj.Generation;
                        dins+=nw.Obj.Demand;
                    }
                } else {
                    gout+=nw.Obj.Generation;
                    dout+=nw.Obj.Demand;
                    if (!nw.Obj.Ext) {
                        gouts+=nw.Obj.Generation;
                        douts+=nw.Obj.Demand;
                    }
                }
            }

            _pfer = gin - din;
            _ia = InterconAllowance(gin,din,din+dout); // Calc IA includig external transfers (+ve number)
            if ( _pfer < 0) {
                _ia = -_ia;
            }

            bfr = new BoundaryFlowResult(gin,din,gout,dout,_ia);

            _lf.StageResults.StageResult(sr,StageResultEnum.Pass,$"{boundname} base transfer = {_pfer:f1} IA = {_ia:f1}");

            sr = _lf.StageResults.NewStage("Calc interconnection sensitivities");

            _itfr = new double[nmax];

            // Scale internal gen & dem only
            kgin = _ia / (gins + dins);
            kdin = -kgin;
            kdout = _ia / (gouts + douts);
            kgout = -kdout;

            i=0;
            foreach(var nw in _lf.Nodes.Objs) {
                p = _lf.Nord.NodePos(i);
                if ( !nw.Obj.Ext) {
                    if ( _nbd.IsInBoundary(nw) ) {
                        _itfr[p] = kgin * nw.Obj.Generation - kdin * nw.Obj.Demand;
                    } else {
                        _itfr[p] = kgout * nw.Obj.Generation - kdout * nw.Obj.Demand;
                    }
                }
                i++;
            }

            // Calc iflows
            mism = Utilities.CopyArray(_itfr);
            _lf.UFac.Solve(_itfr, ref ivang);
            _lf.Ctrls.BoundaryCVang = ivang;
            _lf.CalcACFlows(ivang, mism);
            //
            mi = _lf.MaxMismatch(mism);

            //
            if ( Math.Abs(mism[mi])<0.1 ) {
                var node = _lf.Nodes.get(_lf.Nord.NodeId(mi));
                _lf.StageResults.StageResult(sr,StageResultEnum.Pass,$"Max mismatch {mism[mi]:g1} at {node.Obj.Code}");
            } else {
                var node = _lf.Nodes.get(_lf.Nord.NodeId(mi));
                _lf.StageResults.StageResult(sr,StageResultEnum.Pass,$"Max mismatch {mism[mi]:f1} at {node.Obj.Code}");                
            }

            sr = _lf.StageResults.NewStage("Identity boundary circuits");

            _bts = new BoundaryTrips(_lf.Branches,_nbd);

            _lf.StageResults.StageResult(sr, StageResultEnum.Pass, $"{_bts.LineNames.Count} boundary circuits with total cap {_bts.TotalCapacity:f0}");

            sr = _lf.StageResults.NewStage("Get trip list");

            CVang cvang = _lf.GetCVang();
            RunBoundaryOptimiser( cvang );
            //
            return bfr;
        }

        public int RunBoundaryOptimiser(CVang cvang) {

            BranchWrapper bw;
            NodeWrapper nw;
            double boundCap, controlCost, mm;
            int mi;

            var sr = _lf.StageResults.NewStage("Run planned transfer");

            var r1 = _lf.RunPlannedTransfer(cvang,false);
            if ( r1 == LPhdr.lpOptimum ) {
                _lf.StageResults.StageResult(sr, StageResultEnum.Pass,"");
            } else {
                _lf.StageResults.StageResult(sr, StageResultEnum.Fail, "Planned transfer failed");
                return r1;
            }

            sr = _lf.StageResults.NewStage($"Optimise {_boundnm} boundary cct limits only");

            // Add boundary circuit constraints (mflow is boundary sensitivity)

            _lf.Optimiser.CalcSetPoints();
            var mism = Utilities.CopyArray(_itfr);
            var vang = cvang.Boundary;
            _lf.CalcACFlows(vang, mism);

            _lf.CalcFreeDir(out double[] free,out int[] ord);

            foreach( var ln in _bts.LineNames) {
                bw = _lf.Branches.get(ln);
                var bIndex = _lf.Branches.getIndex(ln)+1;
                if ( _lf.Optimiser.CctSensitivity(bw, vang,null)!=0 ) {
                    _lf.Optimiser.PopulateConstraint(bw,free[bIndex], 0, cvang, false );
                }
            }

            int r2 = 0;
            r1 = _ctrllp.SolveLP(ref r2);

            // Calc loadflow
            var xfer = _lf.Optimiser.BoundCap();
            var sf = Math.Abs(xfer / _ia);
            _lf.Optimiser.CalcSetPoints();
            CalcTransfers(sf,out mism);
            _lf.CalcVang(sf,cvang,ref vang);
            _lf.CalcFlows(vang, mism);
            _lf.CalcFree(ref free, ref ord);

            //
            if ( r1 == LPhdr.lpOptimum ) {
                boundCap = _pfer + Math.Sign(_pfer)*xfer;
                controlCost = _ctrllp.Slack(_lf.Optimiser.bounzc.Id) - _ctrllp.Objective();
                _lf.StageResults.StageResult(sr,StageResultEnum.Pass,$"Boundcap = {boundCap:f1} Control cost = {controlCost:f2}");
            } else if ( r1 == LPhdr.lpInfeasible ) {
                if ( _ctrllp.Slack(r2) < -0.1) {
                    _lf.StageResults.StageResult(sr, StageResultEnum.Fail,$"Unresolvable constraint {_ctrllp.GetCname(r2)}");
                } else {
                    r1 = LPhdr.lpOptimum;
                }
            } else {
                _lf.StageResults.StageResult(sr, StageResultEnum.Fail, "Unknown optimiser fail");
            }

            if ( r1!=LPhdr.lpOptimum || DIAGNOSE ) {
                sr = _lf.StageResults.NewStage("Diagnose results");
                // output diagnosis results
                mi = _lf.MaxMismatch(mism);
                mm = mism[mi];
                nw = _lf.Nodes.get(_lf.Nord.NodeId(mi));
                if ( Math.Abs(mm) < 0.1 ) {
                    _lf.StageResults.StageResult(sr, StageResultEnum.Pass, $"Max mismatch {mm:g1} at {nw.Obj.Code}");
                } else {
                    _lf.StageResults.StageResult(sr, StageResultEnum.Pass, $"Max mismatch {mm:f1} at {nw.Obj.Code}");
                }
                _lf.Optimiser.ReportConstraints(StageResultEnum.Warn);
                _lf.ReportOverloads(free,ord);
            }

            if ( r1!=LPhdr.lpOptimum ) {
                return r1;
            }
            int iter = 0;
            int i=0;
            int cct=0;
            StageResult sr1;
            sr = _lf.StageResults.NewStage($"Optimise {_boundnm} all cct limits");
            do {
                iter++;
                i = 1;
                while( free[ord[i]] < Loadflow.OVRLD && i<= MAXCPI ) {
                    cct = ord[i];
                    bw = _lf.Branches.get(cct);
                    Console.WriteLine($"{bw.LineName} {free[cct]:f1}");
                    _lf.Optimiser.PopulateConstraint(bw,free[cct],sf,cvang,false);
                    i++;
                }

                if ( i==1 ) {
                    break;
                }

                if ( i==2 ) {
                    bw = _lf.Branches.get(cct);
                    sr1 = _lf.StageResults.NewStage($"Iter {iter} Ovrld {bw.LineName}");
                } else {
                    sr1 = _lf.StageResults.NewStage($"Iter {iter} Ovrlds {i-1}");
                }

                r1 = _ctrllp.SolveLP(ref r2);

                xfer = _lf.Optimiser.BoundCap();
                sf = Math.Abs(xfer / _ia);
                _lf.Optimiser.CalcSetPoints();
                CalcTransfers(sf,out mism);
                _lf.CalcVang(sf,cvang,ref vang);
                _lf.CalcFlows(vang, mism);
                _lf.CalcFree(ref free, ref ord);

                if ( r1 == LPhdr.lpOptimum ) {
                    boundCap = _pfer + Math.Sign(_pfer)*xfer;
                    controlCost = _ctrllp.Slack(_lf.Optimiser.bounzc.Id) - _ctrllp.Objective();
                    _lf.StageResults.StageResult(sr1, StageResultEnum.Pass, $"Boundcap = {boundCap:f1} Control cost = {controlCost:f2}");
                } else if ( r1 == LPhdr.lpInfeasible || r1 == LPhdr.lpIters ) {
                    if ( _ctrllp.Slack(r1) < -0.1) {
                        _lf.StageResults.StageResult(sr,StageResultEnum.Fail,$"Unresolvable constraint {_ctrllp.GetCname(r2)}");
                    } else {
                        _ctrllp.MatAltered();
                        r1 = LPhdr.lpOptimum;
                        boundCap = _pfer + Math.Sign(_pfer)*xfer;
                        controlCost = _ctrllp.Slack(_lf.Optimiser.bounzc.Id) - _ctrllp.Objective();
                        _lf.StageResults.StageResult(sr1, StageResultEnum.Pass,$"Boundcap = {boundCap:f1} Control cost = {controlCost:f2}");
                    }
                } else {
                    _lf.StageResults.StageResult(sr1, StageResultEnum.Fail, "Unknown optimiser fail");
                } 

                if ( r1!=LPhdr.lpOptimum || DIAGNOSE ) {
                    sr1 = _lf.StageResults.NewStage("Diagnossis results");
                    // Output diagnosis results
                    boundCap = _pfer + Math.Sign(_pfer)*xfer;
                    _lf.StageResults.StageResult(sr1, StageResultEnum.Warn, $"Boundcap = {boundCap:f1}");
                    _lf.Optimiser.ReportConstraints(StageResultEnum.Warn);
                    _lf.ReportOverloads(free, ord);
                }

                if ( r1!=LPhdr.lpOptimum ) {
                    return r1;
                }

            } while(true);

            _lf.StageResults.StageResult(sr, StageResultEnum.Pass, $"Control cost = {_ctrllp.Slack(_lf.Optimiser.bounzc.Id) - _ctrllp.Objective():f2}");
            sr = _lf.StageResults.NewStage($"{_boundnm} max transfer load flow");

            mi = _lf.MaxMismatch(mism);
            mm = mism[mi];
            boundCap = _pfer + Math.Sign(_pfer)*xfer;
            nw = _lf.Nodes.get(_lf.Nord.NodeId(mi));
            if ( Math.Abs(mm) < 0.1 ) {
                _lf.StageResults.StageResult(sr, StageResultEnum.Pass, $"Boundcap = {boundCap:f1} Max mismatch {mm:e1} = at {nw.Obj.Code}");
            } else {
                _lf.StageResults.StageResult(sr, StageResultEnum.Pass, $"Boundcap = {boundCap:f1} Max mismatch {mm:f1} = at {nw.Obj.Code}");
            }

            string ccts = _lf.Optimiser.ReportConstraints();
            _lf.ReportOverloads(free, ord);

            return r1;
        }

        public BoundaryFlowResult? RunAllBoundaryTrips(string boundaryName, out List<AllTripsResult> singleTrips, out List<AllTripsResult> doubleTrips) {
            double r;

            //
            singleTrips = new List<AllTripsResult>();
            doubleTrips = new List<AllTripsResult>();
            var bfr = SetBound(boundaryName);
            if ( bfr==null ) {
                return null;
            }

            //
            var sw = new Stopwatch();
            int i=1;
            int percent =0;
            sw.Start();

            //
            foreach( var trip in _bts.Trips) {
                if ( sw.Elapsed.TotalSeconds>0.5 ) {
                    percent = (i*100)/_bts.Trips.Count;
                    AllTripsProgress?.Invoke(trip,percent);
                    sw.Restart();
                }
                r = RunBoundaryTrip(trip);

                // Store in trip results
                if ( r == LPhdr.lpOptimum ) {
                    var xfer = _lf.Optimiser.BoundCap();
                    var capacity = _pfer + Math.Sign(_ia) * xfer;
                    var limCct = _lf.Optimiser.LimitCcts();
                    var ctrls = _lf.Ctrls.GetCtrlResults();
                    var tripResult = new AllTripsResult() { Capacity = capacity, Trip = trip, LimCct = limCct, Ctrls = ctrls};
                    if ( trip.Type == BoundaryTripType.Single) {
                        tripResult.Surplus = xfer - Math.Abs(_ia);
                        singleTrips.Add( tripResult );
                    } else {
                        tripResult.Surplus = xfer - Math.Abs(_ia)*0.5;  
                        doubleTrips.Add( tripResult);
                    }
                }
                
                i++;
            }
            AllTripsProgress?.Invoke(_bts.Trips.Last(),100);
            // sort trip results by surplus
            singleTrips = singleTrips.OrderBy(m=>m.Surplus).ToList();
            doubleTrips = doubleTrips.OrderBy(m=>m.Surplus).ToList();
            return bfr;
        }

        public BoundaryFlowResult? RunBoundaryTrip(string boundaryName, string tripName) {
            double r;

            var bfr = SetBound(boundaryName);
            var trip = _bts.GetTrip(tripName);
            if ( trip!=null ) {
                r = RunBoundaryTrip(trip);
            } else {
                throw new Exception($"Trip {tripName} not found");
            }
            return bfr;
        }

        public int RunBoundaryTrip(BoundaryTrips.BoundaryTrip trip) {
            int r1;
            StageResult sr;
            CVang tcva;
            int[] ccts;

            r1 = LPhdr.lpUnknown;
            if ( string.IsNullOrEmpty(_boundnm) ) {
                return r1;
            }

            sr = _lf.StageResults.NewStage($"Setup {_boundnm}{trip.Text}");

            var tripList = _lf.GetTripList(trip.LineNames,out ccts);
            if(!_lf.TripVectors(ccts,out tcva)) {
                if (_lf.Countac(tripList) > 0) {
                    _lf.StageResults.StageResult(sr,StageResultEnum.Fail,"Invalid trip - node disconnected?");
                    return r1;
                }
            }

            _lf.StageResults.StageResult(sr, StageResultEnum.Pass, $"{ccts.Length} circuits");

            return RunBoundaryOptimiser(tcva);

        }

        private void CalcTransfers(double sf, out double[] tfr) {
            tfr = Utilities.CopyArray(_lf.Btfr);
            for(int i=0;i<tfr.Length;i++) {
                tfr[i] += sf * _itfr[i];
            }
        }

        private double InterconAllowance( double gin, double din, double dtot) 
        {
            double x, t, y;

            if ( din<0 ) {
                x = gin -din;
            } else {
                x = gin + din;
            }
            x = 0.5 * x/ dtot;
            t = 1 - Math.Pow((x-0.5) / 0.5415,2);
            y = Math.Sqrt(t) * 0.0633 - 0.0243;
            return y * dtot;
        }
    }
}