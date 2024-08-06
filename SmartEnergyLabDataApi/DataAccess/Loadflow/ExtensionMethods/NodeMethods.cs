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
        public static void SetLocation(this Node n, DataAccess da) {
            if ( n.Code!=null && n.Code.Length>4) {
                var locCode = n.Code.Substring(0,4);
                // First try with locations in this dataset 
                var location = da.NationalGrid.GetGridSubstationLocation(locCode, n.Dataset);
                if ( location!=null ) {
                    n.Location = location;
                }
                // If no match then try looking for ones with no dataset
                if ( location==null ) {
                    location = da.NationalGrid.GetGridSubstationLocation(locCode);
                    if ( location!=null ) {
                        n.Location = location.Copy(n.Dataset);
                        da.NationalGrid.Add(n.Location);
                    }
                }
            }
        }
    }
}