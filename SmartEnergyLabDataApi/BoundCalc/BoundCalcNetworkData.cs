using System.Linq;
using System.Text.Json.Serialization;
using NHibernate;
using NHibernate.Criterion;
using Org.BouncyCastle.Asn1.Icao;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc;

public class BoundCalcNetworkData {
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

    public BoundCalcNetworkData(int datasetId, int transportModelId = 0)
    {
        // Ensure we have at least one transport model
        BoundCalcNetworkData.AddDefaultTransportModels(datasetId);

        using (var da = new DataAccess()) {
            // Transport model entries
            TransportModelEntries = da.BoundCalc.GetTransportModelEntryDatasetData(datasetId);
            // Transport models
            TransportModels = da.BoundCalc.GetTransportModelDatasetData(datasetId, null, true, TransportModelEntries);
            // work out the transport model
            if (transportModelId > 0) {
                TransportModel = TransportModels.Data.Where(m => m.Id == transportModelId).FirstOrDefault();
                if (TransportModel==null) {
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
            (Branches,Ctrls) = da.BoundCalc.GetBranchDatasetData(datasetId, null, true, Nodes);
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

    private void assignNodeLocations()
    {
        // create dictionary using ref. as key
        var locs = Locations.Data;
        var nodes = Nodes.Data;
        var locDict = new Dictionary<string, GridSubstationLocation>();
        foreach (var loc in locs) {
            if (!locDict.ContainsKey(loc.Reference)) {
                locDict.Add(loc.Reference, loc);
            }
        }
        // look up node location based on first 4 chars of code
        foreach (var n in nodes) {
            var locCode = n.Code.Substring(0, 4);
            if (n.Ext) {
                locCode += "X";
            }
            if (locDict.ContainsKey(locCode)) {
                n.Location = locDict[locCode];
            }
        }

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


}
