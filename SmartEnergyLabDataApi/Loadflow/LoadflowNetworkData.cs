using System.Linq;
using Org.BouncyCastle.Asn1.Icao;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowNetworkData {
        public LoadflowNetworkData(Loadflow lf) {
            // Nodes
            Nodes = lf.Nodes.DatasetData;
            // Branches
            Branches = lf.Branches.DatasetData;
            // Controls
            Ctrls = lf.Ctrls.DatasetData;
            // Boundaries
            Boundaries = lf.Boundaries;
            // Zones
            Zones = lf.Zones;
            // Locations
            using (var da = new DataAccess() ) {
                Locations = loadLocations(da, lf.Dataset.Id);
            }
        }

        public DatasetData<Node> Nodes {get; private set;}
        public DatasetData<Branch> Branches {get; private set;}        
        public DatasetData<Ctrl> Ctrls {get; private set;}
        public DatasetData<Data.Boundary> Boundaries {get; private set;}
        public DatasetData<Zone> Zones {get; private set;}
        public DatasetData<GridSubstationLocation> Locations {get; private set;}

        private DatasetData<GridSubstationLocation> loadLocations(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<GridSubstationLocation>();
            var locs = new DatasetData<GridSubstationLocation>(da, datasetId,m=>m.Id.ToString(),q);
            return locs;
        }

    }
}
