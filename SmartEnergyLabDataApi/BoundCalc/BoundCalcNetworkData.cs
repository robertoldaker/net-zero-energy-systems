using System.Linq;
using System.Text.Json.Serialization;
using NHibernate;
using Org.BouncyCastle.Asn1.Icao;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class BoundCalcNetworkData {
        public BoundCalcNetworkData(BoundCalc bc) {
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
            // Locations
            using (var da = new DataAccess() ) {
                Locations = loadLocations(da, bc.Dataset.Id);
                Generators = loadGenerators(da, bc.Dataset.Id);
                TransportModels = loadTransportModels(da, bc.Dataset.Id);
            }
            // Boundary branches
            BoundaryDict = new Dictionary<string, int[]>();
            foreach( var b in bc.Boundaries.Objs) {
                var boundaries = b.BoundCcts.Items.Select(m=>m.Obj.Id).ToArray();
                BoundaryDict.Add(b.name,boundaries);
            }
        }

        public DatasetData<Node> Nodes {get; private set;}
        public DatasetData<Branch> Branches {get; private set;}        
        public DatasetData<Ctrl> Ctrls {get; private set;}
        public DatasetData<Boundary> Boundaries {get; private set;}
        public DatasetData<Zone> Zones {get; private set;}
        public DatasetData<GridSubstationLocation> Locations {get; private set;}
        public DatasetData<Generator> Generators { get; private set; }
        public DatasetData<TransportModel> TransportModels {get; private set;}
        public Dictionary<string,int[]> BoundaryDict {get; private set;}

        private DatasetData<GridSubstationLocation> loadLocations(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<GridSubstationLocation>();
            var locs = new DatasetData<GridSubstationLocation>(da, datasetId,m=>m.Id.ToString(),q);
            return locs;
        }

        private DatasetData<Generator> loadGenerators(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<Generator>();
            var objs = new DatasetData<Generator>(da, datasetId,m=>m.Id.ToString(),q);
            return objs;
        }
        
        private DatasetData<TransportModel> loadTransportModels(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<TransportModel>();
            q = q.Fetch(SelectMode.Fetch,m=>m.Entries[0]);
            var objs = new DatasetData<TransportModel>(da, datasetId,m=>m.Id.ToString(),q, true);
            return objs;
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
