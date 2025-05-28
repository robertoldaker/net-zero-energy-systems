using System.Linq;
using System.Text.Json.Serialization;
using NHibernate;
using NHibernate.Criterion;
using Org.BouncyCastle.Asn1.Icao;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
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
            // NodeGenerators
            NodeGenerators = bc.NodeGenerators;
            //
            using (var da = new DataAccess()) {
                // Locations
                Locations = loadLocations(da, bc.Dataset.Id);
                // Transport models
                TransportModels = loadTransportModels(da, bc.Dataset.Id);
                // Transport model entries
                TransportModelEntries = loadTransportModelEntries(da, bc.Dataset.Id);
            }
            // Boundary branches
            BoundaryDict = new Dictionary<string, int[]>();
            foreach (var b in bc.Boundaries.Objs) {
                var boundaries = b.BoundCcts.Items.Select(m => m.Obj.Id).ToArray();
                BoundaryDict.Add(b.name, boundaries);
            }
            //
            setTransportModelScalings();
            //
            TransportModel = bc.TransportModel;
        }

        public DatasetData<Node> Nodes {get; private set;}
        public DatasetData<Branch> Branches {get; private set;}
        public DatasetData<Ctrl> Ctrls {get; private set;}
        public DatasetData<Boundary> Boundaries {get; private set;}
        public DatasetData<Zone> Zones {get; private set;}
        public DatasetData<GridSubstationLocation> Locations {get; private set;}
        public DatasetData<Generator> Generators { get; private set; }
        public DatasetData<NodeGenerator> NodeGenerators { get; private set; }
        public DatasetData<TransportModel> TransportModels {get; private set;}
        public TransportModel? TransportModel { get; private set; }
        public DatasetData<TransportModelEntry> TransportModelEntries { get; private set; }
        public Dictionary<string,int[]> BoundaryDict {get; private set;}
        private DatasetData<GridSubstationLocation> loadLocations(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<GridSubstationLocation>();
            var locs = new DatasetData<GridSubstationLocation>(da, datasetId, m => m.Id.ToString(), q);
            return locs;
        }

        private DatasetData<Generator> loadGenerators(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<Generator>();
            var objs = new DatasetData<Generator>(da, datasetId,m=>m.Id.ToString(),q);
            return objs;
        }

        private DatasetData<TransportModel> loadTransportModels(DataAccess da, int datasetId)
        {
            var data = da.BoundCalc.GetTransportModelDatasetData(datasetId);
            return data;
        }

        private DatasetData<NodeGenerator> loadNodeGenerators(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<NodeGenerator>();
            var objs = new DatasetData<NodeGenerator>(da, datasetId,m=>m.Id.ToString(),q);
            return objs;
        }

        private DatasetData<TransportModelEntry> loadTransportModelEntries(DataAccess da, int datasetId)
        {
            var q = da.Session.QueryOver<TransportModelEntry>();
            var objs = new DatasetData<TransportModelEntry>(da, datasetId, m => m.Id.ToString(), q);
            return objs;
        }

        private void setTransportModelScalings()
        {
            foreach (var tm in this.TransportModels.Data) {
                tm.UpdateScaling(Nodes.Data, this.NodeGenerators.Data, this.Generators.Data);
            }
        }

        private void assignNodeLocations()
        {
            // create dictionary using ref. as key
            var locs = Locations.Data;
            var nodes = Nodes.Data;
            var locDict = new Dictionary<string, GridSubstationLocation>();
            foreach (var loc in locs)
            {
                if (!locDict.ContainsKey(loc.Reference))
                {
                    locDict.Add(loc.Reference, loc);
                }
            }
            // look up node location based on first 4 chars of code
            foreach (var n in nodes)
            {
                var locCode = n.Code.Substring(0, 4);
                if (n.Ext)
                {
                    locCode += "X";
                }
                if (locDict.ContainsKey(locCode))
                {
                    n.Location = locDict[locCode];
                }
            }

        }

    }
}
