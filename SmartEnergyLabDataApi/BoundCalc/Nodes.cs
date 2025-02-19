using NHibernate;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
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
            int index=1;
            //?? Not sure why this is necessary??
            //?? Without it get a different ref. node?
            var diData = di.Data.OrderBy(m=>m.Id);
            foreach( var node in diData) {                
            //??foreach( var node in di.Data) {                
                var key = node.Code;
                var objWrapper = new NodeWrapper(node,index);
                base.add(key,objWrapper);
                index++;
            }
            DatasetData = di;
        }

        public DatasetData<Node> DatasetData {get; private set;}
    }


    public class NodeWrapper : ObjectWrapper<Node> {
        public NodeWrapper(Node obj, int index) : base(obj, index) {

        }

        public int Pn {get; set;} // Row/Column position of node in admittance matrix

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