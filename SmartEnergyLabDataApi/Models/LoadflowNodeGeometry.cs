using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class LoadflowNodeGeometry {

        public void LinkToGridSubstations() {

            // Download json file unless we are developing
            using( var da = new DataAccess()) {

                var gridSubstations = da.NationalGrid.GetGridSubstations();
                var branches = da.Loadflow.GetBranches();
                var nodes = da.Loadflow.GetNodes();

                var nodeDict = new Dictionary<Node,GridSubstation?>();
                foreach( var b in branches) {
                    linkToGridSubstation(gridSubstations,nodeDict,b.Node1);
                    linkToGridSubstation(gridSubstations,nodeDict,b.Node2);
                }

                Logger.Instance.LogInfoEvent($"NodeDict length=[{nodeDict.Keys.Count}]");
                Logger.Instance.LogInfoEvent($"Found=[{nodeDict.Values.Where(m=>m!=null).Count()}]");
                Logger.Instance.LogInfoEvent($"Not found=[{nodeDict.Values.Where(m=>m==null).Count()}]");


                // update links
                da.CommitChanges();
            }
        }

        private void linkToGridSubstation(IList<GridSubstation> substations, Dictionary<Node,GridSubstation?> nodeDict, Node node) {
            if ( !nodeDict.ContainsKey(node) ) {
                var gridSubstation = getGridSubstation(node, substations);
                // Link to a 
                node.SetGridSubstation(gridSubstation);
                //
                if ( gridSubstation!=null) {
                    //??Logger.Instance.LogInfoEvent($"Found GridSubstation for node [{node.Code}] [{gridSubstation.Reference}]:[[{gridSubstation.Name}]");
                } else {
                    Logger.Instance.LogInfoEvent($"Could not find feature for node [{node.Code}] [{node.Demand}:{node.Generation_A}:{node.Generation_B}]");
                }
                nodeDict.Add(node,gridSubstation);
            }
        }

        GridSubstation? getGridSubstation(Node node, IList<GridSubstation> substations) {
            return substations.Where(m=>node.Code.StartsWith(m.Reference.Substring(0,4))).FirstOrDefault();
        }
    }
}