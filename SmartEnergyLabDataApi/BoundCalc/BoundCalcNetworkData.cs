using System.Linq;
using Org.BouncyCastle.Asn1.Icao;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class BoundCalcNetworkData {
        public BoundCalcNetworkData(BoundCalc lf) {
            // Nodes
            Nodes = lf.Nodes.DatasetData;
            // Branches
            Branches = lf.Branches.DatasetData;
            // Controls
            Ctrls = lf.Ctrls.DatasetData;
            // Boundaries
            Boundaries = lf.Boundaries.DatasetData;
            // Zones
            Zones = lf.Zones;
            // Locations
            using (var da = new DataAccess() ) {
                Locations = loadLocations(da, lf.Dataset.Id);
            }
            //?? Not needed as reverted to using node.Location as a db foreign key
            //??assignNodeLocations();
        }

        public DatasetData<BoundCalcNode> Nodes {get; private set;}
        public DatasetData<BoundCalcBranch> Branches {get; private set;}        
        public DatasetData<BoundCalcCtrl> Ctrls {get; private set;}
        public DatasetData<BoundCalcBoundary> Boundaries {get; private set;}
        public DatasetData<BoundCalcZone> Zones {get; private set;}
        public DatasetData<GridSubstationLocation> Locations {get; private set;}

        private DatasetData<GridSubstationLocation> loadLocations(DataAccess da, int datasetId) {
            var q = da.Session.QueryOver<GridSubstationLocation>();
            var locs = new DatasetData<GridSubstationLocation>(da, datasetId,m=>m.Id.ToString(),q);
            return locs;
        }

        private void assignNodeLocations() {
            // create dictionary using ref. as key
            var locs = Locations.Data;
            var nodes = Nodes.Data;
            var locDict = new Dictionary<string,GridSubstationLocation>();
            foreach( var loc in locs) {
                if ( !locDict.ContainsKey(loc.Reference)) {
                    locDict.Add(loc.Reference,loc);
                }
            }
            // look up node location based on first 4 chars of code
            foreach( var n in nodes) {
                var locCode = n.Code.Substring(0,4);
                if ( n.Ext ) {
                    locCode+="X";
                }
                if ( locDict.ContainsKey(locCode)) {
                    n.Location = locDict[locCode];
                }
            }

        }

    }
}
