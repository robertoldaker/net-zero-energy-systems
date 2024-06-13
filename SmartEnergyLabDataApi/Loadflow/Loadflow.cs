using System.Linq;
using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class Loadflow : IDisposable {

        // Data from the database - nodes, branches and controls
        private DataAccess _da;
        private Dataset _dataset;
        private Nodes _nodes;
        private double[] _btfr;
        private Branches _branches;
        private Ctrls _ctrls;

        // Used to store results        
        private StageResults _stageResults;

        //
        private Optimiser opt;
        private Boundary bo;
        //
        public const double PUCONV = 10000; // Conversion for % on 100MVA to pu on 1MVA
        public const double MINCAP = 1;     // Branch flows ignored if cap below MINCAP        

        public SparseMatrix admat;
        public SolveLinSym _ufac;
        private NodeOrder _nord;

        public const int BPL= 31;  // bits per long
        public const double MINFREE = 1; 
        public const double SCAP = 1;           // Ignore branches with cap < 1 MW
        public const double CSENS = 0.001;      // Require MW flow for max action
        public const double OVRLD = -0.05;


        public Loadflow(int datasetId) {
            _da = new DataAccess();
            _dataset = _da.Datasets.GetDataset(datasetId);
            if ( _dataset == null) {
                throw new Exception($"Cannot find dataset with id=[{datasetId}]");
            }
            _stageResults = new StageResults();
            bo = new Boundary(this);
        }

        public void Dispose()
        {
            _da.Dispose();
        }

        public Dataset Dataset {
            get {
                return _dataset;
            }
        }

        public StageResults StageResults {
            get {
                return _stageResults;
            }
        }

        public Nodes Nodes {
            get {
                return _nodes;
            }
        }

        public Branches Branches {
            get {
                return _branches;
            }
        }

        public Ctrls Ctrls {
            get {
                return _ctrls;
            }
        }

        public Boundary Boundary {
            get {
                return bo;
            }
        }

        public NodeOrder Nord {
            get {
                return _nord;
            }
        }

        public SolveLinSym UFac
        {
            get {
                return _ufac;
            }
        }

        public Optimiser Optimiser 
        {
            get {
                return opt;
            }
        }

        public double[] Btfr {
            get {
                return _btfr;
            }
        }
        

        public bool NetCheck() {

            StageResult sr=null;
            try {
                sr = _stageResults.NewStage("Link to nodes table");
                // create nodes wrapper
                _nodes = new Nodes(_da,_dataset.Id);
                _stageResults.StageResult( sr, StageResultEnum.Pass, $"Count {_nodes.Count}");

                // create branches wrapper
                sr = _stageResults.NewStage("Link to branches table");
                _branches = new Branches(_da,_dataset.Id,_nodes);
                _stageResults.StageResult( sr, StageResultEnum.Pass, $"Count {_branches.Count}");
                sr = _stageResults.NewStage("Link to branches table");

                // create ctrl wrapper
                sr = _stageResults.NewStage("Link to controls table");
                _ctrls = new Ctrls(_da,_dataset.Id,_branches);
                _stageResults.StageResult( sr, StageResultEnum.Pass, $"Count {_ctrls.Count}");
                // No nodes or branches then return
                if ( _nodes.Count==0 || _branches.Count==0 )                 {
                    return false;
                }

            } catch ( Exception e) {
                if ( sr!=null ) {
                    _stageResults.StageResult(sr,StageResultEnum.Fail,e.Message);
                }
                throw;
            }

    
            // Check network
            sr = _stageResults.NewStage("Network connected");
            if ( _branches.IsDisconnected(_nodes) ) {
                var msg = $"Disconnected network detected";
                _stageResults.StageResult( sr, StageResultEnum.Fail, msg);
                //??throw new Exception(msg);
                return false;
            } else {
                _stageResults.StageResult( sr, StageResultEnum.Pass, "");
            }

            // 
            sr = _stageResults.NewStage("Node order");
            _nord = new NodeOrder(_nodes, _branches);
            foreach( var branch in _branches.Objs) {
                branch.Node1Index = _nord.NodePos(_nodes.getIndex(branch.Obj.Node1.Code));
                branch.Node2Index = _nord.NodePos(_nodes.getIndex(branch.Obj.Node2.Code));
            }
            var avNonZeroPerRow = ((double) _nord.nz)/ _nodes.Count;
            var fillIn = 100*((double) _nord.fz - (double) _nord.nz) / (double) _nord.nz;
            _stageResults.StageResult(sr, StageResultEnum.Pass, $"Av non-zero per row = {avNonZeroPerRow:f2}, Fill-in = {fillIn:f1}%");

            //
            sr = _stageResults.NewStage("Build admittance matrix and factorise");
            var admat = AdmittanceMat();

            //
            _ufac = new SolveLinSym(admat, false);
            var refNodeId = _nord.NodeId(_nord.nn);
            _stageResults.StageResult(sr,StageResultEnum.Pass, $"Reference node = {_nodes.get(refNodeId).Obj.Code}");

            //
            sr = _stageResults.NewStage("Base case load flow (ac part)");
            //
            BaseTransfers(ref _btfr);
            var mism = Utilities.CopyArray(_btfr);

            double[] bvang=null;
            _ufac.Solve(_btfr, ref bvang);
            CalcACFlows(bvang, mism);

            //
            var mm = mism[_nord.nn];
            _stageResults.StageResult(sr, StageResultEnum.Pass, $"No control mismatch at ref node {mm:F1}");

            //
            sr = _stageResults.NewStage("Calculate control sensitivities");
            _ctrls.BaseCVang = bvang;
            foreach(var ctrl in _ctrls.Objs) {
                CtrlSensitivity(ctrl);
            }

            //
            opt = new Optimiser(this,bo);

            //
            opt.BuildOptimiser();

            _stageResults.StageResult(sr, StageResultEnum.Pass, $"");

            return true;
        }

        public NodeBoundaryData GetNodeBoundaryData(string boundaryName) {
            var bndry = _da.Loadflow.GetBoundary(boundaryName);
            NodeBoundaryData nbd=null;
            if ( bndry!=null) {
                var bd = _da.Loadflow.GetBoundaryZones(bndry.Id);
                if ( bd==null ) {
                    throw new Exception($"Unexpected empty list of boundary zones for boundary [{boundaryName}]");                    
                }
                nbd = new NodeBoundaryData(bd);
            } else {
                throw new Exception($"Unknown boundary [{boundaryName}]");
            }
            return nbd;
        }

        public void Subnets(int[] snet) {
            int i, j1, j2;
            bool chng;

            for(i=1;i<snet.Length;i++) {
                snet[i] = i;
            }

            do {
                chng = false;
                foreach( var b in _branches.Objs) {
                    j1 = _nodes.getIndex(b.Obj.Node1.Code);
                    j2 = _nodes.getIndex(b.Obj.Node2.Code);
                    if ( snet[j1] < snet[j2] ) {
                        snet[j2] = snet[j1];
                        chng = true;
                    } else if ( snet[j2] < snet[j1] ) {
                        snet[j1] = snet[j2];
                        chng = true;
                    }
                }
            } while( chng);
        }

        // Set up admittance matrix
        // Default lf=true selects most non-zero row as reference
        private SparseMatrix AdmittanceMat( bool lf=true) {
            int nupb, nr, nc;
            double y;
            if ( lf ) {
                nupb = _nord.nn-1; // exclude reference node
            } else {
                nupb = _nord.nn;
            }
            double epr = ((double) _nord.fz) / (nupb+1);
            SparseMatrix adm = new SparseMatrix(nupb, nupb, epr);

            foreach( var b in _branches.Objs) {
                if ( b.Obj.X!=0 ) {
                    y = PUCONV / b.Obj.X;
                    if ( b.Node1Index > b.Node2Index ) { // ensure upper diagonal
                        nr = b.Node2Index;
                        nc = b.Node1Index;
                    } else {
                        nr = b.Node1Index;
                        nc = b.Node2Index;
                    }
                    //
                    adm.Addin(nr,nr,y);
                    if ( nc <= nupb ) {
                        adm.Addin(nc, nc, y);
                        adm.Addin(nr, nc, -y);
                    }
                }
            }
            return adm;
        }

        // Setup transfer vector
        public void BaseTransfers(double[] tvec) {
            int i, p;
            tvec = new double[_nodes.Count];
            i=1;
            foreach( var node in _nodes.Objs) {
                p = _nord.NodePos(i);
                tvec[p] = node.Obj.Generation - node.Obj.Demand;
                i++;
            }
        }

        // Calculate ac flows and associated mismatches
        // call with mism = transfers
        public void CalcACFlows(double[]? vang, double[] mism) {
            int nupb;
            double y, v1, v2, f;

            nupb = _nord.nn;
            foreach( var b in _branches.Objs) {
                if ( b.Obj.X!=0 && !b.Outaged) { // ac branch
                    y = PUCONV / b.Obj.X;
                    v1 = vang[b.Node1Index];
                    v2 = vang[b.Node2Index];
                    f = (v1 - v2) * y;
                    mism[b.Node1Index] = mism[b.Node1Index] - f;
                    mism[b.Node2Index] = mism[b.Node2Index] + f;
                    b.BFlow=f;
                }
            }
        }

        // Calculate all flows and mismatches from vangs and setpoints
        // call with mism = transfers
        public void CalcFlows(double[]? vang,  double[] mism) {
            int nupb;
            double y, v1, v2, f;

            nupb = _nord.nn;
            foreach(var b in _branches.Objs) {
                if ( b.Outaged ) {
                    f = 0;
                } else if ( b.Ctrl == null ) {// Uncontrolled ac branch
                    y = PUCONV / b.Obj.X;
                    v1 = vang[b.Node1Index];
                    v2 = vang[b.Node2Index];
                    f = (v1 - v2) * y;
                } else {
                    switch( b.Ctrl.Obj.Type ) {
                        case LoadflowCtrlType.QB: {
                            y = PUCONV / b.Obj.X;
                            v1 = vang[b.Node1Index];
                            v2 = vang[b.Node2Index];
                            f = (v1 - v2 + (double) b.Ctrl.SetPoint) * y;
                            break;
                        }
                        case LoadflowCtrlType.HVDC: {
                            f = (double) b.Ctrl.SetPoint;
                            break;
                        }
                        default: {
                            throw new Exception("Unknown control type");
                        }
                    }
                }
                mism[b.Node1Index] = mism[b.Node1Index] - f;
                mism[b.Node2Index] = mism[b.Node2Index] + f;
                b.PowerFlow = f;
            }

            // Store mismatches in node wrappers
            for( int i=0;i<mism.Length;i++) {
                int index = _nord.NodeId(i);
                var nodeWrapper = _nodes.get(index);
                nodeWrapper.Mismatch = mism[i];
            }
        }

        public void CalcNodeCol(double[] nquant, double[] tquant) {
            tquant = new double[_nodes.Count];
            int i=1,j;
            foreach( var node in _nodes.Objs) {
                j = _nord.NodePos(i);
                tquant[i] = nquant[i];
            }
        }

        public void CalcBranchCol(double[] nquant, double[] tquant) {
            tquant = new double[_branches.Count];
            int i=1,j;
            foreach( var b in _branches.Objs) {
                if ( nquant[i] < 9999 ) {
                    tquant[i] = nquant[i];
                }
            }
        }

        // Calculate total vang using control setpoints
        // iasf scales the interconnection allowance used in ctrlva(0)
        public void CalcVang(double isaf, CVang ctrlva, ref double[]? vang) {
            int i, j;
            double[] tv;
            double sf2;

            vang = Utilities.CopyArray(ctrlva.Base);

            if ( isaf!=0 && ctrlva.Boundary!=null ) {
                tv = ctrlva.Boundary;
                for(i=0;i<vang.Length;i++) {
                    vang[i] = vang[i] + tv[i] * isaf;
                }
            }

            foreach( var ctrl in _ctrls.Objs) {
                if ( ctrl.SetPoint!=0 && ctrlva.Get(ctrl)!=null ) {
                    tv = ctrlva.Get(ctrl);
                    sf2 = (double) ctrl.SetPoint / ctrl.Obj.MaxCtrl;
                    if ( vang!=null ) {                        
                        for(j=0;j<vang.Length;j++) {
                            vang[j] = vang[j] + tv[j] * sf2;
                        }
                    }
                }
            }
        }

        public int MaxMismatch(double[] mism) {
            int i;
            double maxm=0, amism;
            int maxi=0;

            for(i=0;i<mism.Length;i++) {
                amism = Math.Abs(mism[i]);
                if ( amism > maxm) {
                    maxm = amism;
                    maxi=i;
                }
            }
            return maxi;
        }

        public string LineName(int i) {
            var branch = _branches.get(i);
            return branch.LineName;
        }

        public CVang GetCVang() {
            var cvang = new CVang();
            cvang.Base = _ctrls.BaseCVang;
            int i=1;
            foreach( var ctrl in _ctrls.Objs ) {
                cvang.Set(ctrl, ctrl.CVang);
                i++;
            }
            cvang.Boundary = _ctrls.BoundaryCVang;
            return cvang;
        }

        // Calc intact network sensitivites to max controls
        private void CtrlSensitivity(CtrlWrapper ctrl) {
            double[] tvec,vang=null;

            tvec = new double[_nodes.Count];
            if ( ctrl.Branch.Node1Index<=_nord.nn || ctrl.Branch.Node2Index<=_nord.nn) {
                tvec[ctrl.Branch.Node1Index] = -ctrl.InjMax;
                tvec[ctrl.Branch.Node2Index] = ctrl.InjMax;
                _ufac.Solve(tvec,ref vang);
                ctrl.CVang = vang;
            } else {
                ctrl.CVang = null;
            }
        }

        // Calculate cct free capacity
        public void CalcFree(ref double[] free, ref int[] ord) {
            int i, n;
            double si;

            n = _branches.Objs.Count;
            free = new double[n+1];
            ord = new int[n+1];
            i = 1;
            foreach(var b in _branches.Objs) {
                if ( b.PowerFlow < 0 ) {
                    si = -1;
                } else {
                    si = 1;
                }
                if ( (b.Obj.Cap > SCAP) && b.PowerFlow!=null) {
                    free[i] = b.Obj.Cap - (double)b.PowerFlow*si;
                } else {
                    free[i] = 99999;
                }
                // Store in branch wrapper
                b.FreePower = free[i];
                ord[i] = i;
                i++;
            }
            // False means its no zerobased index
            LPhdr.MergeSortFlt(free, ord, n, 0, false);
        }

        public void CalcFreeDir(out double[] free, out int[] ord) {
            int i, n;
            double si;

            n = _branches.Objs.Count;
            free = new double[n+1];
            ord = new int[n+1];
            i = 1;
            foreach(var b in _branches.Objs) {

                if ( b.BFlow < 0 ) {
                    si = -1;
                } else {
                    si = 1;
                }
                if ( b.Obj.Cap > SCAP && b.PowerFlow!=null) {
                    free[i] = b.Obj.Cap - (double) b.PowerFlow*si;
                } else {
                    free[i] = 99999;
                }
                ord[i] = i;
                i++;
            }
            LPhdr.MergeSortFlt(free, ord, n, 0 , false);
        }

        
        public int RunPlannedTransfer( CVang cva, bool save = true) {
            int i;
            int mi;
            double mm;
            int r1, r2=0;
            double[] mism;
            int[] ord=null;
            double[] free=null;
            double[] vang=null;

            var sr = _stageResults.NewStage("Balance hvdc nodes");
            opt.ResetLP();
            r1 = bo.CtrlLp.SolveLP(ref r2);

            if ( r1 == LPhdr.lpOptimum ) {
                _stageResults.StageResult(sr, StageResultEnum.Pass,$"Control cost = {bo.CtrlLp.Slack(opt.bounzc.Id) - bo.CtrlLp.Objective():F2}");
            } else {
                if ( r1 == LPhdr.lpInfeasible ) {
                    _stageResults.StageResult(sr, StageResultEnum.Fail, $"Unresolvable constraint {bo.CtrlLp.GetCname(r2)}");
                } else {
                    _stageResults.StageResult(sr, StageResultEnum.Fail, $"Uknown optimiser fail");
                }
            }

            sr = _stageResults.NewStage("Minimum control load flow");
            // Calc loadflow
            opt.CalcSetPoints();
            mism = Utilities.CopyArray(_btfr);

            CalcVang(0,cva,ref vang);
            CalcFlows(vang, mism);
            CalcFree(ref free, ref ord);
            mi = MaxMismatch(mism);
            mm = mism[mi];
            var node = _nodes.get(_nord.NodeId(mi));
            if ( Math.Abs(mm) < 0.1) {
                _stageResults.StageResult(sr,StageResultEnum.Pass, $"Max mismatch {mm:#.#E+00} at {node.Obj.Code}");
            } else {
                _stageResults.StageResult(sr,StageResultEnum.Warn, $"Max mismatch {mm:#.#} at {node.Obj.Code}");
            }

            if ( r1!=LPhdr.lpOptimum) {
                // output diagnosis results
                ReportOverloads(free,ord);
                return r1;
            }

            sr = _stageResults.NewStage("Resolve AC constraints");
            do {
                i = 1;
                while( free[ord[i]] < OVRLD ) {
                    var b = _branches.get(ord[i]);
                    Console.WriteLine($"{LineName(ord[i])}, {free[ord[i]]:#.#}");
                    opt.PopulateConstraint(b,free[ord[i]], 0, cva, true);
                    i = i + 1;
                }

                if ( i == 1) {
                    break;
                }

                r1 = bo.CtrlLp.SolveLP(ref r2);
                opt.CalcSetPoints();
                mism = _btfr;
                CalcVang(0,cva,ref vang);
                CalcFlows(vang,mism);
                CalcFree(ref free, ref ord);

                if ( r1 != LPhdr.lpOptimum ) {
                    if ( r1 == LPhdr.lpInfeasible ) {
                        _stageResults.StageResult(sr, StageResultEnum.Fail, $"Unresolvable constraint {bo.CtrlLp.GetCname(r2)}");
                    } else {
                        _stageResults.StageResult(sr, StageResultEnum.Fail, $"Unknown optimiser fail");
                    }
                    opt.ReportConstraints(StageResultEnum.Warn);
                    ReportOverloads(free, ord);
                    return r1;
                }
            } while( true);
            _stageResults.StageResult(sr, StageResultEnum.Pass,$"Control cost = {bo.CtrlLp.Slack(opt.bounzc.Id) - bo.CtrlLp.Objective()}");

            sr = _stageResults.NewStage("Planned transfer load flow");

            mi = MaxMismatch(mism);
            mm = mism[mi];
            node = _nodes.get(_nord.NodeId(mi));
            if ( Math.Abs(mm) < 0.1) {
                _stageResults.StageResult(sr,StageResultEnum.Pass, $"Max mismatch {mm:#.#E+00} at {node.Obj.Code}");
            } else {
                _stageResults.StageResult(sr,StageResultEnum.Warn, $"Max mismatch {mm:#.#} at {node.Obj.Code}");
            }

            opt.ReportConstraints();
            ReportOverloads(free, ord);

            return r1;

        }

        public void RunLoadFlow(CVang cva, bool save=true) {
            double[] mism, vang=null, free=null;
            object[] tout;
            int i, mi, st;
            int[] ord=null;
            double mm;

            var sr = _stageResults.NewStage($"Run loadflow");

            mism = _btfr;
            CalcVang(0,cva,ref vang);
            CalcFlows(vang, mism);
            CalcFree(ref free, ref ord);

            mi = MaxMismatch(mism);
            mm = mism[mi];

            var node = _nodes.get(_nord.NodeId(mi));
            if ( Math.Abs(mm) < 0.1) {
                _stageResults.StageResult(sr,StageResultEnum.Pass, $"Max mismatch {mm:#.#E+00} at {node.Obj.Code}");
            } else {
                _stageResults.StageResult(sr,StageResultEnum.Warn, $"Max mismatch {mm:#.#} at {node.Obj.Code}");
            }

            ReportOverloads(free, ord);

        }

        public void ReportOverloads(double[] free, int[] ord) {
            int i;
            StageResult sr;
            i = 1;
            while( free[ord[i]] < OVRLD ) {
                var b = _branches.get(ord[i]);
                sr = _stageResults.NewStage(b.LineName);
                _stageResults.StageResult(sr, StageResultEnum.Fail, $"Overload {free[ord[i]]:#.#} on capacity {b.Obj.Cap:#.#}");
                i = i + 1;
            }
        }

        // Process base case
        public void RunBaseCase(string setPnm) {

            #if DEBUG
            RunTests();
            Console.WriteLine("All tests passed!!");
            #endif


            // Check network
            var sr = _stageResults.NewStage("Check network");
            var result = NetCheck();
            if ( result ) {
                _stageResults.StageResult(sr,StageResultEnum.Pass,"");
            } else {
                _stageResults.StageResult(sr,StageResultEnum.Fail,"Base network not valid");
                return;
            }
            CVang cva = GetCVang();
            //
            if ( setPnm == "Auto" ) {
                RunPlannedTransfer(cva);
            } else {
                RunLoadFlow();
            }
        }

/*
Public Sub RunTrip(fname As String, setpnm As String)
    Dim ccts() As Long
    Dim n As Long, st As Long
    Dim sm() As Double, tcva() As Variant
    Dim pflow() As Variant, csp() As Variant
    
    st = ControlForm.Newstage("Check network")
    
    If Not NetCheck() Then
        ControlForm.StageResult st, STFAIL, "Base network not valid"
        Exit Sub
    Else
        ControlForm.StageResult st, STPASS, ""
    End If
    
    st = ControlForm.Newstage("Setup trip " & fname)
    
    n = SelectedCcts(ccts)
    
    If n = 0 Then
        ControlForm.StageResult st, STFAIL, "No trip circuits selected"
        Exit Sub
    End If
    
    If Not TripVectors(ccts, tcva) Then
        If Countac(ccts) > 0 Then
            ControlForm.StageResult st, STFAIL, "Invalid trip - node disconnected?"
            Exit Sub
        End If
    End If
    ControlForm.StageResult st, STPASS, CStr(n) & " circuits"
    
    If setpnm = "Auto" Then
        RunPlannedTransfer fname, tcva, pflow
    Else
        ctab.GetColumn setpnm, csp
        RunLoadFlow fname, tcva, csp, pflow
    End If
End Sub
*/
        public void RunTrip(List<string> linkNames, string setpnm="Auto") {

            int[] ccts;
            CVang tcva;

            var sr = _stageResults.NewStage("Check network");

            if ( !NetCheck() ) {
                _stageResults.StageResult(sr, StageResultEnum.Fail, "Base network not valid");
                return;
            } else {
                _stageResults.StageResult(sr, StageResultEnum.Pass, "");                
            }

            sr = _stageResults.NewStage("Setup trip");

            var tripList = GetTripList(linkNames,out ccts);
            var n = ccts.Length;

            if ( tripList.Count==0 ) {
                _stageResults.StageResult(sr, StageResultEnum.Fail,"No trip circuits defined");
                return;
            }

            if ( !TripVectors(ccts, out tcva)) {
                if ( Countac(ccts) > 0) {
                    _stageResults.StageResult(sr,StageResultEnum.Fail,"Invalid trip - node disconnected?");
                    return;
                }
            }

            _stageResults.StageResult(sr,StageResultEnum.Pass,$"{n} circuits");

            if ( setpnm == "Auto") {
                RunPlannedTransfer(tcva);
            }


        }

        
        // Setup transfer vector
        private void BaseTransfers(ref double[] tvec) {
            int i, p, vupb;
            vupb = _nodes.Count;
            tvec = new double[vupb];

            i=0;
            foreach( var node in _nodes.Objs) {
                p = _nord.NodePos(i);
                tvec[p] = node.Obj.Generation - node.Obj.Demand;
                i++;
            }

        }       

        public void RunLoadFlow() {
            
        }

        public List<BranchWrapper> GetTripList(List<string> linkNames, out int[] ccts) {
            _branches.ResetOutaged();
            var tripList = new List<BranchWrapper>();
            foreach( var ln in linkNames) {
                var bw = _branches.get(ln);
                if ( bw!=null) {
                    bw.Outaged = true;
                    tripList.Add(bw);
                }
            }
            ccts = new int[tripList.Count];
            int i=0;
            foreach( var bw in tripList) {
                ccts[i] = _branches.getIndex(bw.Obj.LineName)+1;
                i++;
            }
            //
            return tripList;
        }

        /*
' Calculate Trip base and contrl vectors from intact versions
' Return true if vectors different from base case

Public Function TripVectors(ccts() As Long, tcvang() As Variant) As Boolean
    Dim i As Long
    Dim tv() As Double, ntv() As Double
    Dim sensmat() As Double
    
    ReDim tcvang(UBound(cvang, 1)) As Variant
    
    If Not CalcSensMat(ccts, sensmat) Then ' Might be dc ccts
                
        For i = 0 To UBound(cvang, 1)
            tcvang(i) = cvang(i)
        Next i
        TripVectors = False
        Exit Function
        
    Else
        For i = 0 To UBound(cvang, 1)
            If IsArray(cvang(i)) Then
                tv = cvang(i)
                TripSolve ccts, sensmat, tv, ntv
                tcvang(i) = ntv
            Else
                tcvang(i) = cvang(i)
            End If
        Next i
    End If
    TripVectors = True
End Function
        */
        public bool TripVectors(int[] ccts, out CVang tcvang) {
            double[,] sensmat;
            double[] tv, ntv;


            if ( !CalcSensMat(ccts, out sensmat)) { // might be dc ccts
                tcvang = GetCVang();
                return false;                
            } else {
                // Base
                tcvang = new CVang();
                if ( _ctrls.BaseCVang!=null ) {
                    tv = _ctrls.BaseCVang;
                    TripSolve(ccts, sensmat, tv, out ntv);
                    tcvang.Base = ntv;
                }
                // Ctrls
                int i=0;
                foreach( var ctrl in _ctrls.Objs) {
                    if ( ctrl.CVang!=null ) {
                        tv = ctrl.CVang;
                        TripSolve(ccts, sensmat, tv, out ntv);
                        tcvang.Set(ctrl,ntv);
                    } else {
                        tcvang.Set(ctrl,ctrl.CVang);
                    }
                    i++;
                }
                // Boundary
                if ( _ctrls.BoundaryCVang!=null) {
                    tv = _ctrls.BoundaryCVang;
                    TripSolve(ccts, sensmat, tv, out ntv);
                    tcvang.Boundary = ntv;
                }
            }
            return true;
        }

/*
' Calc injection matrix = (I - Fsens)^-1 for ac elements
' Returns false if (I-Fsens) is singular or no ac ccts

Private Function CalcSensMat(ccts() As Long, sensmat() As Double) As Boolean
    Dim tvec() As Double
    Dim mat() As Double
    Dim res As Variant
    Dim i As Long, j As Long, n As Long
    Dim c As Long, d As Double
    
    n = Countac(ccts)
    
    If n = 0 Then
        CalcSensMat = False
        Exit Function
    End If
    
    ReDim mat(n - 1, n - 1) As Double
    ReDim sensmat(n - 1, n - 1) As Double
    
    For j = 0 To n - 1
        ReDim tvec(UBound(gen, 1) - 1) As Double
        c = ccts(j)
        tvec(bn1(c)) = 1#
        tvec(bn2(c)) = -1#
        ufac.Solve tvec, tvec
        For i = 0 To n - 1
            c = ccts(i)
            If i = j Then
                mat(i, j) = 1#
            End If
            mat(i, j) = mat(i, j) - (tvec(bn1(c)) - tvec(bn2(c))) * PUCONV / xval(c, 1)
        Next i
    Next j
    d = Excel.WorksheetFunction.MDeterm(mat)
    If Abs(d) <= lpEpsilon Then
        CalcSensMat = False
        Exit Function
    End If
    
    res = Excel.WorksheetFunction.MInverse(mat)
    
    If n = 1 Then
        sensmat(0, 0) = res(1)
    Else
        For i = 0 To n - 1
            For j = 0 To n - 1
                sensmat(i, j) = res(i + 1, j + 1)
            Next j
        Next i
    End If
    CalcSensMat = True
End Function
*/
        private bool CalcSensMat(int[] ccts, out double[,] sensmat) {

            var n = Countac(ccts);

            if ( n == 0 ) {
                sensmat = new double[0,0];
                return false;
            }

            var mat = new double[n,n];
            sensmat = new double[n,n];

            double[] tvec;
            int c;
            BranchWrapper bw;
            for(int j=0;j<n;j++) {
                tvec = new double[_nodes.Count];
                bw = _branches.get(ccts[j]);
                tvec[bw.Node1Index] = 1;
                tvec[bw.Node2Index] = -1;
                _ufac.Solve(tvec, ref tvec);
                for (int i=0;i<n;i++) {
                    c = ccts[i];
                    bw = _branches.get(c);
                    if ( i==j ) {
                        mat[i,j] = 1;
                    }
                    mat[i,j] = mat[i,j] - (tvec[bw.Node1Index] - tvec[bw.Node2Index]) * PUCONV / bw.Obj.X;
                }
            }
            //
            double d = Utilities.Determinant(mat);
            if ( Math.Abs(d)<=LPhdr.lpEpsilon) {
                return false;
            }
            //
            var res = Utilities.MatrixInverse(mat);

            for( int i=0;i<n;i++) {
                for( int j=0;j<n;j++) {
                    sensmat[i,j] = res[i,j];
                }
            }
            //
            return true;
        }

        // Count number of ac circuits
        public int Countac(List<BranchWrapper> trips) {
            int n = 0;
            foreach( var trip in trips) {
                if ( trip.Obj.X != 0 ) { // Count ac ccts
                    n = n + 1;
                }
            }                   
            return n;
        }

        // Count number of ac circuits
        public int Countac(int[] ccts) {
            int n = 0;
            for(int i=0;i<ccts.Length;i++) {
                var bw = _branches.get(ccts[i]);
                if ( bw.Obj.X != 0 ) { // Count ac ccts
                    n = n + 1;
                }
            }                   
            return n;
        }

        /*
        ' Calculate trip tvang from intact ovang

Private Sub TripSolve(ccts() As Long, sensmat() As Double, ovang() As Double, tvang() As Double)
    Dim nupb As Long, n As Long
    Dim c As Long, i As Long, j As Long
    Dim f() As Double, inj() As Double
    Dim tvec() As Double
    
    
    nupb = UBound(sensmat, 1)
    ReDim f(nupb) As Double
    ReDim inj(nupb) As Double
    ReDim tvec(UBound(ovang)) As Double
    
    ' Calc original flows
    n = 0
    For i = 0 To UBound(ccts)
        c = ccts(i)
        If xval(c, 1) <> 0# Then
            f(n) = (ovang(bn1(c)) - ovang(bn2(c))) * PUCONV / xval(c, 1)
            n = n + 1
        End If
    Next i
    
    ' Calc injections
    n = 0
    For i = 0 To UBound(ccts)
        c = ccts(i)
        If xval(c, 1) <> 0# Then
            For j = 0 To nupb
                inj(n) = inj(n) + sensmat(n, j) * f(j)
            Next j
            tvec(bn1(c)) = tvec(bn1(c)) + inj(n)
            tvec(bn2(c)) = tvec(bn2(c)) - inj(n)
            n = n + 1
        End If
    Next i
    
    ufac.Solve tvec, tvec
    
    tvang = ovang
    
    For i = 0 To UBound(ovang)
        tvang(i) = tvang(i) + tvec(i)
    Next i
End Sub

        */

        public void TripSolve(int[] ccts, double[,] sensmat, double[] ovang, out double[] tvang) {
            int nupb, n, c, i, j;
            double[] f, inj, tvec;

            nupb = sensmat.GetLength(0)-1;
            f=new double[nupb+1];
            inj = new double[nupb+1];
            tvec = new double[ovang.Length];

            // Calc original flows
            n = 0;
            BranchWrapper bw;
            for(i=0;i<ccts.Length;i++) {
                c = ccts[i];
                bw = _branches.get(c);
                if (bw.Obj.X!=0) {
                    f[n] = (ovang[bw.Node1Index] - ovang[bw.Node2Index]) * PUCONV / bw.Obj.X;
                    n = n + 1;
                }
            }

            // Calc injections
            n=0;
            for( i=0;i<ccts.Length;i++) {
                c = ccts[i];
                bw = _branches.get(c);
                if (bw.Obj.X!=0) {
                    for(j=0;j<=nupb;j++) {
                        inj[n] = inj[n] + sensmat[n,j] * f[j];
                    }
                    tvec[bw.Node1Index] = tvec[bw.Node1Index] + inj[n];
                    tvec[bw.Node2Index] = tvec[bw.Node2Index] - inj[n];
                    n = n + 1;
                }
            }

            //
            _ufac.Solve(tvec, ref tvec);
            tvang = Utilities.CopyArray(ovang);

            for (i=0;i<ovang.Length;i++) {
                tvang[i] = tvang[i] + tvec[i];
            }
        }

        public static void RunTests() {
            if (!SparseMatrix.Test()) {
                throw new Exception("SparseMatrix internal test failed");
            }
            var mo = new MO();
            if ( !mo.Test() ) {
                throw new Exception("MO test failed");
            }
            var solveLin = new SolveLin();
            if ( !solveLin.Test() ) {
                throw new Exception("SolveLin internal test failed");
            }
            if ( !SolveLinSym.Test() ) {
                throw new Exception("SolveLinSym internal test failed");
            }
            var sparseInverse = new SparseInverse();
            if ( !sparseInverse.Test() ) {
                throw new Exception("SparseInverse internal test failed");
            }
            var lp = new LP();
            if ( !lp.Test1() ) {
                throw new Exception("LP internal test failed");
            }
            //?? After email with Lewis apprarently this test doesn't work
            //var lpModel = new LPModel();
            //if ( !lpModel.Test() ) {
            //    throw new Exception("LPModel internal test failed");
            //}
        }
    }

    public class CVang {
        
        private Dictionary<CtrlWrapper,double[]?> _dict;

        public CVang() {
            _dict = new Dictionary<CtrlWrapper, double[]?>();
        }

        public double[]? Base {get; set;}        

        public double[]? Get(CtrlWrapper ctrl) {
            return _dict[ctrl];
        }

        public void Set(CtrlWrapper ctrl, double[]? vang) {
            if ( !_dict.ContainsKey(ctrl) ) {
                _dict.Add(ctrl,vang);
            } else {
                _dict[ctrl] = vang;
            }
        }
        public double[]? Boundary {get; set;}
    }
}