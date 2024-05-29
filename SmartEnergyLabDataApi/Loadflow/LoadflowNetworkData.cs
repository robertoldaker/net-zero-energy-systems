using System.Linq;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowNetworkData {
        public LoadflowNetworkData(Loadflow lf) {
            // Nodes
            Nodes = lf.Nodes.DatasetData;
            // Branches
            Branches = lf.Branches.DatasetData;
            // Controls
            Ctrls = lf.Ctrls.DatasetData;
        }

        public DatasetData<Node> Nodes {get; private set;}
        public DatasetData<Branch> Branches {get; private set;}        
        public DatasetData<Ctrl> Ctrls {get; private set;}

    }
}
