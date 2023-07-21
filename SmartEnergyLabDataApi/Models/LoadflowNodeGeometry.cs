using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class LoadflowNodeGeometry {

        private static Dictionary<string,int> _nodeVoltageDict = new Dictionary<string, int>() {
            {"1",132},
            {"2",275},
            {"3",33},
            {"4",400},
            {"6",66}
        };

        public void LinkToGridSubstations() {

            // Download json file unless we are developing
            using( var da = new DataAccess()) {

                var gridSubstations = da.NationalGrid.GetGridSubstations();
                var gridSubstationLocations = da.NationalGrid.GetGridSubstationLocations();
                var nodes = da.Loadflow.GetNodes();

                //
                int nFound=0;
                foreach( var node in nodes) {
                    // Lookup grid locations based on fir 4 chars of code
                    var locCode = node.Code.Substring(0,4);
                    // use 5th char as reference to voltage
                    var volDig = node.Code.Substring(4,1);
                    // these are nodes at other end of inter connectors
                    if ( node.Ext ) {
                        locCode+="X";
                    }
                    // see if the location exists
                    var loc = gridSubstationLocations.Where(m=>m.Reference==locCode).FirstOrDefault();
                    if ( loc==null)  {
                        Logger.Instance.LogWarningEvent($"Could not find location for node [{node.Code}]");
                    } else {
                        nFound++;
                        node.Location = loc;
                        if ( _nodeVoltageDict.ContainsKey(volDig) ) {
                            node.Voltage = _nodeVoltageDict[volDig];
                        } else {
                            Logger.Instance.LogWarningEvent($"Unexpected voltage digit for node [{node.Code}] [{volDig}]");
                        }
                    }
                }

                Logger.Instance.LogInfoEvent($"Found [{nFound}] locations of [{nodes.Count}] available nodes");


                // update links
                da.CommitChanges();
            }
        }

    }
}