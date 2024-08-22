using NHibernate;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class Nodes : DataStore<NodeWrapper> {
        public Nodes(DataAccess da,int datasetId, DatasetData<GridSubstationLocation> locDatasetData) : base() {
            //  
            var q = da.Session.QueryOver<Node>();
            q = q.Fetch(SelectMode.Fetch,m=>m.Zone);
            q = q.OrderBy(m=>m.Code).Asc;
            var di = new DatasetData<Node>(da,datasetId,m=>m.Id.ToString(), q);
            foreach( var node in di.Data) {                
                if ( node.Location!=null ){
                    node.Location = locDatasetData.GetItem(node.Location.Id);
                }
            }
            foreach( var node in di.Data) {                
                var key = node.Code;
                var objWrapper = new NodeWrapper(node);
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