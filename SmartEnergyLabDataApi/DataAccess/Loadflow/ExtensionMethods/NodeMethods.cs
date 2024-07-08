using HaloSoft.EventLogger;
using Npgsql.Replication;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data
{
    public static class NodeMethods {
        public static void SetVoltage(this Node n) {
            if ( n.Code!=null && n.Code.Length>4) {
                var vc = n.Code[4].ToString();
                if ( LoadflowNodeGeometry.NodeVoltageDict.ContainsKey(vc) ) {
                    n.Voltage = LoadflowNodeGeometry.NodeVoltageDict[vc];
                } else {
                    Logger.Instance.LogInfoEvent($"Could not set voltage for node [{n.Code}] as unknown voltage ident [{vc}]");
                }
            }
        }
    }
}