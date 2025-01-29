using System.ComponentModel;
using System.Text.Json.Serialization;
using NHibernate;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.Loadflow;

namespace SmartEnergyLabDataApi.Loadflow
{

    public class Branches : DataStore<BranchWrapper> {
        public Branches(DataAccess da, int datasetId, Nodes nodes) {
            var q = da.Session.QueryOver<Branch>();
            var di = new DatasetData<Branch>(da,datasetId,m=>m.Id.ToString(),q);            
            foreach( var b in di.Data) {
                b.Node1 = nodes.DatasetData.GetItem(b.Node1Id);
                b.Node2 = nodes.DatasetData.GetItem(b.Node2Id);
                var key = b.LineName;
                var objWrapper = new BranchWrapper(b,nodes);
                base.add(key,objWrapper);
            }
            DatasetData = di;
        }
        public DatasetData<Branch> DatasetData {get; private set;}

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

        // Control associated with branch (bctrl)
        [JsonIgnore()]
        public CtrlWrapper Ctrl {get; set;}

        // outaged (bout)
        public bool Outaged {
                get {
                    return Obj.Outaged;
                }
                set {
                    Obj.Outaged = value;
                }
        }

        // Intact planned flow (ipflow)
        public double? PowerFlow {
            get {
                return Obj.PowerFlow;
            }
            set {
                Obj.PowerFlow = value;
            }
        }

        // Boundary flow
        public double? BFlow {
            get {
                return Obj.BFlow;
            }
            set {
                Obj.BFlow = value;
            }
        }

        // Free power
        public double? FreePower {
            get {
                return Obj.FreePower;
            }
            set {
                Obj.FreePower = value;
            }
        }
    }
}