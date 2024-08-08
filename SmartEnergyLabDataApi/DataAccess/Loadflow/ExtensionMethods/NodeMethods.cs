using HaloSoft.EventLogger;
using Npgsql.Replication;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data
{
    public static class NodeMethods {
        private static Dictionary<string,int> _nodeVoltageDict = new Dictionary<string, int>() {
            {"1",132},
            {"2",275},
            {"3",33},
            {"4",400},
            {"6",66}
        };
        public static void SetVoltage(this Node n) {
            if ( n.Code!=null && n.Code.Length>4) {
                var vc = n.Code[4].ToString();
                if ( _nodeVoltageDict.ContainsKey(vc) ) {
                    n.Voltage = _nodeVoltageDict[vc];
                } else {
                    Logger.Instance.LogInfoEvent($"Could not set voltage for node [{n.Code}] as unknown voltage ident [{vc}]");
                }
            }
        }
        public static void SetLocation(this Node n, DataAccess da) {
            var locCode = n.Code.Substring(0,4);
            var loc = da.NationalGrid.GetGridSubstationLocation(locCode,n.Dataset);
            if ( loc == null ) {
                loc = da.NationalGrid.GetGridSubstationLocation(locCode,null);
                if ( loc!=null ) {
                    var newLoc = loc.Copy(n.Dataset);
                    da.NationalGrid.Add(newLoc);
                }
            }
        }
    }
}