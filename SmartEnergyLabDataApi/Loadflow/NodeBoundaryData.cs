using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class NodeBoundaryData {

        private Dictionary<int,bool> _zonesIn;
        public NodeBoundaryData(IList<BoundaryZone> boundaryZones) {
            _zonesIn = new Dictionary<int, bool>();
            foreach( var bz in boundaryZones) {
                _zonesIn.Add(bz.Zone.Id,true);
            }
        }

        public bool IsInBoundary(NodeWrapper nw) {
            return _zonesIn.ContainsKey(nw.Obj.Zone.Id);
        }

        public int Count {
            get {
                return _zonesIn.Count;
            }
        }
    }
}