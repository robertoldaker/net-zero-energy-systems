using SmartEnergyLabDataApi.Data.BoundCalc;
namespace SmartEnergyLabDataApi.BoundCalc;

public class NetworkChecker {

    private Dictionary<Node,List<Node>> _nodeDict;

    public NetworkChecker(IList<Branch> branches) {
        _nodeDict = new Dictionary<Node,List<Node>>();
        foreach( var b in branches) {
            // Node 1
            if ( !_nodeDict.ContainsKey(b.Node1)) {
                _nodeDict.Add(b.Node1,new List<Node>());
            }
            _nodeDict[b.Node1].Add(b.Node2);
            // Node 2
            if ( !_nodeDict.ContainsKey(b.Node2)) {
                _nodeDict.Add(b.Node2,new List<Node>());
            }
            _nodeDict[b.Node2].Add(b.Node1);
        }
    }

    public List<List<Node>> Check() {
        // create a dictionary to store visits to nodes
        var nodeVisitDict = new Dictionary<Node,bool>();
        foreach( var n in _nodeDict.Keys) {
            nodeVisitDict.Add(n,false);
        }
        //
        var separateNetworks = new List<List<Node>>();
        //
        var cont = true;
        while( cont ) {
            var ns = nodeVisitDict.Keys.FirstOrDefault();
            if ( ns!=null) {
                visitNode(ns, nodeVisitDict);
            }
            //
            var networkNodes = nodeVisitDict.Where(m=>m.Value).Select(m=>m.Key).ToList();
            foreach (var n in networkNodes) {
                nodeVisitDict.Remove(n);
            }
            separateNetworks.Add(networkNodes);
            //
            cont = nodeVisitDict.Where(m=>!m.Value).Count()>0;
        }
        // Order list of networks be descending size
        separateNetworks = separateNetworks.OrderByDescending(m=>m.Count).ToList();
        //
        return separateNetworks;
    }

    private void visitNode(Node node, Dictionary<Node,bool> nodeVisitDict) {
        nodeVisitDict[node] = true;
        foreach( var n in _nodeDict[node]) {
            if ( !nodeVisitDict[n] ) {
                visitNode(n,nodeVisitDict);
            }
        }
    }

}
