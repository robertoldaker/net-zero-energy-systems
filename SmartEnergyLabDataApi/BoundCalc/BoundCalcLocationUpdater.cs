using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using SmartEnergyLabDataApi.Models;
using static SmartEnergyLabDataApi.BoundCalc.BoundCalcETYSLoader;

namespace SmartEnergyLabDataApi.BoundCalc;

public class BoundCalcLocationUpdater {


    private Dictionary<string,SubstationCode> _codesDict;

    public string Update(int datasetId) {

        var etysLoader = new BoundCalcETYSLoader();
        _codesDict = etysLoader.LoadSubstationCodes();

        (int initialNotFound,int ratio) = getNumNodesWthoutLocations(datasetId);
        // do nothing is ratio of not found is too great
        if ( ratio>0.3 ) {
            return $"Ratio of nodes without locations is too great [{ratio}]";
        }
        //
        bool cont = true;
        int prevNotFound = 0;
        int notFound=0;
        //
        while ( cont ) {
            notFound = updateLocations(datasetId);
            Logger.Instance.LogInfoEvent($"[{notFound}] nodes without locations");
            cont = notFound != prevNotFound;
            prevNotFound = notFound;
        }
        //
        return $"Number of nodes without locations reduced from [{initialNotFound}] to [{notFound}]";
    }

    public (int,int) getNumNodesWthoutLocations(int datasetId) {
        using( var da = new DataAccess() ) {
            var dataset = da.Datasets.GetDataset(datasetId);
            if ( dataset==null ) {
                throw new Exception($"Cannot find dataset with id=[{datasetId}]");
            }
            var nodes = da.BoundCalc.GetNodes(dataset);
            var notFoundDict = getNotFoundDict(nodes);
            return (notFoundDict.Count, notFoundDict.Count/ nodes.Count);
        }
    }

    private Dictionary<string,IList<Node>> getNotFoundDict(IList<Node> nodes) {
        var notFoundDict = new Dictionary<string,IList<Node>>();
        foreach( var node in nodes) {
            var locCode = node.GetLocationCode();
            // see if the location exists
            var loc = node.Location;
            if ( loc==null && !node.Ext)  {
                if ( !notFoundDict.ContainsKey(locCode) ) {
                    notFoundDict.Add(locCode,new List<Node>());
                }
                notFoundDict[locCode].Add(node);
            } 
        }
        return notFoundDict;
    }

    private int updateLocations(int datasetId) {

        //
        Dictionary<string,IList<Node>> notFoundDict;

        using( var da = new DataAccess() ) {
            var dataset = da.Datasets.GetDataset(datasetId);
            if ( dataset==null ) {
                throw new Exception($"Cannot find dataset with id=[{datasetId}]");
            }
            var nodes = da.BoundCalc.GetNodes(dataset);
            var branches = da.BoundCalc.GetBranches(dataset);
            var gridSubstationLocations = da.NationalGrid.GetGridSubstationLocations(dataset);

            var knownLocations = new Dictionary<string,double[]>() {
                {"MEDB",new double[] {55.06572,-1.76592}}, // MEDBURN TEE B
                {"HEDD",new double[] {55.06572,-1.76592}}, // 
                {"AUCH",new double[] {55.07064,-5.02455}}, // AUCHENCROSH 
                {"CREA",new double[] {58.21040,-4.50244}}, // CREAG RIABHACH WINDFARM
                {"MILW",new double[] {57.12366,-4.84634}}  // MILLENIUM WIND
            };

            //
            notFoundDict = getNotFoundDict(nodes);

            foreach( var nf in notFoundDict ) {
                var locCode = nf.Key;
                GridSubstationLocation newLoc = null;
                if ( knownLocations.ContainsKey(locCode)) {
                    newLoc = GridSubstationLocation.Create(locCode,GridSubstationLocationSource.Estimated, dataset);
                    newLoc.Name = getLocationName(locCode);
                    newLoc.GISData.Latitude = knownLocations[locCode][0];
                    newLoc.GISData.Longitude = knownLocations[locCode][1];
                    da.NationalGrid.Add(newLoc);
                    Logger.Instance.LogInfoEvent($"Added known location for code [{locCode}] [{newLoc.Name}]");
                } else {
                    newLoc = addEstimatedLocation(locCode, branches, gridSubstationLocations, dataset);
                    if ( newLoc!=null ) {
                        da.NationalGrid.Add(newLoc);
                        Logger.Instance.LogInfoEvent($"Added estimated location for code [{newLoc.Reference}] [{newLoc.Name}]");
                    } else {
                        Logger.Instance.LogInfoEvent($"Could not estimate location for code [{locCode}]");
                    }
                }
                if ( newLoc!=null ) {
                    var ns = notFoundDict[locCode];
                    foreach( var node in ns) {
                        node.Location = newLoc;
                    }
                }
            }

            // update links
            da.CommitChanges();  

            //
            return notFoundDict.Count;                    
        }
    }

    private string getLocationName(string locCode) {
        if ( _codesDict.ContainsKey(locCode)) {
            return _codesDict[locCode].Name;
        } else {
            return locCode;
        }
    }

    private GridSubstationLocation addEstimatedLocation(string code,IList<Branch> branches, IList<GridSubstationLocation> gridSubstationLocations, Dataset dataset) {
        var name = getLocationName(code);
        var connectedNodes = branches.Where(m=>m.Node1.Code.Substring(0,4) == code && m.Node2.Code.Substring(0,4)!=code && m.Node2.Location!=null).Select(m=>m.Node2).ToList();
        var connected2Nodes = branches.Where(m=>m.Node2.Code.Substring(0,4) == code && m.Node1.Code.Substring(0,4)!=code && m.Node1.Location!=null).Select(m=>m.Node1).ToList();
        connectedNodes.AddRange(connected2Nodes);
        //
        GridSubstationLocation newLoc = null;
        var connectedCodes = connectedNodes.Select( m=>m.Code.Substring(0,4)).Distinct().ToList();
        if ( connectedCodes.Count>0) {
            double lat=0,lng=0;
            int nLocs=0;
            foreach( var c in connectedCodes) {
                var loc = gridSubstationLocations.Where(m=>m.Reference==c).FirstOrDefault();
                if ( loc!=null){
                    lat+=loc.GISData.Latitude;
                    lng+=loc.GISData.Longitude;
                    nLocs++;
                }
            }
            //
            if ( lat!=0 && lng!=0) {
                lat = lat/ (double) nLocs;
                lng = lng / (double) nLocs;
                // Add an estimated location
                newLoc = GridSubstationLocation.Create(code,GridSubstationLocationSource.Estimated, dataset);
                newLoc.GISData.Latitude = lat;
                newLoc.GISData.Longitude = lng;
                newLoc.Name = name;
            }
        }
        return newLoc;
    }


}