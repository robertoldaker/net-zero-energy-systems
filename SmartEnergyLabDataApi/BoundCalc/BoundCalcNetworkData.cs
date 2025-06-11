using System.Linq;
using System.Text.Json.Serialization;
using NHibernate;
using NHibernate.Criterion;
using Org.BouncyCastle.Asn1.Icao;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using Lad.NetworkLibrary;
using NLog.LayoutRenderers;
using Microsoft.Extensions.ObjectPool;
using Microsoft.AspNetCore.SignalR;
using Lad.NetworkOptimiser;
using Lad.LinearProgram;
using NHibernate.Mapping;
using NHibernate.Hql.Ast.ANTLR;

namespace SmartEnergyLabDataApi.BoundCalc;

public class BoundCalcNetworkData {

    private Dictionary<Node, Network.Node> _nodeDict = new Dictionary<Node, Network.Node>();
    private Dictionary<Branch, Network.BranchSpec> _branchDict = new Dictionary<Branch, Network.BranchSpec>();
    private Dictionary<Ctrl, Network.ControlSpec> _ctrlDict = new Dictionary<Ctrl, Network.ControlSpec>();
    private Reporter _reporter = new Reporter();

    public BoundCalcNetworkData(BoundCalc bc)
    {
        // Nodes
        Nodes = bc.Nodes.DatasetData;
        // Branches
        Branches = bc.Branches.DatasetData;
        // Controls
        Ctrls = bc.Ctrls.DatasetData;
        // Boundaries
        Boundaries = bc.Boundaries.DatasetData;
        // Zones
        Zones = bc.Zones;
        // Generators
        Generators = bc.Generators;
        //
        using (var da = new DataAccess()) {
            // Locations
            Locations = loadLocations(da, bc.Dataset.Id);
            // Transport models
            TransportModels = loadTransportModels(da, bc.Dataset.Id);
            // Transport model entries
            TransportModelEntries = loadTransportModelEntries(da, bc.Dataset.Id);
            //
            setTransportModelScalings(da, bc.Dataset.Id);
        }
        // Boundary branches
        BoundaryDict = new Dictionary<string, int[]>();
        foreach (var b in bc.Boundaries.Objs) {
            var boundaries = b.BoundCcts.Items.Select(m => m.Obj.Id).ToArray();
            BoundaryDict.Add(b.name, boundaries);
        }
        //
        TransportModel = bc.TransportModel;
    }

    public BoundCalcNetworkData(int datasetId, int transportModelId = 0, SetPointMode setPointMode = SetPointMode.Auto)
    {
        // Ensure we have at least one transport model
        BoundCalcNetworkData.AddDefaultTransportModels(datasetId);

        using (var da = new DataAccess()) {
            Dataset = da.Datasets.GetDataset(datasetId);
            if (Dataset == null) {
                throw new Exception($"Cannot find dataset with id=[{datasetId}]");
            }
            SetPointMode = setPointMode;
            //
            StageResults = _reporter.StageResults;

            // Transport model entries
            TransportModelEntries = da.BoundCalc.GetTransportModelEntryDatasetData(datasetId);
            // Transport models
            TransportModels = da.BoundCalc.GetTransportModelDatasetData(datasetId, null, true, TransportModelEntries);
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
            Nodes = da.BoundCalc.GetNodeDatasetData(datasetId, null, true, Locations, ngDi);
            // Branches and controls
            (Branches, Ctrls) = da.BoundCalc.GetBranchDatasetData(datasetId, null, true, Nodes);
            // Boundaries
            Boundaries = da.BoundCalc.GetBoundaryDatasetData(datasetId);
            // Zones
            Zones = da.BoundCalc.GetZoneDatasetData(datasetId);
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
    public SetPointMode SetPointMode { get; private set; }
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
    public Network Model { get; private set; }
    public Reporter Reporter { get; private set; }

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

    private DatasetData<TransportModel> loadTransportModels(DataAccess da, int datasetId)
    {
        var data = da.BoundCalc.GetTransportModelDatasetData(datasetId, null, true);
        return data;
    }

    private DatasetData<TransportModelEntry> loadTransportModelEntries(DataAccess da, int datasetId)
    {
        var q = da.Session.QueryOver<TransportModelEntry>();
        var objs = new DatasetData<TransportModelEntry>(da, datasetId, m => m.Id.ToString(), q);
        return objs;
    }

    private void setTransportModelScalings(DataAccess da, int datasetId)
    {
        var nodeGenDi = da.BoundCalc.GetNodeGeneratorDatasetData(datasetId, null);
    }

    private Network createNetworkModel()
    {
        // set as static field in Network to allow diagnostic messages to be generated "internally" and get recorded as a StageResult
        // and hence shown in the gui
        // Network.Nodes
        List<Network.Node> networkNodes = [];
        foreach (var n in Nodes.Data) {
            Network.Node netNode = new(n.Code, n.ZoneName, 1, n.Demand, n.Generation, n.ScInFeed(), n.FaultLimit(), n.Ext);
            _nodeDict.Add(n, netNode);
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
            networkBranches.Add(bs);
        }
        // Boundaries
        List<Network.BoundSpec> networkBoundaries = [];
        foreach (var bo in Boundaries.Data) {
            var zoneNames = bo.Zones.Select(m => m.Code).ToArray();
            networkBoundaries.Add(new(bo.Code, zoneNames));
        }
        // Controls
        List<Network.ControlSpec> networkControls = [];
        foreach (var co in Ctrls.Data) {
            Network.ControlSpec cs = new(co.LineName, co.Type.ToString(), co.Cost, co.MinCtrl, co.MaxCtrl);
            _ctrlDict.Add(co, cs);
            networkControls.Add(cs);
        }

        Network fullnet = new(networkNodes, networkBranches, networkControls, networkBoundaries);

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
    }


    public static string AddDefaultTransportModels(int datasetId)
    {
        string msg = "";
        using (var da = new DataAccess()) {
            var tmDi = da.BoundCalc.GetTransportModelDatasetData(datasetId, null, false);
            var dataset = da.Datasets.GetDataset(datasetId);
            // If we haven;t got any transport models then add 2 default ones
            if (tmDi.Data.Count == 0 && tmDi.DeletedData.Count == 0) {
                msg += addTransportModel(da, dataset, "Peak Security", new Dictionary<GeneratorType, double>()
                {
                        { GeneratorType.Interconnector, 0 },
                        { GeneratorType.Tidal, 0 },
                        { GeneratorType.Wave, 0 },
                        { GeneratorType.WindOffshore, 0 },
                        { GeneratorType.WindOnshore, 0 },
                    });
                msg += addTransportModel(da, dataset, "Year Round", new Dictionary<GeneratorType, double>()
                {
                        { GeneratorType.Interconnector, 1 },
                        { GeneratorType.Nuclear, 0.85 },
                        { GeneratorType.OCGT, 0 },
                        { GeneratorType.PumpStorage, 0.5},
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
        SetPointMode setPointMode,
        int transportModelId,
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

        nd.ProgressManager.Start("Calculating", 1);

        var fullnet = nd.Model;
        //
        Network.Node lmn = fullnet.BaseLF.LargestMismatch();
        nd.Report($"No control max. mismatch at {lmn.Name}", fullnet.BaseLF.Mismatch(lmn).ToString("F1"));

        NetOptimiser netopt = new(fullnet);

        LPResult rc1 = netopt.BalanceHVDCNodes(out int rc2);
        nd.Report($"\nBalancing HVDC nodes result", $"{rc1}:{rc2}");

        if (boundaryName == null) {
            // No boundary specified
            if (tripStr != null) {
                string[] trips = tripStr.Split(',');
                Network.LoadFlow lf = fullnet.SimSolver.UseTrip(new("", trips));
                nd.FillResults(lf);
            } else {
                nd.FillResults(fullnet.BaseLF);
            }
        } else {
            // boundary specified
            Network.Boundary bnd = fullnet.BoundaryDict[boundaryName];
            netopt.SetActiveBoundary(bnd);
            string tripnm = "Intact";
            Console.WriteLine($"Boundary {bnd.Name} Planned Transfer {bnd.PlannedTransfer:F1} Interconnection Allowance {bnd.InterconMargin:F1} has {bnd.BoundaryBranches.Count} boundary branches ");
            var tripSpec = new Network.NetSim.TripSpec(tripnm);
            rc1 = netopt.OptimiseNet(tripSpec, out rc2, out var lf, out double bndcap0);

        }

        nd.ProgressManager.Finish();

        //
        return new BoundCalcResults(nd);
    }

    public void FillResults(Network.LoadFlow lf, bool nodeMarginals = false)
    {
        // always do node marginals
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
                c.SetPoint = netControl.SetPoint;
            } else {
                throw new Exception($"Cannot find Network.Control with code {lineName}");
            }
        }
    }
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
