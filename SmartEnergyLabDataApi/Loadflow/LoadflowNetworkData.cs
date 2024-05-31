using System.Linq;
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
            //
            using( var da = new DataAccess() ) {
                // Boundaries
                Boundaries = loadBoundaries(da,lf.Dataset.Id);
                // Zones
                Zones = loadZones(da,lf.Dataset.Id);
            }

        }

        public DatasetData<Node> Nodes {get; private set;}
        public DatasetData<Branch> Branches {get; private set;}        
        public DatasetData<Ctrl> Ctrls {get; private set;}
        public DatasetData<Data.Boundary> Boundaries {get; private set;}
        public DatasetData<Zone> Zones {get; private set;}

        private DatasetData<Data.Boundary> loadBoundaries(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<Data.Boundary>();
            var ds = new DatasetData<Data.Boundary>(da, datasetId,m=>m.Id.ToString(),q);
            // add zones they belong to
            var boundDict = da.Loadflow.GetBoundaryZoneDict(ds.Data);
            foreach( var b in ds.Data) {
                if ( boundDict.ContainsKey(b) ) {
                    b.Zones = boundDict[b];
                } else {
                    b.Zones = new List<Zone>();
                }
            }
            return ds;
        }

        private DatasetData<Zone> loadZones(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<Zone>();
            var ds = new DatasetData<Zone>(da, datasetId,m=>m.Id.ToString(),q);
            return ds;
        }

    }
}
