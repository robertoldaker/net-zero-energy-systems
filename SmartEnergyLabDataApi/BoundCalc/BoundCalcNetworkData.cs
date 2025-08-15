using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using Lad.NetworkLibrary;
using Microsoft.AspNetCore.SignalR;
using Lad.NetworkOptimiser;
using Lad.LinearProgram;
using GeneratorType = SmartEnergyLabDataApi.Data.BoundCalc.GeneratorType;
using System.Text.RegularExpressions;
using NLog.LayoutRenderers;
using Microsoft.JSInterop.Infrastructure;


namespace SmartEnergyLabDataApi.BoundCalc;

public enum SetPointModeNew { Auto, Manual, BalanceHVDCNodes }

public class BoundCalcNetworkData {

    private Dictionary<Node, Network.Node> _nodeDict = new Dictionary<Node, Network.Node>();
    private Dictionary<Network.Node, Node> _reverseNodeDict = new Dictionary<Network.Node, Node>();
    private Dictionary<Branch, Network.BranchSpec> _branchDict = new Dictionary<Branch, Network.BranchSpec>();
    private Dictionary<Network.BranchSpec, Branch> _reverseBranchDict = new Dictionary<Network.BranchSpec, Branch>();
    private Dictionary<Ctrl, Network.Control> _ctrlDict = new Dictionary<Ctrl, Network.Control>();
    private Dictionary<Network.Control, Ctrl> _reverseCtrlDict = new Dictionary<Network.Control, Ctrl>();
    private Reporter _reporter = new Reporter();
    public BoundCalcNetworkData(int datasetId, int transportModelId = 0)
    {
        // Ensure we have at least one transport model
        BoundCalcNetworkData.AddDefaultTransportModels(datasetId);

        using (var da = new DataAccess(false)) {
            Dataset = da.Datasets.GetDataset(datasetId);
            if (Dataset == null) {
                throw new Exception($"Cannot find dataset with id=[{datasetId}]");
            }
            //
            StageResults = _reporter.StageResults;

            // Transport models
            (TransportModels, TransportModelEntries) = da.BoundCalc.GetTransportModelDatasetData(datasetId, null, true);
            // work out the transport model
            if (transportModelId > 0) {
                TransportModel = TransportModels.Data.Where(m => m.Id == transportModelId).FirstOrDefault();
                if (TransportModel == null) {
                    throw new Exception($"Cannot find transport model with id=[{transportModelId}]");
                }
            } else {
                // set the first one available
                TransportModel = TransportModels.Data.Count() > 0 ? TransportModels.Data[0] : null;
            }
            // Locations
            Locations = da.NationalGrid.GetLocationDatasetData(datasetId);
            var ngDi = da.BoundCalc.GetNodeGeneratorDatasetData(datasetId);
            // Nodes
            Nodes = da.BoundCalc.GetNodeDatasetData(datasetId, null, true);
            // Branches and controls
            (Branches, Ctrls) = da.BoundCalc.GetBranchDatasetData(datasetId, null, true);
            // This sets br.km
            foreach (var br in Branches.Data) {
                br.SetKm();
            }
            // Boundaries
            Boundaries = da.BoundCalc.GetBoundaryDatasetData(datasetId);
            // Zones
            Zones = da.BoundCalc.GetZoneDatasetData(datasetId, null);
            // Generators
            Generators = da.BoundCalc.GetGeneratorDatasetData(datasetId);
            // Update scalings for all transport models
            foreach (var tm in this.TransportModels.Data) {
                tm.UpdateScaling(Nodes.Data, ngDi.Data, this.Generators.Data);
            }
            // Update generator values using the specified transport model
            if (TransportModel != null) {
                TransportModel.UpdateGenerators(Generators.Data);
            }
            // Boundary branches
            BoundaryDict = new Dictionary<string, int[]>();
            foreach (var b in Boundaries.Data) {
                var branchIds = getBranchIds(b, Branches);
                BoundaryDict.Add(b.Code, branchIds);
            }
        }

        //
        Model = createNetworkModel();
    }

    public Dataset Dataset { get; private set; }
    public BoundCalcStageResults StageResults { get; private set; }
    public DatasetData<Node> Nodes { get; private set; }
    public DatasetData<Branch> Branches { get; private set; }
    public DatasetData<Ctrl> Ctrls { get; private set; }
    public DatasetData<Boundary> Boundaries { get; private set; }
    public DatasetData<Zone> Zones { get; private set; }
    public DatasetData<GridSubstationLocation> Locations { get; private set; }
    public DatasetData<Generator> Generators { get; private set; }
    public DatasetData<TransportModel> TransportModels { get; private set; }
    public TransportModel? TransportModel { get; private set; }
    public DatasetData<TransportModelEntry> TransportModelEntries { get; private set; }
    public Dictionary<string, int[]> BoundaryDict { get; private set; }
    public Network? Model { get; private set; }

    private ProgressManager _progressManager = new ProgressManager();
    public ProgressManager ProgressManager
    {
        get {
            return _progressManager;
        }
    }

    private int[] getBranchIds(Boundary b, DatasetData<Branch> branchDi)
    {
        var branchIds = new List<int>();
        foreach (var br in branchDi.Data) {
            var z1 = br.Node1.Zone;
            var z2 = br.Node2.Zone;
            var in1 = b.Zones.FirstOrDefault(m => m.Id == z1.Id) != null;
            var in2 = b.Zones.FirstOrDefault(m => m.Id == z2.Id) != null;
            if (in1 ^ in2) {
                branchIds.Add(br.Id);
            }
        }
        return branchIds.ToArray();
    }

    private DatasetData<GridSubstationLocation> loadLocations(DataAccess da, int datasetId)
    {
        var q = da.Session.QueryOver<GridSubstationLocation>();
        var locs = new DatasetData<GridSubstationLocation>(da, datasetId, m => m.Id.ToString(), q);
        return locs;
    }

    private (DatasetData<TransportModel> tmDi, DatasetData<TransportModelEntry>? tmeDi) loadTransportModels(DataAccess da, int datasetId)
    {
        return da.BoundCalc.GetTransportModelDatasetData(datasetId, null, true);
    }

    private void setTransportModelScalings(DataAccess da, int datasetId)
    {
        var nodeGenDi = da.BoundCalc.GetNodeGeneratorDatasetData(datasetId, null);
    }

    private Network? createNetworkModel()
    {
        // Clear dictionaries
        _nodeDict.Clear();
        _reverseNodeDict.Clear();
        _branchDict.Clear();
        _reverseBranchDict.Clear();
        _ctrlDict.Clear();
        _reverseCtrlDict.Clear();
        // set as static field in Network to allow diagnostic messages to be generated "internally" and get recorded as a StageResult
        // and hence shown in the gui
        // Network.Nodes
        List<Network.Node> networkNodes = [];
        foreach (var n in Nodes.Data) {
            Network.Node netNode = new(n.Code, n.ZoneName, 1, n.Demand, n.Generation, n.ScInFeed(), n.FaultLimit(), n.Ext);
            _nodeDict.Add(n, netNode);
            _reverseNodeDict.Add(netNode, n);
            networkNodes.Add(netNode);
        }
        // Network.Branches
        List<Network.Branch> networkBranches = [];
        foreach (var b in Branches.Data) {
            Network.BranchSpec bsSpec = new(b.LineName, b.X, b.Cap, b.R, 0, b.CableLength + b.OHL);
            var node1 = _nodeDict[b.Node1];
            var node2 = _nodeDict[b.Node2];
            Network.Branch bs = new(node1, node2, bsSpec);
            _branchDict.Add(b, bs);
            _reverseBranchDict.Add(bs, b);
            networkBranches.Add(bs);
        }
        // Boundaries
        List<Network.BoundSpec> networkBoundaries = [];
        foreach (var bo in Boundaries.Data) {
            var zoneNames = bo.Zones.Select(m => m.Code).ToArray();
            networkBoundaries.Add(new(bo.Code, zoneNames));
        }
        // Controls
        List<Network.ControlSpec> networkControlSpecs = [];
        foreach (var co in Ctrls.Data) {
            Network.ControlSpec cs = new(co.LineName, co.Type.ToString(), co.Cost, co.MinCtrl, co.MaxCtrl);
            networkControlSpecs.Add(cs);
        }

        // Ensure we have some nodes
        if (networkNodes.Count > 0) {
            Network fullnet = new(networkNodes, networkBranches, networkControlSpecs, networkBoundaries);
            //
            int i = 0;
            // add the actual controls created to a dictionary - relies on fact that they are in the Controls list
            // in the same order as appears in networkControlSpecs list
            foreach (var control in fullnet.Controls) {
                var ctrl = Ctrls.Data[i];
                _ctrlDict.Add(ctrl, control);
                _reverseCtrlDict.Add(control, ctrl);
                i++;
            }
            //

            fullnet.Reporter = _reporter;
            //
            Report("Nodes", fullnet.Nodes.Count);
            Report("Branches", fullnet.Branches.Count);
            Report("Controls", fullnet.Controls.Count);
            Report("Boundaries", fullnet.BoundaryDict.Count);
            Report("HVDC nodes", fullnet.Nord.NodeCount - fullnet.Nord.ACNodeCount);
            Report("INZC", fullnet.Nord.INZC);
            Report("FNZC", fullnet.Nord.FNZC);
            Report("Total Demand (MW)", fullnet.SystemDemand.ToString("F0"));
            Report("Total Generation (MW)", fullnet.SystemGeneration.ToString("F0"));
            Report("Ref. node", fullnet.Nord.LFReference().Name);

            return fullnet;
        } else {
            return null;
        }

    }


    public static string AddDefaultTransportModels(int datasetId)
    {
        string msg = "";
        using (var da = new DataAccess()) {
            (var tmDi, var tmeDi) = da.BoundCalc.GetTransportModelDatasetData(datasetId, null, false);
            var dataset = da.Datasets.GetDataset(datasetId);
            // If we haven;t got any transport models then add 2 default ones
            if (tmDi.Data.Count == 0 && tmDi.DeletedData.Count == 0) {
                msg += addTransportModel(da, dataset, "Peak Security", new Dictionary<GeneratorType, double>() {
                    { GeneratorType.Interconnector, 0 },
                    { GeneratorType.Tidal, 0 },
                    { GeneratorType.Wave, 0 },
                    { GeneratorType.WindOffshore, 0 },
                    { GeneratorType.WindOnshore, 0 },
                });
                msg += addTransportModel(da, dataset, "Economy Test", new Dictionary<GeneratorType, double>() {
                    { GeneratorType.Interconnector, 1 },
                    { GeneratorType.Nuclear, 0.85 },
                    { GeneratorType.OCGT, 0 },
                    { GeneratorType.PumpStorage, 0.5 },
                    { GeneratorType.Tidal, 0.7 },
                    { GeneratorType.Wave, 0.7 },
                    { GeneratorType.WindOffshore, 0.7 },
                    { GeneratorType.WindOnshore, 0.7 },
                });
                da.CommitChanges();
            }
        }
        return msg;
    }

    private static string addTransportModel(DataAccess da, Dataset dataset, string name, Dictionary<GeneratorType, double> initialScalingDict)
    {
        string msg = "";
        var tm = new TransportModel(dataset);
        tm.Name = name;
        da.BoundCalc.Add(tm);
        //
        foreach (var gt in Enum.GetValues<GeneratorType>()) {
            // ignore interconnectors as modelled as a control
            var autoScaling = true;
            double scaling = 0;
            if (initialScalingDict.ContainsKey(gt)) {
                autoScaling = false;
                scaling = initialScalingDict[gt];
            }
            var tme = new TransportModelEntry(tm, dataset) {
                GeneratorType = gt,
                TransportModel = tm,
                AutoScaling = autoScaling,
                Scaling = scaling,
            };
            da.BoundCalc.Add(tme);
        }
        msg += $"Added transport model [{name}]\n";
        return msg;
    }

    public void Report(string msg, object obj, ReportState state = ReportState.Pass)
    {
        _reporter.Report(msg, obj, state);
    }

    public static BoundCalcResults Run(
        int datasetId,
        SetPointModeNew setPointMode,
        int transportModelId,
        bool nodeMarginals,
        string? boundaryName = null,
        bool boundaryTrips = false,
        string? tripStr = null,
        string? connectionId = null,
        IHubContext<NotificationHub> hubContext = null)
    {
        var nd = new BoundCalcNetworkData(datasetId, transportModelId);
        if (connectionId != null) {
            nd.ProgressManager.ProgressUpdate += (m, p) => {
                hubContext.Clients.Client(connectionId).SendAsync("BoundCalc_AllTripsProgress", new { msg = m, percent = p });
            };
        }

        // Get network model and check not null
        var fullnet = nd.Model;
        if (fullnet == null) {
            throw new Exception("Unexpected null Network model");
        }
        // Get boundary from fullnet
        Network.Boundary? bnd = boundaryName!=null ? fullnet.BoundaryDict[boundaryName] : null;
        int nCycles = bnd != null ? bnd.SCTrips.Count + bnd.DCTrips.Count + 2 : 1;
        nd.ProgressManager.Start("Calculating", nCycles);

        //
        Network.BaseLoadFlow baseLf = new(fullnet, Network.Node.DefaultGetGen, Network.Node.DefaultGetDem);
        Network.Node lmn = baseLf.LargestMismatch();
        nd.Report($"No control max. mismatch at {lmn.Name}", baseLf.Mismatch(lmn).ToString("F1"));

        // Create network optimiser model for this network - (this could be stored in Network object?)
        NetOptimiserDescription netoptdesc = new(fullnet);
        // Create network optimiser instance
        NetOptimiser netopt = new(netoptdesc);

        // Initial setpoints for controls (manual controls may be adjusted)
        Network.CtrlSetPoints isetpts = new("isetpts", fullnet);

        BoundCalcBoundaryTripResults bcTripResults = null;
        if (bnd == null) {
            //
            // No boundary specified
            //
            // setup trips
            Network.TripSpec ts = null;
            if (tripStr != null) {
                // with user supplied trips
                string[] trips = tripStr.Split(',');
                ts = new("", trips);
            } else {
                // no trips
                ts = new("Intact");
            }
            if (setPointMode == SetPointModeNew.Auto) {
                Network.NetState ns = new(baseLf, ts, isetpts);
                (var rc1, var rc2, bool disc) = netopt.OptimiseNet(ns, out Network.FullLoadFlow lf, out _);
                var state = rc1 == LPResult.Optimum ? ReportState.Pass : ReportState.Fail;
                nd.Report($"\nOptimising network", $"{rc1}:{rc2}", state);
                // Fill in node mismatches, branch flows etc into NetworkData
                nd.FillResults(lf, ns.NSetPts, nodeMarginals);
            } else if (setPointMode == SetPointModeNew.BalanceHVDCNodes) {
                (LPResult rc1, int rc2) = netopt.BalanceHVDCNodes(isetpts, out Network.CtrlSetPoints hvdcsetpts);
                var state = rc1 == LPResult.Optimum ? ReportState.Pass : ReportState.Fail;
                nd.Report($"\nBalancing HVDC nodes result", $"{rc1}:{rc2}");
                Network.NetState ns = new(baseLf, ts, hvdcsetpts);
                var lf = ns.MakeLoadFlow();
                nd.FillResults(lf, hvdcsetpts, nodeMarginals);
            } else {
                // set setpoints from the Ctrl objects
                foreach (var ctrl in nd.Ctrls.Data) {
                    var control = nd.GetNetworkControl(ctrl);
                    if (ctrl.SetPoint != null) {
                        isetpts[control] = (double)ctrl.SetPoint;
                    }
                }
                Network.NetState ns = new(baseLf, ts, isetpts);
                var lf = ns.MakeLoadFlow();
                nd.FillResults(lf, ns.NSetPts, nodeMarginals);
            }
        } else {
            //
            // Boundary specified
            //
            // Get ranking limits for intact boundary
            Network.Boundary.LimitList bIntact = netopt.OptimiseBoundary(isetpts, bnd, bnd.InterconMargin, baseLf, [new("Intact")], 12, (Network.TripSpec ts)=> {
                return optimiseBoundaryProgress(ts, nd.ProgressManager);
            });
            // Get ranking limits for single ccts
            Network.Boundary.LimitList bSingle = netopt.OptimiseBoundary(isetpts, bnd, bnd.InterconMargin, baseLf, bnd.SCTrips, 24, (Network.TripSpec ts) => {
                return optimiseBoundaryProgress(ts, nd.ProgressManager);
            });
            // Get ranking limits for double ccts
            Network.Boundary.LimitList bDouble = netopt.OptimiseBoundary(isetpts, bnd, 0.5 * bnd.InterconMargin, baseLf, bnd.DCTrips, 24, (Network.TripSpec ts) => {
                return optimiseBoundaryProgress(ts, nd.ProgressManager);
            });
            var worstTrip = GetWorstTrip(bIntact, bSingle, bDouble);
            // Fill branch/node/ctrl results with worstLimit
            if (worstTrip.Loadflow != null) {
                nd.FillResults(worstTrip.Loadflow, worstTrip.Loadflow.NState.NSetPts, nodeMarginals);
            } else {
                throw new Exception("Unexpected null Loadflow");
            }
            //
            bcTripResults = new BoundCalcBoundaryTripResults(nd, bIntact, bSingle, bDouble, worstTrip);
        }

        nd.ProgressManager.Finish();

        //
        return new BoundCalcResults(nd, bcTripResults);
    }

    private static Network.Boundary.Limit GetWorstTrip(Network.Boundary.LimitList bIntact, Network.Boundary.LimitList bSingle, Network.Boundary.LimitList bDouble)
    {
        // Get first trip that does not have a failed outcome
        var lims = new List<Network.Boundary.Limit>();
        var wIntact = bIntact.TopN.Where(m => string.IsNullOrEmpty(m.TripOutcome)).FirstOrDefault();
        if (wIntact != null) {
            lims.Add(wIntact);
        }
        var wSingle = bSingle.TopN.Where(m => string.IsNullOrEmpty(m.TripOutcome)).FirstOrDefault();
        if (wSingle != null) {
            lims.Add(wSingle);
        }
        var wDouble = bDouble.TopN.Where(m => string.IsNullOrEmpty(m.TripOutcome)).FirstOrDefault();
        if (wDouble != null) {
            lims.Add(wDouble);
        }
        //
        var worstLimit = lims.OrderBy(m => m.BoundCap).FirstOrDefault();
        //
        if (worstLimit != null) {
            return worstLimit;
        } else {
            throw new Exception("No valid trip could be performed");
        }
    }

    private static bool optimiseBoundaryProgress(Network.TripSpec ts, ProgressManager pm)
    {
        pm.Update(ts.Name);
        return false;
    }

    public static BoundCalcResults RunBoundaryTrip(int datasetId, int transportModelId, string boundaryName, string tripName, string tripStr)
    {
        var nd = new BoundCalcNetworkData(datasetId, transportModelId);
        // Get network model and check not null
        var fullnet = nd.Model;
        //
        if (fullnet == null) {
            throw new Exception("Unexpected null full network model");
        }
        //
        Network.BaseLoadFlow baseLf = new(fullnet, Network.Node.DefaultGetGen, Network.Node.DefaultGetDem);
        Network.Node lmn = baseLf.LargestMismatch();
        nd.Report($"No control max. mismatch at {lmn.Name}", baseLf.Mismatch(lmn).ToString("F1"));

        // Create network optimiser model for this network - (this could be stored in Network object?)
        NetOptimiserDescription netoptdesc = new(fullnet);
        // Create network optimiser instance
        NetOptimiser netopt = new(netoptdesc);

        //Initial setpoints for controls (manual controls may be adjusted)
        Network.CtrlSetPoints isetpts = new("isetpts", fullnet);

        // Get boundary from fullnet
        Network.Boundary bnd = fullnet.BoundaryDict[boundaryName];
        // Create a new setpoints object with the bnd the active boundary
        Network.CtrlSetPoints bndsetpts = new(bnd.Name + ">" + isetpts.Name, isetpts) { ActiveBoundary = bnd };
        //
        // with user supplied trips
        Network.TripSpec ts;
        if (string.IsNullOrEmpty(tripStr)) {
            ts = new(tripName);
        } else {
            string[] trips = tripStr.Split(',');
            ts = new(tripName, trips);
        }

        Network.NetState ns = new(baseLf, ts, bndsetpts); // Create a new netstate with the tripspec and setpoints
        (LPResult r1, int r2, bool discon) = netopt.OptimiseNet(ns, out Network.FullLoadFlow lf, out double _);
        nd.FillResults(lf, ns.NSetPts, false);
        return new BoundCalcResults(nd);
    }

    public void FillResults(Network.LoadFlow lf, Network.CtrlSetPoints setPts, bool nodeMarginals = false)
    {
        // do node marginals if required (is processing heavy)
        double[]? margkm = null, tlf = null;
        if (nodeMarginals) {
            lf.CalcNodeMarginals(out margkm, out tlf);
        }
        foreach (var n in Nodes.Data) {
            var netNode = _nodeDict[n];
            n.Mismatch = lf.Mismatch(netNode);
            if (nodeMarginals && tlf != null && margkm != null) {
                n.TLF = tlf[netNode.Index];
                n.km = margkm[netNode.Index];
            }
        }
        foreach (var b in Branches.Data) {
            if (Model.BranchDict.TryGetValue(b.LineName, out Network.Branch netBranch)) {
                b.PowerFlow = lf.Flow(netBranch);
                b.FreePower = lf.CalcBrFree(netBranch);
            } else {
                throw new Exception($"Cannot find Network.Branch with code {b.LineName}");
            }
        }
        foreach (var c in Ctrls.Data) {
            var lineName = c.LineName;
            var netControl = Model.Controls.Where(m => m.Name == lineName).FirstOrDefault();
            if (netControl != null) {
                c.SetPoint = setPts[netControl];
            } else {
                throw new Exception($"Cannot find Network.Control with code {lineName}");
            }
        }
    }

    public class CtrlSetPoint {
        public int CtrlId { get; set; }
        public double SetPoint { get; set; }
    }

    public static void ManualSetPointMode(int datasetId, int userId, List<CtrlSetPoint> initialSetPoints)
    {
        //
        using (var da = new DataAccess()) {
            var dataset = da.Datasets.GetDataset(datasetId);
            if (dataset == null) {
                throw new Exception($"Cannot find dataset with id={datasetId}");
            }
            if (dataset.User?.Id != userId) {
                throw new Exception($"Not authorised");
            }
            var colName = "SetPoint";
            var ues = da.Datasets.GetUserEdits(typeof(Ctrl).Name, datasetId, colName);
            foreach (var sp in initialSetPoints) {
                var ue = ues.FirstOrDefault(m => m.Key.ToString() == sp.CtrlId.ToString());
                if (ue == null) {
                    ue = new UserEdit() {
                        TableName = typeof(Ctrl).Name,
                        ColumnName = colName,
                        Dataset = dataset,
                        Key = sp.CtrlId.ToString()
                    };
                    da.Datasets.Add(ue);
                }
                ue.Value = sp.SetPoint.ToString();
            }
            //
            da.CommitChanges();
        }
    }

    public static void NewDataset(Dataset newDataset)
    {
        int datasetId = newDataset.Id;
        // Get branches in separate DataAccess instance to prevent it being overwritten
        DatasetData<Branch> branchDi;
        using (var da = new DataAccess()) {
            var q = da.Session.QueryOver<Branch>();
            branchDi = new DatasetData<Branch>(da, datasetId, m => m.Id.ToString(), q);
        }

        using (var da = new DataAccess()) {
            // Check the dataset is owned by the user
            var dataset = da.Datasets.GetDataset(datasetId);
            if (dataset == null) {
                throw new Exception($"Cannot find dataset with id [{datasetId}]");
            }
            //
            var regEx = new Regex(@"^GB network (\d{4})\/\d{2} \((\d{4})\)$");
            var match = regEx.Match(dataset.Parent.Name);
            if (match.Success) {
                int year, targetYear;
                targetYear = int.Parse(match.Groups[1].Value);
                year = int.Parse(match.Groups[2].Value);
                adjustBranchCapacities(da, branchDi, dataset, year, targetYear);
            }
            da.CommitChanges();
        }
    }

    private static void adjustBranchCapacities(DataAccess da, DatasetData<Branch> branchDi, Dataset dataset, int year, int targetYear)
    {
        var adjustments = da.BoundCalc.GetBoundCalcAdjustments(year, targetYear);
        var userEdits = da.Datasets.GetUserEdits(typeof(Branch).Name, dataset.Id);
        foreach (var adj in adjustments) {
            var branch = branchDi.Data.FirstOrDefault(b => b.Code == adj.BranchCode);
            if (branch != null) {
                var userEdit = userEdits.FirstOrDefault(e => e.Key == branch.Id.ToString());
                if (userEdit == null) {
                    userEdit = new UserEdit() {
                        Dataset = dataset,
                        Key = branch.Id.ToString(),
                        TableName = typeof(Branch).Name,
                        ColumnName = "Cap"
                    };
                    da.Datasets.Add(userEdit);
                }
                var cap = adj.Capacity * 1.1;
                userEdit.Value = cap.ToString();
            } else {
                throw new Exception($"Cannot find branch with code [{adj.BranchCode}]");
            }
        }
    }

    public Network.Control GetNetworkControl(Ctrl ctrl)
    {
        if (_ctrlDict.TryGetValue(ctrl, out var cs)) {
            return cs;
        } else {
            throw new Exception($"Ctrl dict does contain expected key [{ctrl.Code}]");
        }
    }

    public List<Branch> GetBranches(IEnumerable<string> itemNames)
    {
        var fullnet = Model;
        var branches = new List<Branch>();
        foreach (var itemName in itemNames) {
            if (fullnet.BranchDict.TryGetValue(itemName, out var nb)) {
                if (_reverseBranchDict.TryGetValue(nb, out var br)) {
                    branches.Add(br);
                } else {
                    throw new Exception($"Cannot find Branch with name [{nb.Name}]");
                }
            } else {
                throw new Exception($"Cannot find Network.Branch with name [{itemName}]");
            }
        }
        //
        return branches;
    }

    public class Reporter : IReporter {
        private BoundCalcStageResults _stageResults = new BoundCalcStageResults();
        public void Report(string msg, object obj, ReportState state = ReportState.Pass)
        {
            BoundCalcStageResultEnum result;
            if (state == ReportState.Pass) {
                result = BoundCalcStageResultEnum.Pass;
            } else if (state == ReportState.Fail) {
                result = BoundCalcStageResultEnum.Fail;
            } else if (state == ReportState.Warning) {
                result = BoundCalcStageResultEnum.Warn;
            } else {
                throw new Exception($"Unexpected value of ReportState [{state}]");
            }
            var sr = _stageResults.NewStage(msg);
            _stageResults.StageResult(sr, result, obj != null ? obj.ToString() : "");
        }

        public BoundCalcStageResults StageResults
        {
            get {
                return _stageResults;
            }
        }
    }

}
