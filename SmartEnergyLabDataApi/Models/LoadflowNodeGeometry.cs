using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class LoadflowNodeGeometry {

        private class InterConnector {
            public LatLng LatLng {get; private set;}
            public string Name {get; private set;}
            public InterConnector(string name, LatLng latLng) {
                Name = name;
                LatLng = latLng;
            }
        }

        private class LatLng {
            public double Lat {get; set;}
            public double Lng {get; set;}
        }

        // These are on the end of the inter-connectors so show them in their respective countries
        private static Dictionary<string,InterConnector> _nodeInterConnectors = new Dictionary<string, InterConnector>() {
            { "SELL4X", new InterConnector("IFA1", new LatLng() {Lat=50.755175, Lng= 1.63329}) },
            { "CHIL4X", new InterConnector("IFA2", new LatLng() {Lat=49.84289, Lng= 0.84227}) },
            { "RICH4X", new InterConnector("NEMO", new LatLng() {Lat=51.142778, Lng= 2.86376}) },
            { "GRAI4X", new InterConnector("BritNed", new LatLng() {Lat=51.527152, Lng= 3.56688}) }, 
            { "CONQ4X", new InterConnector("EWLink", new LatLng() {Lat=53.714219, Lng=-6.21094}) },  
        };

        public void LinkToGridSubstations() {

            // Download json file unless we are developing
            using( var da = new DataAccess()) {

                var gridSubstations = da.NationalGrid.GetGridSubstations();
                var nodes = da.Loadflow.GetNodes();

                var nodeDict = new Dictionary<Node,GridSubstation?>();
                // try with first 5 chars
                foreach( var node in nodes) {
                    nodeDict.Add(node,null);
                    linkToGridSubstation(gridSubstations,nodeDict,node,5);
                }
                // then with 4 chars
                foreach( var node in nodes) {
                    linkToGridSubstation(gridSubstations,nodeDict,node,4);
                }

                Logger.Instance.LogInfoEvent($"NodeDict length=[{nodeDict.Keys.Count}]");
                Logger.Instance.LogInfoEvent($"Found=[{nodeDict.Values.Where(m=>m!=null).Count()}]");
                Logger.Instance.LogInfoEvent($"Not found=[{nodeDict.Values.Where(m=>m==null).Count()}]");


                // update links
                da.CommitChanges();
            }
        }

        private void linkToGridSubstation(IList<GridSubstation> substations, Dictionary<Node,GridSubstation?> nodeDict, Node node, int compLength) {
            if ( _nodeInterConnectors.ContainsKey(node.Code)) {
                if ( node.GISData==null ) {
                    node.GISData = new GISData();
                }
                var ic = _nodeInterConnectors[node.Code];
                node.Name=ic.Name;
                node.GISData.Latitude = ic.LatLng.Lat;
                node.GISData.Longitude = ic.LatLng.Lng;
            } else {
                var gridSubstation = nodeDict[node];
                if ( gridSubstation==null) {
                    gridSubstation = getGridSubstation(node, substations, compLength);
                    if ( gridSubstation!=null) {
                        // Copy over GIS data from gridsubstation
                        if  ( node.GISData==null ) {
                            node.GISData = new GISData();
                        }
                        node.GISData.Latitude = gridSubstation.GISData.Latitude;
                        node.GISData.Longitude = gridSubstation.GISData.Longitude;
                        //
                        node.Name = gridSubstation.Name;
                        //
                        nodeDict[node]=gridSubstation;
                    }
                    //
                    if ( gridSubstation!=null) {
                        //??Logger.Instance.LogInfoEvent($"Found GridSubstation for node [{node.Code}] [{gridSubstation.Reference}]:[[{gridSubstation.Name}]");
                    } else {
                        Logger.Instance.LogInfoEvent($"Could not find feature for node [{node.Code}] [{node.Demand}:{node.Generation_A}:{node.Generation_B}]");
                    }
                }
            }
        }

        GridSubstation? getGridSubstation(Node node, IList<GridSubstation> substations, int compLength) {
            return substations.Where(m=>node.Code.StartsWith(m.Reference.Substring(0,compLength))).FirstOrDefault();
        }
    }
}