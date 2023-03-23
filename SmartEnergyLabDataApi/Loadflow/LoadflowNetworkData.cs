using System.Linq;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowNetworkData {
        public LoadflowNetworkData(Loadflow lf) {
            // Nodes
            Nodes = lf.Nodes.Objs;
            // Branches
            Branches = lf.Branches.Objs;
            // Controls
            Ctrls = lf.Ctrls.Objs;
        }

        public IList<NodeWrapper> Nodes {get; private set;}
        public IList<BranchWrapper> Branches {get; private set;}        
        public IList<CtrlWrapper> Ctrls {get; private set;}

    }
}
