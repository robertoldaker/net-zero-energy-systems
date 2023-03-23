using System.Text.Json.Serialization;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{

    public class Branches : DataStore<BranchWrapper> {
        public Branches(IList<Branch> bs, Nodes nodes) {
            foreach( var b in bs) {
                var key = getLineName(b);
                var objWrapper = new BranchWrapper(b,nodes);
                base.add(key,objWrapper);
            }
        }

        private string getLineName(Branch b) {
            return b.LineName;
        }

        public bool IsDisconnected(Nodes nodes) {
            var snet = new int[nodes.Count];
            for( int i=0; i<snet.Length;i++ ) {
                snet[i] = i;
            }
            bool chng = false;
            int j1, j2;
            do {
                chng = false;
                foreach( var b in Objs) {
                    var branch = b.Obj;
                    j1 = nodes.getIndex(branch.Node1.Code);
                    j2 = nodes.getIndex(branch.Node2.Code);
                    if ( snet[j1] < snet[j2]) {
                        snet[j2] = snet[j1];
                        chng = true;
                    } else if ( snet[j2] < snet[j1]) {
                        snet[j1] = snet[j2];
                        chng = true;
                    }
                }
            } while (chng);
            //
            var nonZeros = snet.Where(m=>m!=0).ToList();
            return nonZeros.Count()>0;
        }

        public void ResetOutaged() {
            foreach( var bw in _list) {
                bw.Outaged = false;
            }
        }


    }

    public class BranchWrapper : ObjectWrapper<Branch> {
        public BranchWrapper(Branch obj,  Nodes nodes) : base(obj) {
            Node1 = nodes.get(obj.Node1.Code);
            Node2 = nodes.get(obj.Node2.Code);
        }

        // Line code name (lcstr)
        public string LineName {
            get {
                return Obj.LineName;
            }
        }

        [JsonIgnore()]
        // Node 1 wrapper
        public NodeWrapper Node1 {get; set;}

        // Node 2 wrapper
        [JsonIgnore()]
        public NodeWrapper Node2 {get; set;}

        // Node 1 order position (bn1)
        public int Node1Index {get; set;}

        // Node 2 order position (bn2)
        public int Node2Index {get; set;}

        // outaged (bout)
        public bool Outaged {get; set;}

        // Control associated with branch (bctrl)
        [JsonIgnore()]
        public CtrlWrapper Ctrl {get; set;}

        // Intact planned flow (ipflow)
        public double? PowerFlow {get; set;}

        // Boundary flow
        public double? BFlow {get; set;}

        public double? FreePower {get; set;}
    }
}