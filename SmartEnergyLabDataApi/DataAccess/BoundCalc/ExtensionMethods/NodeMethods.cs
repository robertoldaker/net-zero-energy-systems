using HaloSoft.EventLogger;
using Npgsql.Replication;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    public static class NodeMethods {
        private static Dictionary<string, int> _nodeVoltageDict = new Dictionary<string, int>() {
            {"1",132},
            {"2",275},
            {"3",33},
            {"4",400},
            {"6",66}
        };
        public static void SetVoltage(this Node n)
        {
            if (n.Code != null && n.Code.Length > 4) {
                var vc = n.Code[4].ToString();
                if (_nodeVoltageDict.ContainsKey(vc)) {
                    n.Voltage = _nodeVoltageDict[vc];
                } else {
                    Logger.Instance.LogInfoEvent($"Could not set voltage for node [{n.Code}] as unknown voltage ident [{vc}]");
                }
            }
        }
        public static void SetLocation(this Node n, DataAccess da)
        {
            var locCode = n.GetLocationCode();
            var loc = da.NationalGrid.GetGridSubstationLocation(locCode, n.Dataset, true);
            if (loc == null) {
                loc = da.NationalGrid.GetGridSubstationLocation(locCode, null);
                if (loc != null) {
                    var newLoc = loc.Copy(n.Dataset);
                    da.NationalGrid.Add(newLoc);
                    loc = newLoc;
                }
            }
            n.Location = loc;
        }

        public static string GetLocationCode(this Node node)
        {
            // Lookup grid locations based on first 4 chars of code
            var locCode = node.Code.Substring(0, 4);
            // these are nodes at other end of inter connectors
            if (node.Ext) {
                locCode += "X";
                // this is a digit that allows for multiple interconnectors from the same source
                if (node.Code.Length > 6) {
                    locCode += node.Code[6];
                }
            }
            //
            return locCode;
        }

        public static void UpdateGenerators(this Node node, DatasetData<Node> nodeDi, DatasetData<NodeGenerator> nodeGenDi)
        {
            node.Generators = nodeGenDi.Data.Where(m => m.Node.Id == node.Id).Select(m => m.Generator).OrderBy(m => m.Name).ToList();
            node.DeletedGenerators = nodeGenDi.DeletedData.Where(m => m.Node.Id == node.Id).Select(m => m.Generator).OrderBy(m => m.Name).ToList();
            node.NewGenerators = nodeGenDi.Data.Where(m => m.Node.Id == node.Id && m.DatasetId != node.DatasetId).Select(m => m.Generator).OrderBy(m => m.Name).ToList();
            if (node.NewGenerators.Count > 0) {
                nodeDi.UserEdits.Add(new UserEdit() {
                    ColumnName = "Generators",
                    TableName = "Node",
                    Key = node.Id.ToString()
                });
            }
        }
    }
}
