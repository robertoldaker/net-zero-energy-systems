using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using SmartEnergyLabDataApi.Elsi;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class NodeBoundaryData {

        private Dictionary<int,bool> _zonesIn;
        public NodeBoundaryData(IList<BoundCalcZone> zones) {
            _zonesIn = new Dictionary<int, bool>();
            foreach( var bz in zones) {
                _zonesIn.Add(bz.Id,true);
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