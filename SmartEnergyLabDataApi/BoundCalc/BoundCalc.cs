using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class BoundCalc : IDisposable {

        // Data from the database - nodes, branches and controls
        private DataAccess _da;
        private Dataset _dataset;
        
        private Nodes _nodes;
        private double[] _btfr;
        private Branches _branches;
        private Ctrls _ctrls;        
        private Boundaries _boundaries;
        private DatasetData<Zone> _zones;
        private NodeOrder _nord;
        public SparseMatrix admat;
        public SolveLinSym _ufac;

        public double[] btfer;                  // Base node transfers
        public double[] itfer;                  // Boundary interconnection transfers
        public double[] mism;                   // Mismatch results
        public double[] km;                     // Marginal km result
        public double[] tlf;                    // Marginal tlf result
        public double[] flow;                   // flow results
        public double[] free;                   // free capacity
        public int[] ord;                       // order of free capacity
        public double[]?[] civang;              // voltage angles for base intact case, max controls and boundary interconnection

        public double[] bc;                     // boundary capacity determined by free capacity on circuit
        public int[] mord;                      // order of bc

        public BoundaryWrapper? ActiveBound;
        public Trip? ActiveTrip;
        public double[]?[] tcvang;               // trip control voltage angles
        public Trip WorstTrip;
        public double WTCapacity;

        public int setptmode;
        //
        public const int SPZero = 0;          // controls at zero cost points
        public const int SPMan = 1;           // given by ISetPt input data
        public const int SPAuto = 2;          // determined by optimiser values
        public const int SPBLANK = -1;        // do not display

        public const double PUCONV = 10000;   // Conversion for % on 100MVA to pu on 1MVA
        public const double SCAP = 1;         // Ignore branches with cap < 1 MW
        public const double OVRLD = -0.05;
        public const int MAXCPI = 20;         // Maximum cct constraints added per iteration
        public const int LRGCAP = 50000;

        // Used to store results        
        private BoundCalcStageResults _stageResults;
        private Optimiser opt;

        public TopTrips _acctOut;
        public TopTrips _scctOut;
        public TopTrips _dcctOut;

        private ProgressManager _progressManager = new ProgressManager();

        public ProgressManager ProgressManager {
            get {
                return _progressManager;
            }
        }

        public BoundCalc(int datasetId, bool buildOptimiser=false) {
            _da = new DataAccess();
            _dataset = _da.Datasets.GetDataset(datasetId);
            if ( _dataset == null) {
                throw new Exception($"Cannot find dataset with id=[{datasetId}]");
            }
            _stageResults = new BoundCalcStageResults();
            // create nodes wrapper
            var locDi = _da.NationalGrid.GetLocationDatasetData(_dataset.Id);
            _nodes = new Nodes(_da,_dataset.Id,locDi);
            // create branches wrapper
            _branches = new Branches(_da,_dataset.Id,_nodes, buildOptimiser);
            // create ctrl wrapper
            _ctrls = new Ctrls(_da,_dataset.Id,this);
            // zones
            _zones = loadZones(_da,datasetId,_nodes);
            // boundaries
            Trip.BoundCalc = this;
            _boundaries = new Boundaries(_da,_dataset.Id,this);
            //
            _acctOut = new TopTrips(this,20);
            _scctOut = new TopTrips(this,30);
            _dcctOut = new TopTrips(this,30);
            //
            if ( buildOptimiser ) {
                if (NetCheck()) {
                    opt = Optimiser.BuildOptimiser(this);
                } else {
                    throw new Exception("Unspecified problem checking network");
                }
            }

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

        public BoundCalcStageResults StageResults {
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

        public Boundaries Boundaries {
            get {
                return _boundaries;
            }
        }

        public DatasetData<Zone> Zones {
            get {
                return _zones;
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

        public List<BoundCalcAllTripsResult> IntactTrips {
            get {
                return _acctOut.Results;
            }
        }

        public List<BoundCalcAllTripsResult> SingleTrips {
            get {
                return _scctOut.Results;
            }
        }

        public List<BoundCalcAllTripsResult> DoubleTrips {
            get {
                return _dcctOut.Results;
            }
        }
        public (int,Node?) Subnets() {
            int i, j1, j2;
            bool chng;

            var snet = new int[_nodes.Count];
            for(i=0;i<snet.Length;i++) {
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

            for( i=0; i<snet.Length; i++) {
                if ( snet[i] != 0) {
                    return (i,_nodes.DatasetData.Data[i]);                    
                }
            }
            return (0,null);
        }

        // Create intact network admittance matrix
        // Sets up node ordering object
        private SparseMatrix AdmittanceMat( bool lf=true) {
            int i, nupb, nr, nc;
            int n1, n2;
            double y;

            _nord = new NodeOrder(_nodes,_branches);

            if ( lf ) {
                nupb = _nord.nn-1; // exclude reference node
            } else {
                nupb = _nord.nn;
            }

            foreach( var nd in _nodes.Objs) {
                nd.Pn = _nord.NodePos(nd.Index); // cache node positions in admittance matrix
            }

            var adm = new SparseMatrix(nupb, nupb, Nord.fz / (nupb + 1));

            foreach( var br in _branches.Objs) {
                br.pn1 = br.Node1.Pn;            // cache node 1 and node2 positons in admittance matrix
                br.pn2 = br.Node2.Pn;
                if ( br.Obj.X !=0 ) {
                    y = PUCONV / br.Obj.X;
                    if ( br.pn1 > br.pn2 ) {     // ensure upper diagonal
                        nr = br.pn2;
                        nc = br.pn1;
                    } else {
                        nr = br.pn1;
                        nc = br.pn2;
                    }
                    adm.Addin(nr,nr, y);
                    if ( nc <= nupb ) {          // ensure not reference
                        adm.Addin(nc,nc,y);
                        adm.Addin(nr,nc,-y);
                    }
                }
            }

            return adm;
        } 

        // Setup transfer vector
        private void BaseTransfers(out double[] tvec) {
            int i;
            tvec = new double[_nodes.Count];

            foreach( var node in _nodes.Objs) {
                tvec[node.Pn] = node.Obj.Generation - node.Obj.Demand;
            }

        }

        // Calcuate base transfer plus interconnection
        // isf scales interconnection allowance used in itfr
        public void CalcTransfers(double isf, out double[] tfr) {
            int i;
            tfr = Utilities.CopyArray(btfer);

            if ( isf!=0 ) {
                for( i=0; i<tfr.Length;i++) {
                    tfr[i] = tfr[i] + isf * itfer[i];
                }
            }
        }

        // Calculate all flows and mismatches from vangs and setpoints
        // call with mism = transfers
        // outages=falase ensures intact network calculation irrespective of ActiveTrip
        public void CalcFlows(double[] vang, int setptmd, bool outages, out double[] lflow, double[] mism)
        {
            double f;
            lflow = new double[_branches.Count];

            foreach( var br in _branches.Objs ) {
                f = br.flow(vang, setptmd, outages);
                lflow[br.Index -1] = f;
                mism[br.pn1]-= f;
                mism[br.pn2]+= f;
                //
                br.PowerFlow=f;
            }
        }

        private int CalcIntactVang() {
            double[] tvec, vang=null;

            civang = new double[_ctrls.Count+2][]; // index 0 is base vang, 1 .. n are controls and n+1 is boundary transfer

            foreach( var ct in _ctrls.Objs) {
                tvec = new double[_nodes.Count];
                var br = ct.Branch;
                if ( br.pn1 <= _nord.nn  || br.pn2 <= _nord.nn) {
                    tvec[br.pn1] = -ct.InjMax;
                    tvec[br.pn2] =  ct.InjMax;
                    _ufac.Solve(tvec, ref vang);
                    civang[ct.Index] = vang;
                } else {
                    civang[ct.Index] = null;
                }
            }

            BaseTransfers(out btfer);
            mism = Utilities.CopyArray(btfer);
            _ufac.Solve(btfer, ref vang);
            civang[0] = vang;
            CalcFlows( vang, SPZero, false, out flow, mism);
            return _nord.NodeId(_nord.nn); // Index of refnode
        }        

        public bool NetCheck() {

            double tgen=0, tdem=0, timp=0;

            BoundCalcStageResult sr=null;
            try {
                sr = _stageResults.NewStage("#nodes");
                _stageResults.StageResult( sr, BoundCalcStageResultEnum.Pass, $"{_nodes.Count}");

                sr = _stageResults.NewStage("#zones");
                _stageResults.StageResult( sr, BoundCalcStageResultEnum.Pass, $"{_zones.Data.Count}");

                sr = _stageResults.NewStage("#branches");
                _stageResults.StageResult( sr, BoundCalcStageResultEnum.Pass, $"{_branches.Count}");

                sr = _stageResults.NewStage("#controls");
                _stageResults.StageResult( sr, BoundCalcStageResultEnum.Pass, $"{_ctrls.Count}");

                sr = _stageResults.NewStage("#boundaries");
                _stageResults.StageResult( sr, BoundCalcStageResultEnum.Pass, $"{_boundaries.Count}");
                // No nodes or branches then return
                if ( _nodes.Count==0 || _branches.Count==0 )                 {
                    return false;
                }

            } catch ( Exception e) {
                if ( sr!=null ) {
                    _stageResults.StageResult(sr,BoundCalcStageResultEnum.Fail,e.Message);
                }
                throw;
            }

            foreach ( var zn in _zones.Data) {
                tgen+=zn.TGeneration;
                tdem+=zn.Tdemand;
                timp+=zn.UnscaleGen - zn.UnscaleDem;
            }

            MiscReport("Generation", tgen.ToString("f1"));
            MiscReport("Demand", tdem.ToString("f1"));
            MiscReport("Imports", timp.ToString("f1"));

            (int res, Node? node) = Subnets();
            if ( res!=0) {
                throw new Exception($"Disconnected network detected at node {node?.Name}");
            }

            //
            var admat = AdmittanceMat(true);

            MiscReport("HVDC nodes", $"{_nodes.Count - _nord.nn -1}" );
            MiscReport("NZ per row", $"{(double) _nord.nz / _nord.nn:F4}");
            MiscReport("FZ per row", $"{(double) _nord.fz / _nord.nn:F4}");

            _ufac = new SolveLinSym(admat, false);
            var refIndex = CalcIntactVang();

            var nd = _nodes.get(refIndex);

            MiscReport("RefNode",nd.Obj.Code);
            MiscReport("No Ctrl Mismatch", $"{mism[nd.Pn]:F0}");

            ActiveBound = null;
            ActiveTrip = null;
    
            return true;
        }

        // Calculate total vang using control setpoints and interconnection scaling factor iasf
        // iasf scales the interconnection allowance used to compute ctrlva(upb)
        public void CalcVang(double isaf, double[]?[] ctrlva, out double[] vang) {
            int i, j, n;
            double sf2, sp;
            double[] tv;

            n = _ctrls.Count + 1;            
            vang = Utilities.CopyArray(ctrlva[0]);    // base vangs

            if ( isaf!=0 && ctrlva[n]!=null) {
                tv = ctrlva[n];
                for( i=0; i<vang.Length; i++) {
                    vang[i] = vang[i] + tv[i] * isaf;
                }
            }

            foreach( var ct in _ctrls.Objs) {
                sp = ct.GetSetPoint(setptmode);
                if ( sp!=0 && ctrlva[ct.Index]!=null ) {
                    tv = ctrlva[ct.Index];
                    sf2 = sp / ct.Obj.MaxCtrl;
                    for ( j=0; j < vang.Length;j++) {
                        vang[j] = vang[j] + tv[j] * sf2;
                    }
                }
            }            
        }

        // Return node position of largest mismatch

        public int MaxMismatch(double[] mism) {
            int i, maxi=0;
            double maxm=0, amism;

            for( i=0;i<mism.Length;i++) {
                amism = Math.Abs(mism[i]);
                if ( amism > maxm) {
                    maxm = amism;
                    maxi=i;
                }
            }
            return maxi;
        }

        // Calculate cct free capacity in direction of flow

        public void CalcMinFree(double[] flow) {
            int i, n;

            n = _branches.Count - 1;
            free = new double[n+1];
            ord = new int[n+1];

            foreach( var br in _branches.Objs) {
                i = br.Index - 1;
                if ( br.Obj.Cap > SCAP) {
                    free[i] = br.Obj.Cap - Math.Abs(flow[i]);
                } else {
                    free[i] = 99999;
                }
                ord[i] = i;
                // Store in branch wrapper
                br.FreePower = free[i];
            }
            LPhdr.MergeSortFlt(free,ord, n+1);

            //

        }

        // Calculate cct free capcity in flow direction given by vang

        public void CalcDirFree(double[] flow, double[] vang) {
            int i,n;

            n = _branches.Count - 1;
            free = new double[n+1];
            ord = new int[n+1];

            foreach( var br in _branches.Objs) {
                i = br.Index - 1;
                if ( br.Obj.Cap > SCAP) {
                    free[i] = br.Obj.Cap - flow[i] * br.Dirn(vang);
                } else {
                    free[i] = 99999;
                }
                ord[i] = i;
                //
                br.FreePower = free[i];
            }
            LPhdr.MergeSortFlt(free,ord, n+1);
        }

        // Calculate the boundary scaling factor at which given flows will reach capapcity
        // Calculate boundcap implied by each circuit assuming flows correspond to specified transfer

        public double CalcSF(double[] mflow, double[] ivang, double tfer, double ia) {
            int i, n;
            double mfree, iflow;

            n = _branches.Count - 1;
            mord = new int[n+1];
            bc = new double[n+1];

            foreach( var br in _branches.Objs) {
                i = br.Index - 1;
                iflow = br.flow(ivang, SPZero, true); // the flow resulting from interconnection
                if ( br.Obj.Cap > SCAP) {
                    mfree = br.Obj.Cap - mflow[i] * Math.Sign(iflow);
                } else {
                    mfree = 99999;                    
                }
                if ( Math.Abs(iflow) < LPhdr.lpEpsilon) {
                    bc[i] = 99999;
                } else {
                    bc[i] = tfer + ia * mfree / Math.Abs(iflow);
                }
                mord[i] = i;
            }

            LPhdr.MergeSortFlt(bc,mord, n+1);
            i = mord[0];
            var brr = _branches.get(i+1);
            iflow = brr.flow(ivang, SPZero, true);

            if ( brr.Obj.Cap > SCAP ) {
                mfree = brr.Obj.Cap - mflow[i] * Math.Sign(iflow);
            } else {
                mfree = 99999;
            }

            if ( Math.Abs(iflow) < LPhdr.lpEpsilon ) {
                throw new Exception("Unable to calculate boundary capacity as interconnection flow too small");
            } else {
                return mfree / Math.Abs(iflow);
            }
        }

        // Calculate the loadflow with interconnection and control setpoints
        // setpoints depend on setptmode global
        // returns node index with largest mismatch
        public void CalcLoadFlow(double[]?[] cvang, double isaf, out double[] lflow, bool save = true) {
            double[] tfr, vang;

            CalcTransfers(isaf, out tfr);
            mism = Utilities.CopyArray(tfr);
            CalcVang(isaf, cvang, out vang);
            CalcFlows( vang, setptmode, true, out lflow, mism);
            //
            // Store mismatches in node wrappers
            foreach( var nd in _nodes.Objs) {
                nd.Mismatch = mism[nd.Pn];
            }
        }

        public void SaveLFResults( double[] lflow) {

            CalcMinFree(lflow);
            //
            var mi = MaxMismatch(mism);
            var mm = mism[mi];
            var nd = _nodes.get(_nord.NodeId(mi));
            MiscReport($"Max mismatch [{nd.Obj.Code}]", $"{mm:g5}");
        }

        public void CalcBoundLF(double[]?[] cvang, out double[] lflow, bool save = false) {
            double[] mflow;
            double[] ivang;
            double iasf;
            bool pt;
            double pfer;

            pt = ActiveBound == null;
            CalcLoadFlow(cvang,0,out mflow, false);
            if ( !pt ) {
                if ( ActiveTrip==null) {
                    ivang = civang[_ctrls.Count+1]; // get ivang from intact volt angles
                } else {
                    ivang = tcvang[_ctrls.Count+1]; // get ivang from trip volt angles
                }
                iasf = CalcSF(mflow, ivang, Math.Abs(ActiveBound.PlannedTransfer), Math.Abs(ActiveBound.InterconAllowance)); // Small scalling factor

                CalcLoadFlow(cvang, iasf, out lflow, save);
                MiscReport("Boundary capacity", $"{bc[mord[0]]:0.00}");
            } else {
                lflow = mflow;
                if (save) {
                    SaveLFResults(mflow);
                }
            }
        }

        public void ClearOutages() {
            foreach( var br in _branches.Objs) {
                br.BOut = false;
            }
        }

        // Balance HV nodes

        public int BalanceHVDC() {
            int r1, r2=0;
            string title = "Balance HVDC";
            opt.ResetLP();
            r1 = opt.ctrllp.SolveLP(ref r2);
            if ( r1 == LPhdr.lpOptimum) {
                MiscReport(title,$"Ctrl cost: {opt.ControlCost():0.00}");
            } else {
                if ( r1 == LPhdr.lpInfeasible ) {
                    MiscReport(title,$"Unresolvable constraint {opt.ctrllp.GetCname(r2)}");
                } else {
                    MiscReport(title,"Unknown optimiser fail");
                }
            }
            return r1;
        }

        // Optimise the loadflow
        // if activebound is nothing then optimise how planned transfer condition
        // else add boundary circuits pluc overloads and optimise with sensitivitieses to the boundary transfer variable

        public int OptimiseLoadflow( double[]?[] cva, out double[] lflow, bool save = true) {
            int i, iter=0, xa, r1,r2=0;
            string title;
            double[] ivang=null;
            bool pt;
            double iasf=0;

            setptmode = SPAuto;
            pt = ActiveBound == null;
            r1 = BalanceHVDC();
            CalcLoadFlow(cva, 0, out lflow, r1!= LPhdr.lpOptimum); // start at planned transfer

            if ( r1 != LPhdr.lpOptimum) {
                return r1;
            }

            xa = cva.Length - 1;
            if (!pt) { // add boundary circuits with free calculated in direction of interconnection
                ivang = cva[xa];
                CalcDirFree(lflow, ivang);
                foreach( var br in ActiveBound.BoundCcts.Items) {
                    opt.PopulateConstraint(br, free[br.Index - 1], br.Dirn(ivang), iasf, cva, false);
                }

                title = "Capacity of boundary circuits";
                r1 = opt.ctrllp.SolveLP(ref r2);
                
                if ( r1 == LPhdr.lpOptimum) {
                    iasf = opt.boun.Value(opt.ctrllp) / Math.Abs( ActiveBound.InterconAllowance);
                    MiscReport(title, $"{opt.BoundCap():0.00}");                    
                } else {
                    if ( r1 == LPhdr.lpInfeasible ) {
                        MiscReport(title, $"Unresolvable constraint ${opt.ctrllp.GetCname(r2)}");
                    } else {
                        MiscReport(title, "Unknown optimiser fail");
                    }
                }

                CalcLoadFlow(cva, iasf, out lflow, r1!=LPhdr.lpOptimum);

                if ( r1!= LPhdr.lpOptimum ) {
                    return r1;
                }
            }

            do {
                iter++;

                if ( pt ) {
                    CalcMinFree( lflow);
                } else {
                    CalcDirFree(lflow, ivang); // free in direction of interconnection transfer
                }

                i = 0;
                while ( (free[ord[i]] < OVRLD) && (i<=MAXCPI )) {
                    var br = _branches.get(ord[i] + 1);
                    if ( pt ) {
                        opt.PopulateConstraint(br, free[ord[i]], lflow[ord[i]], 0, cva, pt);
                    } else {
                        opt.PopulateConstraint(br, free[ord[i]], br.Dirn(ivang), iasf, cva, pt);
                    }
                    i++;
                }

                if ( i==0 ) {
                    break;
                }

                MiscReport($"Iter {iter}", $"Ovrlds: {i-1}");

                r1 = opt.ctrllp.SolveLP(ref r2);

                if ( !pt) {
                    iasf = opt.boun.Value(opt.ctrllp) / Math.Abs(ActiveBound.InterconAllowance);
                }

                CalcLoadFlow(cva, iasf, out lflow, r1!=LPhdr.lpOptimum);

                if ( r1!= LPhdr.lpOptimum ) {
                    if ( r1 == LPhdr.lpInfeasible ) {
                        MiscReport("Optimiser",$"Unresolvable constraint {opt.ctrllp.GetCname(r2)}");
                    } else {
                        MiscReport("Optimiser", "Unknown optimiser fail");
                    }
                    return r1;
                }

            } while(true);

            if ( !pt ) {
                MiscReport("Boundary capacity (all circuits)", $"{opt.BoundCap():0.00}");
                CalcSF(lflow,ivang,Math.Abs(opt.BoundCap()),Math.Abs(ActiveBound.InterconAllowance));
            }

            if ( save ) {
                SaveLFResults(lflow);
                MiscReport("Boundary optimisation",$"Ctrl cost: {opt.ControlCost():0.00}");
            }
            //

            return r1;
        }

        // Calculate nodal loss and mwkm sensitivities
        // Based n base flows and bflow()

        public void CalcNodeMarginals(double[] bflow) {
            double[] tv, va=null, sflow;
            int i, j;

            tlf = new double[_nodes.Count];
            km = new double[_nodes.Count];

            for( i=0; i<_nord.nn; i++) { // for each node excluding refnode and hvdc nodes
                tv = new double[_nodes.Count];
                tv[i] = 1;
                _ufac.Solve(tv, ref va);
                CalcFlows(va, SPZero, true, out sflow, tv);

                foreach( var br in _branches.Objs) {
                    j = br.Index - 1;
                    tlf[i] = tlf[i] + 2 * bflow[j] * sflow[j] * br.Obj.R / PUCONV;
                    if ( bflow[j] * sflow[j] < 0 && br.km!=null) {
                        km[i] = km[i] - Math.Abs(sflow[j] * (double) br.km);
                    } else if ( br.km!=null ) {
                        km[i] = km[i] + Math.Abs(sflow[j] * (double) br.km);
                    }
                }
            }

        }

        // Set active boundary always clearing any active trip

        public void SetActiveBoundary(BoundaryWrapper bnd) {
            double[] iflow, ivang=null;
            int mi;
            double mm;

            if ( !(ActiveBound == bnd)) {
                ActiveBound = bnd;

                if (ActiveTrip!=null) {
                    ActiveTrip.Deactivate();
                    ActiveTrip = null;
                }

                if ( bnd!=null ) {
                    // calc interconnection vang for new boundary
                    bnd.InterconnectionTransfers(out itfer,_nodes);
                    mism = Utilities.CopyArray(itfer);                    
                    _ufac.Solve(itfer, ref ivang);
                    civang[_ctrls.Count+1] = ivang;
                    CalcFlows(ivang,SPZero, false, out iflow, mism);
                    mi = MaxMismatch(mism);
                    mm = mism[mi];
                    var nd = _nodes.get(_nord.NodeId(mi));
                    MiscReport($"{bnd.name} setup mismatch {nd.Obj.Code}",$"{mm:g5}");
                    MiscReport("Planned Transfer", $"{bnd.PlannedTransfer:0.00}");
                    MiscReport("Interconnection Allowance",$"{bnd.InterconAllowance:0.00}");                
                } else {
                    civang[_ctrls.Count+1] = null;
                    MiscReport("Boundary unspecified","");
                }
            } else {
                // nothing to do
            }
        }

        // Set a trip to be active
        // Sets trip voltage angles tcvang if successful

        public bool SetActiveTrip(Trip tr) {

            if ( ActiveTrip == tr) {        // nothing to do - note trip no reactiviated
                return true;
            }

            if ( ActiveTrip!=null ) {       // deactivate current trip
                ActiveTrip.Deactivate();
                ActiveTrip = null;
            }

            if ( tr == null ) {             // nothing to do
                return true;
            }

            if ( tr.TripVectors(civang, out tcvang)) {
                MiscReport($"Setup Trip {tr.name}", tr.TripDescription());
                ActiveTrip = tr;
                return true;
            } else {
                MiscReport($"Trip {tr.name}","splits AC network");
                return false;
            }
        }

        // Run calculater
        // If bound is nothing then run planned transfer case
        // If bound is not already active then setup itfer and civang
        // Optionally save boundary max transfer or planned transfer loadflows
        // Optionally undertake nodemarginals calc for planned transfer only
        public int RunBoundCalc(BoundaryWrapper bound, Trip tr, int setptmd, bool nodemarginals, bool save = false) {
            double[] vang;
            double[]?[] cvang;
            int mi,res;
            double mm;

            SetActiveBoundary(bound);
            if ( !SetActiveTrip(tr) ) {
                return LPhdr.lpZeroPivot;
            }

            if ( ActiveTrip == null ) {
                cvang = civang;
            } else {
                cvang = tcvang;
            }

            if ( setptmd == SPAuto) {
                setptmd = SPAuto;                
                res = OptimiseLoadflow(cvang, out flow, save);
                if ( res!= LPhdr.lpOptimum) {
                    return res;
                }
                if ( ActiveBound!=null) {
                    if ( Math.Abs(opt.BoundCap()) < WTCapacity) {
                        WorstTrip = tr;
                        WTCapacity = Math.Abs(opt.BoundCap());
                    }
                }

            } else {
                setptmode = SPMan;
                CalcBoundLF( cvang, out flow, save);
                if ( ActiveBound!=null ) {
                    if ( bc[mord[0]] < WTCapacity ) {
                        WorstTrip = tr;
                        WTCapacity = bc[mord[0]];
                    }
                }
                if ( save ) {
                    //??
                }
            }

            if ( bound == null && nodemarginals ) {
                CalcNodeMarginals(flow);
                //??
            } else {
                //??
            }

            return LPhdr.lpOptimum;
        }

        // Run trip case and fill in relevent TopN tables
        public void RunTrip(BoundaryWrapper bn, Trip tr, int setptmd, bool save=false)
        {
            List<string> limccts = new List<string>();;
            int res;
            if ( tr == null ) { // intact network case
                _acctOut.SetBoundary(bn, bn.PlannedTransfer + bn.InterconAllowance); // use full interconnection for intact case
                res = RunBoundCalc(bn, null, setptmd, false, save);
                if ( res!=LPhdr.lpOptimum) {
                    //??
                } else {
                    if ( setptmd == SPAuto ) {
                        limccts = opt.Limitccts();
                    }
                    _acctOut.Insert(null, bc, mord, limccts);
                }
            } else {
                if ( tr.name.Substring(0,1) == "S" ) { // single circuit trip
                    _scctOut.SetBoundary(bn, bn.PlannedTransfer + bn.InterconAllowance); // use full interconnection for since cct trips
                    res = RunBoundCalc(bn, tr, setptmd, false, save);
                    if ( res!=LPhdr.lpOptimum) {
                        //??
                    } else {
                        if ( setptmd == SPAuto) {
                            limccts = opt.Limitccts();
                        }
                        _scctOut.Insert( tr, bc, mord, limccts); 
                    }
                } else {
                    _dcctOut.SetBoundary(bn, bn.PlannedTransfer + 0.5*bn.InterconAllowance); // use half interconnection for double cct trips 
                    res = RunBoundCalc(bn, tr, setptmd, false, save);
                    if ( res!=LPhdr.lpOptimum) {
                        //??
                    } else {
                        if ( setptmd == SPAuto ) {
                            limccts = opt.Limitccts();
                        }
                        _dcctOut.Insert(tr, bc, mord, limccts);
                    }
                }
            }
        }

        // Run trip collection
        // Boundary must be specified
        public void RunTripSet(BoundaryWrapper bn, Collection<Trip> trips, int setptmd) {
            foreach( var tr in trips.Items) {
                RunTrip(bn, tr, setptmd, false);
                _progressManager.Update(tr.name);
            }
            //?? output results
        }

        public void RunAllTrips(BoundaryWrapper bn, int setptmd) {
            WTCapacity = 99999;
            RunTrip(bn, null, setptmd, true);
            RunTripSet(bn, bn.STripList, setptmd);
            RunTripSet(bn, bn.DTripList, setptmd);
            MiscReport("Worst Case Trip",$"{WTCapacity:0.00}");
            RunBoundCalc(bn, WorstTrip, setptmd, false, true);
        }


        public void MiscReport(string msg, object result) {
            var sr = _stageResults.NewStage(msg);
            _stageResults.StageResult(sr,BoundCalcStageResultEnum.Pass,$"{result}");
        }

        public NodeBoundaryData GetNodeBoundaryData(Boundary bndry) {
            NodeBoundaryData nbd=null;
            nbd = new NodeBoundaryData(bndry.Zones);
            return nbd;
        }

        private DatasetData<Zone> loadZones(DataAccess da, int datasetId, Nodes nodes) {
            var q = da.Session.QueryOver<Zone>();
            var ds = new DatasetData<Zone>(da, datasetId,m=>m.Id.ToString(),q);
            //
            foreach( var nd in nodes.DatasetData.Data) {
                if ( nd.Ext) {
                    nd.Zone.UnscaleDem+=nd.Demand;
                    nd.Zone.UnscaleGen+=nd.Generation;
                } else {
                    nd.Zone.Tdemand+=nd.Demand;
                    nd.Zone.TGeneration+=nd.Generation;
                }
            }
            //
            return ds;
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

}