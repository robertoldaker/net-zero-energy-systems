using NHibernate;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class Nodes : DataStore<NodeWrapper> {
        public Nodes(DataAccess da,int datasetId) : base() {
            //  
            var q = da.Session.QueryOver<Node>();
            q = q.Fetch(SelectMode.Fetch,m=>m.Zone);
            var di = new DatasetData<Node>(da,datasetId,m=>m.Code, q);
            foreach( var b in di.Data) {
                var key = b.Code;
                var objWrapper = new NodeWrapper(b);
                base.add(key,objWrapper);
            }
            DatasetData = di;
        }

        public DatasetData<Node> DatasetData {get; private set;}
    }


    public class NodeWrapper : ObjectWrapper<Node> {
        public NodeWrapper(Node obj) : base(obj) {

        }

        public double? Mismatch { 
            get {
                return Obj.Mismatch;
            } 
            set {
                Obj.Mismatch = value;
            }
        }

    }

}