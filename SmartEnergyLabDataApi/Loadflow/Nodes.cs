using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class Nodes : DataStore<NodeWrapper> {
        public Nodes(IList<Node> bs) : base() {
            foreach( var b in bs) {
                var key = b.Code;
                var objWrapper = new NodeWrapper(b);
                base.add(key,objWrapper);
            }
        }

    }

    public class NodeWrapper : ObjectWrapper<Node> {
        public NodeWrapper(Node obj) : base(obj) {

        }

        public double? Mismatch { get; set; }

    }
}