using System.Linq;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Linq;
using NHibernate.Util;
using NLog.LayoutRenderers;
using Org.BouncyCastle.Asn1.Mozilla;
using Org.BouncyCastle.Crypto.Signers;
using Renci.SshNet.Security;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;
using static SmartEnergyLabDataApi.Models.GoogleMapsGISFinder;

namespace SmartEnergyLabDataApi.Loadflow;
public class LoadflowETYSLoader
{
    public enum LoadOptions {All,OnlyHighVoltageCircuits};

    private LoadOptions _loadOptions;
    private HttpClient _httpClient;
    private object _httpClientLock = new object();

    private string APPENDIX_B_URL = "https://www.nationalgrideso.com/document/304986/download";
    private string APPENDIX_G_URL = "https://www.nationalgrideso.com/document/294506/download";
    private string DATASET_BASE_NAME = "GB network";
    private int BASE_YEAR = 2023;

    private GoogleMapsGISFinder _gisFinder = new GoogleMapsGISFinder();

    
    public LoadflowETYSLoader(LoadOptions loadOptions)
    {
        _loadOptions = loadOptions;
    }

    public void FixMissingZones() {
        // repeat until we are not setting any more zones
        while ( updateNodeZones() ) {

        }
    }

    public void Load() {
        string fnB = saveAppendix(APPENDIX_B_URL);
        loadAppendixB(fnB, out var codesDict);
        string fnG = saveAppendix(APPENDIX_G_URL);
        loadAppendixG(fnG);
        // repeat until we are not setting any more zones
        while ( updateNodeZones() ) {

        }
        // set node locations
        updateNodeLocations(codesDict);
    }

    private bool updateNodeZones() {
        bool result = false;
        using( var da = new DataAccess() ) {

            var knownZonesDict = new Dictionary<string,string>() {
                {"ALYT","T2"},
                {"BRCW","T4"},
                {"DISS","J3"},
                {"MEDB","Q5"},
                {"SHUR","E7"},
                {"MARH","K5"},
                {"MARV","D4"},
                {"STOB","L7"},
                {"SAFO","G6"}
            };

            var dataset = getDataset(da,BASE_YEAR);
            var nodes = da.Loadflow.GetNodes(dataset);
            var branches = da.Loadflow.GetBranches(dataset);
            var zones = da.Loadflow.GetZones(dataset);
            //
            var nodesNoZones = nodes.Where(m=>m.Zone==null).ToList();
            //
            Logger.Instance.LogInfoEvent($"Number of nodes no zones = [{nodesNoZones.Count}]");
            //
            var nodeLocCodes = nodesNoZones.Select(m=>m.Code.Substring(0,4)).Distinct().ToList();
            //
            foreach( var locCode in nodeLocCodes) {
                // get node2 zones
                var nodeZones = branches.Where( m=>m.Node1.Code.Substring(0,4)==locCode && m.Node2.Zone!=null).Select(m=>m.Node2.Zone).Distinct().ToList<Zone>();
                // get node1 zones
                var node1Zones = branches.Where( m=>m.Node2.Code.Substring(0,4)==locCode && m.Node1.Zone!=null).Select(m=>m.Node1.Zone).Distinct().ToList<Zone>();
                nodeZones.AddRange(node1Zones);
                //
                var nodeZoneCodes = nodeZones.Select(m=>m.Code).Distinct().ToList();
                //                
                if ( nodeZoneCodes.Count==1) {
                    var nodesToSet = nodesNoZones.Where(m=>m.Code.Substring(0,4)==locCode).ToList();
                    foreach( var node in nodesToSet) {
                        result = true;
                        node.Zone = nodeZones[0];
                        Logger.Instance.LogInfoEvent($"Node=[{node.Code}] [{node.Zone.Code}]");
                    }
                } else if ( nodeZones.Count>1) {
                    if ( knownZonesDict.ContainsKey(locCode) ) {
                        var zone = zones.Where(m=>m.Code==knownZonesDict[locCode]).FirstOrDefault();
                        if ( zone!=null) {
                            var nodesToSet = nodesNoZones.Where(m=>m.Code.Substring(0,4)==locCode).ToList();
                            foreach( var node in nodesToSet ) {
                                result = true;
                                node.Zone = zone;
                                Logger.Instance.LogInfoEvent($"Node=[{node.Code}] [{node.Zone.Code}]");
                            }
                        }
                    } else {
                        Logger.Instance.LogInfoEvent($"Node=[{locCode}] count=[{nodeZones.Count}]");
                        foreach( var z in nodeZones) {
                            Logger.Instance.LogInfoEvent($"z=[{z.Code}],[{z.Dataset.Name}]");
                        }
                    }
                }
            }
            //
            da.CommitChanges();
        }
        return result;
    }

    private void updateNodeLocations(Dictionary<string,SubstationCode> codesDict) {
        using( var da = new DataAccess() ) {
            var dataset = getDataset(da,BASE_YEAR);
            var nodes = da.Loadflow.GetNodes(dataset);
            var branches = da.Loadflow.GetBranches(dataset);
            var gridSubstationLocations = da.NationalGrid.GetGridSubstationLocations(dataset);

            var blackList=new string[] {"SANX","GART","FENW","CHAS","CLYN","BEIW","GLGL","WHHO","LOCL","TKNW","TKNO","ORMO","ORMW"};
            var codeAliases = new Dictionary<string,string>() {
                {"COWT","COWB"},
                {"NORW","NORM"},
                {"BEDT","BEDD"},
                {"STYC","STAY"}
            };
            var knownLocations = new Dictionary<string,double[]>() {
                {"CREA",new double[] {58.21040,-4.50244}}, // CREAG RIABHACH WINDFARM
                {"MILW",new double[] {57.12366,-4.84634}}  // MILLENIUM WIND
            };

            //
            int nFound=0;
            var notFoundDict = new Dictionary<string,bool>();
            var notFoundCodesDict = new Dictionary<SubstationCode,IList<Node>>();
            foreach( var node in nodes) {
                // Lookup grid locations based on first 4 chars of code
                var locCode = node.Code.Substring(0,4);
                // these are nodes at other end of inter connectors
                if ( node.Ext ) {
                    locCode+="X";
                }

                // see if we need to use an alias to lookup the code
                if ( codeAliases.ContainsKey(locCode)) {
                    locCode = codeAliases[locCode];
                }
                // see if the location exists
                var loc = gridSubstationLocations.Where(m=>m.Reference==locCode).FirstOrDefault();
                if ( loc==null)  {
                    // check we havn't checked before and the blacklist doesn't contain it
                    /*if ( codesDict.ContainsKey(locCode) && !notFoundDict.ContainsKey(locCode) && !blackList.Contains(locCode) ) {
                        var substationCode = codesDict[locCode];
                        loc = googleMapsLookup(da, substationCode);
                        if ( loc == null ) {
                            //??Logger.Instance.LogWarningEvent($"Could not find location for node [{locCode}] [{substationCode.Name}]");
                            notFoundDict.Add(locCode,true);
                        } else {
                            gridSubstationLocations.Add(loc);
                            nFound++;
                        }
                    } 
                    */
                    if ( !node.Ext && codesDict.ContainsKey(locCode)) {
                        var sc = codesDict[locCode];
                        if ( sc.Owner == SubstationOwner.NGET || sc.Owner == SubstationOwner.SHET) {
                            if ( !notFoundCodesDict.ContainsKey(sc) ) {
                                notFoundCodesDict.Add(sc,new List<Node>());
                            }
                            notFoundCodesDict[sc].Add(node);
                        }
                    } else {
                        Logger.Instance.LogInfoEvent($"Cannot find location code in ETYS appendixB [{node.Code}] [{node.Name}]");
                    }
                } else {
                    nFound++;
                }
                if ( loc!=null ) {
                    node.Location = loc;
                }
            }

            Logger.Instance.LogInfoEvent($"Found [{nFound}] locations for [{nodes.Count}] nodes with missing locations");

            foreach( var nf in notFoundCodesDict ) {
                var sc = nf.Key;
                GridSubstationLocation newLoc = null;
                if ( knownLocations.ContainsKey(sc.Code)) {
                    newLoc = GridSubstationLocation.Create(sc.Code,GridSubstationLocationSource.Estimated, dataset);
                    newLoc.Name = sc.Name;
                    newLoc.GISData.Latitude = knownLocations[sc.Code][0];
                    newLoc.GISData.Longitude = knownLocations[sc.Code][1];
                    da.NationalGrid.Add(newLoc);
                    Logger.Instance.LogInfoEvent($"Added known location for code [{sc.Code}] [{sc.Name}]");
                } else {
                    newLoc = addEstimatedLocation(sc, branches, gridSubstationLocations, dataset);
                    if ( newLoc!=null ) {
                        da.NationalGrid.Add(newLoc);
                        Logger.Instance.LogInfoEvent($"Added estimated location for code [{sc.Code}] [{sc.Name}]");
                    } else {
                        Logger.Instance.LogInfoEvent($"Could not find location for code [{sc.Code}]");
                    }
                }
            }

            // update links
            da.CommitChanges();                        
        }
    }

    private GridSubstationLocation addEstimatedLocation(SubstationCode sc,IList<Branch> branches, IList<GridSubstationLocation> gridSubstationLocations, Dataset dataset) {
        var code = sc.Code;
        var name = sc.Name;
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

    private GridSubstationLocation googleMapsLookup(DataAccess da, SubstationCode substationCode, Dataset dataset) {

        // append substation to get google maps to search for substation site
        var substationLookup = substationCode.Name;
        // add substation if not there and not the offshore entries
        if ( substationCode.Owner != SubstationOwner.OFTO && !substationLookup.EndsWith("substation",StringComparison.OrdinalIgnoreCase)) {
            substationLookup+=" substation";
        }
        // add scotland if located in scotland
        if ( substationCode.Owner == SubstationOwner.SHET || substationCode.Owner == SubstationOwner.SPT) {
            substationLookup+=", scotland";
        } else if (substationCode.Owner == SubstationOwner.NGET) {
            substationLookup+=", england, wales";
        }
        TextSearch textSearch;
        //
        try {
            textSearch = _gisFinder.TextSearchNew(substationLookup);
        } catch( Exception e) {
            Logger.Instance.LogErrorEvent(e.Message);
            return null;
        }
        if ( textSearch?.places?.Count>0) {            
            var placeName = textSearch.places[0].displayName.text;
            if ( isLocInPlaceName(substationCode.Name,placeName) ) {
                var loc = GridSubstationLocation.Create(substationCode.Code, GridSubstationLocationSource.GoogleMaps, dataset);
                loc.Name = substationCode.Name;
                loc.GISData.Latitude = textSearch.places[0].location.latitude;
                loc.GISData.Longitude = textSearch.places[0].location.longitude;
                da.NationalGrid.Add(loc);
                Logger.Instance.LogInfoEvent($"Found location for substation [{substationCode.Code}] [{substationCode.Name}] [{placeName}]");
                return loc;
            } else {
                Logger.Instance.LogInfoEvent($"Could not find location in place name [{substationCode.Name}] [{placeName}]");
                return null;
            }
        } else {
            return null;
        }

    }

    private bool isLocInPlaceName(string name, string placeName) {
        // just see if the first word is anywhere in the place name
        var cpnts = name.Split(" ");
        return placeName.Contains(cpnts[0],StringComparison.OrdinalIgnoreCase);
    }

    private void checkNodeNames(string fnB, string fnG) {
        // Appendix G
        // get list of node demands 
        var nodeDemands = loadNodeDemands(fnG);
        // extract node names
        var gNodeNames = nodeDemands.Select(m=>m.Node).ToList();

        // Appendix B
        var transCircuits = loadHighVoltageCircuits(fnB);
        var bNodeNames = getNodeNames(transCircuits);

        // compare
        var numNodes = gNodeNames.Count;
        var found = 0;
        var found4 = 0;
        foreach( var node in gNodeNames) {
            var node4 = node.Substring(0,4);
            if ( bNodeNames.Any(m=>string.Compare(m,node,true)==0) ) {
                found++;
            } else {
                var find4 = bNodeNames.FindAll(m=>string.Compare(m.Substring(0,4),node4,true)==0);
                if ( find4.Count>0 ) {
                    found4++;
                } else {
                    Logger.Instance.LogInfoEvent($"Appendix G node not found in appendix B [{node}]");
                }
            }
        }

        Logger.Instance.LogInfoEvent($"Num nodes in appendix G={numNodes}, found whole in appendix B={found}, found 4-char={found4}");
    }

    private void loadAppendixG(string fn) {
        var nodeDemands = loadNodeDemands(fn);
        //??checkNodeDemands(nodeDemands);
        assignNodeDemands(nodeDemands);

    }

    private void checkNodeDemands(List<NodeDemand> nodeDemands) {
        using( var da = new DataAccess()) {
            var ds = getDataset(da,2023);
            var ssDs = getSpreadsheetDataset(da);
            //
            var nodes = da.Loadflow.GetNodes(ds);
            var ssNodes = da.Loadflow.GetNodes(ssDs);
            //
            int numNotFound = 0;
            int numNotFoundSS = 0;
            foreach( var nodeDemand in nodeDemands ) {
                var node = nodes.Where(m=>m.Code == nodeDemand.Node).FirstOrDefault();
                if ( node==null ) {
                    numNotFound++;
                }
                node = ssNodes.Where(m=>m.Code == nodeDemand.Node).FirstOrDefault();
                if ( node==null ) {
                    numNotFoundSS++;
                }
            }
            //
            Logger.Instance.LogInfoEvent($"Percentage found 2022        [{100*(nodeDemands.Count-numNotFound)/nodeDemands.Count:F0}]");
            Logger.Instance.LogInfoEvent($"Percentage found spreadsheet [{100*(nodeDemands.Count-numNotFoundSS)/nodeDemands.Count:F0}]");
        }
    }

    private void assignNodeDemands(List<NodeDemand> nodeDemands) {
        using( var da = new DataAccess()) {
            var ds = getDataset(da,2023);
            //
            var nodes = da.Loadflow.GetNodes(ds);
            var branches = da.Loadflow.GetBranches(ds);
            // explicitly set demands to 0 since sometimes these need adding
            foreach( var node in nodes) {
                node.Demand = 0;
            }
            //
            int numNotFound = 0;
            foreach( var nodeDemand in nodeDemands ) {
                var dNodes = findDemandNodes(nodeDemand.Node,nodes,branches);
                if ( dNodes.Count==0 ) {
                    numNotFound++;
                    Logger.Instance.LogInfoEvent($"Cannot find any nodes to assign nodeDemand [{nodeDemand.Node}]");
                    continue;
                } else if ( dNodes.Count>1 ) {
                    //??Logger.Instance.LogInfoEvent($"Multiple nodes found to assign nodeDemand [{nodeDemand.Node}], dividing demand , count=[{dNodes.Count}]");
                }
                // if multiple nodes to assign demand then add equally
                var demand = nodeDemand.DemandDict[2023]/((double) dNodes.Count);
                foreach( var dn in dNodes) {
                    //??Logger.Instance.LogInfoEvent($"Assigning demand for [{nodeDemand.Node}] to [{dn.Code}]");
                    dn.Demand += demand;
                }
            }
            //
            da.CommitChanges();
            //
            Logger.Instance.LogInfoEvent($"Percentage assigned 2022        [{100*(nodeDemands.Count-numNotFound)/nodeDemands.Count:F0}]");
        }
    }

    private IList<Node> findDemandNodes(string name, IList<Node> nodes, IList<Branch> branches) {
        // look for node matching first 4 chars plus voltage index
        if ( name.Length<5 ) {
            Logger.Instance.LogInfoEvent($"Name not long enough [{name}]");
            return new List<Node>();
        } else {
            var shortName = name.Substring(0, 5);
            var ns = nodes.Where(m => m.Code.StartsWith(shortName)).ToList();
            return ns;
        }
    }

    private IList<Node> _findDemandNodes(string name, IList<Node> nodes, IList<Branch> branches) {
        var lengths = new int[] {name.Length,6,5,4};
        var dNodesDict = new Dictionary<Node,Boolean>();
        foreach (var len in lengths)
        {
            if (name.Length >= len)
            {
                var shortName = name.Substring(0, len);
                var ns = nodes.Where(m => m.Code.StartsWith(shortName)).ToList();
                foreach (var dNode in ns)
                {
                    addGSPNodes(dNode, dNodesDict, branches);
                }
                if ( dNodesDict.Count > 0 ) {
                    break;
                }
            }
        }
        if ( dNodesDict.Count==0 ) {
            foreach (var len in lengths)
            {
                if (name.Length >= len)
                {
                    var shortName = name.Substring(0, len);
                    var ns = nodes.Where(m => m.Code.StartsWith(shortName)).ToList();
                    if ( ns.Count>0 ) {
                        dNodesDict.Add(ns[0],true);
                        break;
                    }
                }
            }
        }
        return dNodesDict.Keys.ToList();
    }

    private void addGSPNodes(Node node, Dictionary<Node,Boolean> dNodesDict, IList<Branch> branches) {
        var nodes = branches.Where(m=>m.Node1.Code==node.Code  && m.LinkType == "Transformer").Select(m=>m.Node2).ToList();
        foreach( var n in nodes) {
            if ( !dNodesDict.ContainsKey(n) ) {
                dNodesDict.Add(n,true);
            }
        }
    }

    private List<NodeDemand> loadNodeDemands(string fn) {
        var nodeDemands = new List<NodeDemand>();
        using (var stream = new FileStream(fn,FileMode.Open)) {
            using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                // there is a hidden sheet called "demand data 2015" so need to skip this
                gotoSheet(reader,"demand data 2022");
                //
                int headerRowCount = 10;
                for( int i=0;i<headerRowCount;i++) {
                    reader.Read();
                }
                var years = new List<int>() { 2023, 2024, 2025, 2026, 2027, 2028, 2029, 2030 };
                //
                while( reader.Read() ) {
                    // Lots of empty lines at the end so need to break when they are found
                    if ( reader.GetString(0)==null ) {
                        break;
                    }
                    nodeDemands.Add(new NodeDemand(reader, years));
                }
            }
        }
        return nodeDemands;
    }

    private string saveAppendixB() {        

        var client = getHttpClient();
        string fn;
        
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, APPENDIX_B_URL)) {
                //
            var response = client.SendAsync(message).Result;
            //
            if ( response.IsSuccessStatusCode) {
                var stream = response.Content.ReadAsStream();
                var cd = response.Content.Headers.ContentDisposition;                    
                if ( stream!=null && cd!=null && cd.FileName!=null) {
                    // filename is surrounded in quotes so these need removing
                    var cleanFn = cd.FileName.Replace("\"","");
                    fn = Path.Combine(AppFolders.Instance.Temp,cleanFn);
                    saveToFile(stream, fn);
                } else {
                    throw new Exception($"Unexpected problem loading Appendix B from ETYS website, url=[{APPENDIX_B_URL}]");
                }
            } else {
                throw new Exception($"Problem loading Appendix B from ETYS website [{response.StatusCode}], url=[{APPENDIX_B_URL}]");
            }
        }
        //
        return fn;
    }

    private string saveAppendix(string url) {        

        var client = getHttpClient();
        string fn;
        
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, url)) {
                //
            var response = client.SendAsync(message).Result;
            //
            if ( response.IsSuccessStatusCode) {
                var stream = response.Content.ReadAsStream();
                var cd = response.Content.Headers.ContentDisposition;                    
                if ( stream!=null && cd!=null && cd.FileName!=null) {
                    // filename is surrounded in quotes so these need removing
                    var cleanFn = cd.FileName.Replace("\"","");
                    fn = Path.Combine(AppFolders.Instance.Temp,cleanFn);
                    saveToFile(stream, fn);
                } else {
                    throw new Exception($"Unexpected problem loading file from ETYS website, url=[{url}]");
                }
            } else {
                throw new Exception($"Problem loading file from ETYS website [{response.StatusCode}], url=[{url}]");
            }
        }
        //
        return fn;
    }

    private void loadAppendixB(string fn, out Dictionary<string,SubstationCode> codesDict) {
        codesDict = loadSubstationCodes(fn);
        var transCircuits = loadHighVoltageCircuits(fn);        
        var nodeNames = getNodeNames(transCircuits);
        //
        var networks = checkNetwork(transCircuits);
        if ( networks.Count > 1) {
            Logger.Instance.LogErrorEvent("Multiple isolated networks found");
            for( int i=1;i<networks.Count;i++) {
                string msg="Network: ";
                foreach( var n in networks[i]) {
                    msg+=n;
                    var cir = transCircuits.Where(m=>m.Node1==n || m.Node2==n).FirstOrDefault();
                    if ( cir!=null) {
                        msg+=$" ({cir.Owner})";
                    }
                    if ( n!=networks[i].Last() ) {
                        msg+=", ";
                    }
                }
                Logger.Instance.LogErrorEvent(msg);
            }
            //
            //??throw new Exception("Multiple isolated networks found");
        }
        //
        updateNodes(nodeNames);
        updateBranches(transCircuits);
        updateCtrls(transCircuits);
    }

    private List<List<string>> checkNetwork(List<Circuit> transCircuits) {
        var checker = new NetworkChecker(transCircuits);
        var result = checker.Check();
        return result;
    }

    private class NetworkChecker {

        private Dictionary<string,List<string>> _nodeDict;

        public NetworkChecker(List<Circuit> circuits) {
            _nodeDict = new Dictionary<string,List<string>>();
            foreach( var c in circuits) {
                // Node 1
                if ( !_nodeDict.ContainsKey(c.Node1)) {
                    _nodeDict.Add(c.Node1,new List<string>());
                }
                _nodeDict[c.Node1].Add(c.Node2);
                // Node 2
                if ( !_nodeDict.ContainsKey(c.Node2)) {
                    _nodeDict.Add(c.Node2,new List<string>());
                }
                _nodeDict[c.Node2].Add(c.Node1);
            }
        }

        public List<List<string>> Check() {
            // create a dictionary to store visits to nodes
            var nodeVisitDict = new Dictionary<string,bool>();
            foreach( var n in _nodeDict.Keys) {
                nodeVisitDict.Add(n,false);
            }
            //
            var separateNetworks = new List<List<string>>();
            //
            var cont = true;
            while( cont ) {
                var ns = nodeVisitDict.Keys.FirstOrDefault();
                if ( ns!=null) {
                    visitNode(ns, nodeVisitDict);
                }
                //
                var networkNodes = nodeVisitDict.Where(m=>m.Value).Select(m=>m.Key).ToList();
                foreach (var n in networkNodes) {
                    nodeVisitDict.Remove(n);
                }
                separateNetworks.Add(networkNodes);
                //
                cont = nodeVisitDict.Where(m=>!m.Value).Count()>0;
            }
            // Order list of networks be descending size
            separateNetworks = separateNetworks.OrderByDescending(m=>m.Count).ToList();
            //
            return separateNetworks;
        }

        private void visitNode(string node, Dictionary<string,bool> nodeVisitDict) {
            nodeVisitDict[node] = true;
            foreach( var n in _nodeDict[node]) {
                if ( !nodeVisitDict[n] ) {
                    visitNode(n,nodeVisitDict);
                }
            }
        }

    }

    public Dictionary<string,SubstationCode> LoadSubstationCodes() {
        string fnB = saveAppendix(APPENDIX_B_URL);
        var codesDict = loadSubstationCodes(fnB);
        return codesDict;
    }

    public enum SubstationOwner {SHET,SPT,NGET,OFTO}
    public class SubstationCode {
        public SubstationCode( string code, string name, SubstationOwner owner) {
            Code = code;
            Name = name;
            Owner = owner;
        }
        public string Code {get; set;}
        public string Name {get; set;}
        public SubstationOwner Owner {get; set;}
    }

    private Dictionary<string,SubstationCode> loadSubstationCodes(string fn) {
        var codesDict = new Dictionary<string,SubstationCode>();
        using (var stream = new FileStream(fn,FileMode.Open)) {
            using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                // transmission circuits
                gotoSheet(reader,"B-1-1a");
                loadCodesDict(codesDict, reader,SubstationOwner.SHET);
                gotoSheet(reader,"B-1-1b");
                loadCodesDict(codesDict, reader,SubstationOwner.SPT);
                gotoSheet(reader,"B-1-1c");
                loadCodesDict(codesDict, reader,SubstationOwner.NGET);
                gotoSheet(reader,"B-1-1d");
                loadCodesDict(codesDict, reader,SubstationOwner.OFTO);
            }
        }
        return codesDict;
    }

    private void loadCodesDict(Dictionary<string,SubstationCode> codesDict, IExcelDataReader reader, SubstationOwner owner) {
        // Skip first 2 rows
        reader.Read();
        reader.Read();
        //
        while( reader.Read() ) {
            var code = reader.GetString(0);
            if ( code == null ) {
                break;
            }
            var name = reader.GetString(1);
            if ( !codesDict.ContainsKey(code)) {
                codesDict.Add(code,new SubstationCode(code,name,owner));
            }
        }
    }

    private void updateCtrls(List<Circuit> transCircuits) {
        using( var da = new DataAccess() ) {

            // Add Quad boosters
            addQuadBoosters(da);

            // Add HVDC controlss
            addHVDCCtrls(da);
            //
            da.CommitChanges();
        }
    }

    private void addHVDCCtrls(DataAccess da) {
        // get original ctrls and branches from spreadsheet
        var ssDataset = getSpreadsheetDataset(da);
        var ssCtrls = da.Loadflow.GetCtrls(ssDataset);
        var ssBranches = da.Loadflow.GetBranches(ssDataset);

        // get current ones
        var dataset = getDataset(da, BASE_YEAR);
        var existingBranches = da.Loadflow.GetBranches(dataset);
        var existingCtrls = da.Loadflow.GetCtrls(dataset);
        var existingNodes = da.Loadflow.GetNodes(dataset);
        var existingZones = da.Loadflow.GetZones(dataset);

        // HVDC controls
        var ssCtrlsHVDC = ssCtrls.Where( m=>m.Type == LoadflowCtrlType.HVDC).ToList();
        foreach ( var ssC in ssCtrlsHVDC ) {
            // associated branch
            var ssB = ssBranches.Where( m=>m.LineName == ssC.LineName).FirstOrDefault();
            if ( ssB!=null ) {
                var ctrl = existingCtrls.Where(m=>m.Code==ssC.Code).FirstOrDefault();
                var branch = existingBranches.Where(m=>m.Code==ssB.Code).FirstOrDefault();
                Node node1=null,node2=null;
                if ( ctrl ==null ) {
                    node1  = getCtrlNode(da,existingNodes,existingZones,ssC.Node1,dataset);
                    if ( node1==null ) {
                        Logger.Instance.LogInfoEvent($"Could not find node [{ssC.Node1.Code}]");
                        continue;
                    }
                    node2  = getCtrlNode(da,existingNodes,existingZones,ssC.Node2,dataset);
                    if ( node2==null) {
                        Logger.Instance.LogInfoEvent($"Could not find node [{ssC.Node2.Code}]");
                        continue;
                    }
                    if ( branch == null ) {
                        branch = new Branch() {
                            Node1 = node1,
                            Node2 = node2,
                            Code = ssB.Code,
                            LinkType = ssB.LinkType,
                            B = ssB.B,
                            X = ssB.X,
                            Cap = ssB.Cap,
                            CableLength = ssB.CableLength,
                            OHL = ssB.OHL,                            
                            Dataset = dataset,
                        };
                        da.Loadflow.Add(branch);
                        Logger.Instance.LogInfoEvent($"Adding branch [{branch.LineName}]");
                    }
                    ctrl = new Ctrl() {
                        Branch = branch,
                        Type = ssC.Type,
                        MinCtrl = ssC.MinCtrl,
                        MaxCtrl = ssC.MaxCtrl,
                        Cost = ssC.Cost,
                        Dataset = dataset,
                    };
                    da.Loadflow.Add(ctrl);
                    Logger.Instance.LogInfoEvent($"Adding Ctrl [{ctrl.LineName}]");
                }
            } else {
                Logger.Instance.LogInfoEvent($"Cannot find branch for Ctrl [{ssC.LineName}]");
            }
        }
    }

    private Node getCtrlNode(DataAccess da, IList<Node> existingNodes,  IList<Zone> existingZones, Node n, Dataset  ds) {
        Node node;
        // Try full name
        node = existingNodes.Where( m=>m.Code == n.Code).FirstOrDefault();
        if ( node!=null ) {
            return node;
        }
        // if is a dummy one to support HVDC links then create
        if ( n.Code[5] == 'X') {
            node = new Node() {
                Code = n.Code,
                Dataset = ds,
                Voltage = n.Voltage,
                Ext = n.Ext,
                Zone = existingZones.Where(m=>m.Code == n.Zone.Code).FirstOrDefault()
            };
            da.Loadflow.Add(node);
            existingNodes.Add(node);
            Logger.Instance.LogInfoEvent($"Adding HVDC node [{node.Code}]");
            return node;
        }
        // Just first 5 chars (location code + voltage)
        node = existingNodes.Where(m=>m.Code.Substring(0,5) == n.Code.Substring(0,5)).FirstOrDefault();
        return node;
    }

    private void addQuadBoosters(DataAccess da) {
        // get current ones
        var dataset = getDataset(da, BASE_YEAR);
        var existingBranches = da.Loadflow.GetBranches(dataset);
        // this is a transformer between 2 nodes that have the same voltage
        var ctrlBranches=existingBranches.Where( m=>
            m.Node1.Code.Substring(0,5)==m.Node2.Code.Substring(0,5) && 
            m.LinkType == "Transformer");
        foreach( var b in ctrlBranches) {
            //??Logger.Instance.LogInfoEvent($"QB Ctrl branch?=[{b.Node1.Code}] [{b.Node2.Code}]");
        }
        //
        var existingCtrls = da.Loadflow.GetCtrls(dataset);
        existingCtrls = existingCtrls.Where(m=>m.Type==LoadflowCtrlType.QB).ToList();
        // add any that do not exist
        int index = 1;
        foreach( var b in ctrlBranches) {
            var ctrls = existingCtrls.Where( 
                m=>m.Branch.Id == b.Id).ToList();
            if ( ctrls.Count==0 )  {
                var voltage=b.Node1.Code[4];
                // Make the codes the same so we can locate the branch
                b.Code = $"Q{index++}";
                var ctrl = new Ctrl() {
                    Branch = b,
                    //
                    MinCtrl = (voltage == '4') ? -0.2 : -0.15,
                    MaxCtrl = (voltage == '4') ?  0.2 :  0.15,
                    //
                    Type = LoadflowCtrlType.QB,                        
                    Cost = 10.0,
                    //
                    Dataset = dataset,
                };
                //
                da.Loadflow.Add(ctrl);
                Logger.Instance.LogInfoEvent($"Adding ctrl=[{ctrl.Code} [{ctrl.Node1.Code}] [{ctrl.Node2.Code}]");
            }
        }

        // remove any that had been created but now should not exists
        foreach( var c in existingCtrls) {
            var branches = ctrlBranches.Where( 
                m=>(m.Node1.Code == c.Node1.Code) &&
                   (m.Node2.Code == c.Node2.Code)).ToList();
            if ( branches.Count==0) {
                da.Loadflow.Delete(c);
                Logger.Instance.LogInfoEvent($"Removing ctrl=[{c.LineName}] ??");
            }
        }
    }

    private void repairNodes() {
        using ( var da = new DataAccess() ) {
            var ssDataset = getSpreadsheetDataset(da);
            var nodes = da.Loadflow.GetNodes(ssDataset);
            var zones = da.Loadflow.GetZones(ssDataset);
            foreach( var node in nodes ) {
                var zone = zones.Where(m=>m.Code == node.Zone.Code).Take(1).FirstOrDefault();
                node.Zone = zone;
            }
            da.CommitChanges();
        }
    }

    private void updateNodes(List<string> nodeNames) {
        using ( var da = new DataAccess() ) {
            //
            var ssDataset = getSpreadsheetDataset(da);
            var dataset = getDataset(da, BASE_YEAR);
            //
            var existingNodes = da.Loadflow.GetNodes(dataset);
            var ssNodes = da.Loadflow.GetNodes(ssDataset);
            var zones = da.Loadflow.GetZones(dataset);
            if ( zones.Count==0 ) {
                zones = copyBoundariesAndZones(da,ssDataset,dataset);
            }
            //
            int numAdded=0;
            var nodeDict = new Dictionary<string,bool>();
            foreach( var nodeName in nodeNames) {
                var existingNode = existingNodes.Where( m=>m.Code == nodeName).Take(1).FirstOrDefault();
                if ( existingNode==null ) {
                    existingNode = new Node() {
                        Code = nodeName,
                        Dataset = dataset
                    };
                    existingNode.SetVoltage();
                    existingNode.SetLocation(da);
                    da.Loadflow.Add(existingNode);
                    numAdded++;
                }
                //
                if ( existingNode.Zone==null ) {
                    if ( !setZone(existingNode,zones,ssNodes) ) {
                        var code = existingNode.Code.Substring(0,4);
                        if ( !nodeDict.ContainsKey(code) ) {
                            nodeDict.Add(code,true);
                        }
                    }
                }
            }
            //
            da.CommitChanges();
            Logger.Instance.LogInfoEvent($"Added [{numAdded}] nodes for dataset [{dataset.Name}]");
            Logger.Instance.LogInfoEvent($"Could not find zones for [{nodeDict.Count}] node codes");
        }

    }

    private bool setZone(Node node, IList<Zone> zones, IList<Node> ssNodes) {
        var code = node.Code.Substring(0,4);
        var ssNode = ssNodes.Where(m=>m.Code.StartsWith(code)).Take(1).FirstOrDefault();
        if ( ssNode==null ) {
            return false;
        } else {
            var ssZone = ssNode.Zone;
            var zone = zones.Where(m=>m.Code == ssZone.Code).Take(1).FirstOrDefault();
            node.Zone = zone;
            return true;
        }
    }

    private IList<Zone> copyBoundariesAndZones(DataAccess da, Dataset ds, Dataset dataset) {
        // copy over zones from original loadflow dataset to new one
        var zones = da.Loadflow.GetZones(ds);
        var newZones = new List<Zone>();
        foreach( var zone in zones ) {
            var nz = new Zone() {
                Code = zone.Code,
                Dataset = dataset
            };
            da.Loadflow.Add(nz);
            newZones.Add(nz);
        }
        var boundaries = da.Loadflow.GetBoundaries(ds);
        var newBoundaries = new List<Data.Boundary>();
        foreach( var b in boundaries ) {
            var nb = new Data.Boundary() {
                Code = b.Code,
                Dataset = dataset
            };
            da.Loadflow.Add(nb);
            newBoundaries.Add(nb);
        }
        // and boundary zones
        var boundaryZones = da.Loadflow.GetBoundaryZones(ds);
        var newBoundaryZones = new List<BoundaryZone>();
        foreach( var bz in boundaryZones ) {
            var nb = newBoundaries.Where( m=>m.Code == bz.Boundary.Code).FirstOrDefault();
            var nz = newZones.Where( m=>m.Code == bz.Zone.Code).FirstOrDefault();
            var nbz = new BoundaryZone() {
                Boundary = nb,
                Zone = nz,
                Dataset = dataset
            };
            da.Loadflow.Add(nbz);
        }
        return newZones;
    }

    private Dataset getDataset(DataAccess da, int year) {
        var ending = _loadOptions == LoadOptions.All ? " (all circuits)" : "";
        var datasetName = $"{DATASET_BASE_NAME} {year}{ending}";
        var dataset = da.Datasets.GetDataset(DatasetType.Loadflow,datasetName);
        if ( dataset==null) {
            var root = da.Datasets.GetRootDataset(DatasetType.Loadflow);
            dataset = new Dataset() { Type = DatasetType.Loadflow, Parent = root, Name = datasetName };                
            da.Datasets.Add(dataset);
        }
        return dataset;
    }

    private Dataset getSpreadsheetDataset(DataAccess da) {
        var name = "GB network";
        var ds = da.Datasets.GetDataset(DatasetType.Loadflow,name);
        if ( ds==null) {
            throw new Exception($"Cannot find dataset [{name}]");
        }
        return ds;
    }


    private void updateBranches(List<Circuit> circuits) {
        using( var da = new DataAccess() ) {
            var dataset = getDataset(da, BASE_YEAR);
            var existingNodes = da.Loadflow.GetNodes(dataset);
            var existingBranches = da.Loadflow.GetBranches(dataset);
            //
            int numAdded = 0;
            int numIgnored = 0;
            foreach( var circuit in circuits) {
                var existingBranch = existingBranches.Where( m=>m.Node1.Code == circuit.Node1 && m.Node2.Code == circuit.Node2).Take(1).FirstOrDefault();
                if ( existingBranch == null ) {
                    var existingNode1 = existingNodes.Where( m=>m.Code ==circuit.Node1).Take(1).FirstOrDefault();
                    if ( existingNode1==null) {
                        Logger.Instance.LogInfoEvent($"Cannot find node1 [{circuit.Node1}]");
                        numIgnored++;
                        continue;
                    }
                    var existingNode2 = existingNodes.Where( m=>m.Code ==circuit.Node2).Take(1).FirstOrDefault();
                    if ( existingNode2==null) {
                        Logger.Instance.LogInfoEvent($"Cannot find node2 [{circuit.Node2}]");
                        numIgnored++;
                        continue;
                    }
                    var branch = new Branch() {
                         Node1 = existingNode1,
                         Node2 = existingNode2,
                         Dataset = dataset,
                         LinkType = circuit.CircuitType,
                         R = circuit.R,
                         X = circuit.X,
                         B = circuit.B,
                         OHL = circuit.OhlLength,
                         CableLength = circuit.CableLength,
                         Cap = circuit.Rating,
                         WinterCap = circuit.WinterRating,
                         SpringCap = circuit.SpringRating,
                         SummerCap = circuit.SummerRating,
                         AutumnCap = circuit.AutumnRating,
                    };
                    da.Loadflow.Add(branch);
                    numAdded++;
                }
            }
            da.CommitChanges();
            Logger.Instance.LogInfoEvent($"Added [{numAdded}] branches for dataset [{dataset.Name}]");
        }
    }

    private List<string> getNodeNames(List<Circuit> list) {
        var dict = new Dictionary<string,bool>();
        foreach( var l in list) {
            if ( !dict.ContainsKey(l.Node1)) {
                dict.Add(l.Node1,true);
            }
            if ( !dict.ContainsKey(l.Node2)) {
                dict.Add(l.Node2,true);
            }
        }
        return dict.Keys.ToList();
    }

    private List<Circuit> loadHighVoltageCircuits(string fn) {
        var transCircuits = new List<Circuit>();
        using (var stream = new FileStream(fn,FileMode.Open)) {
            using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                // transmission circuits
                gotoSheet(reader,"B-2-1a");
                loadCircuits(Circuit.TableType.TransmissionCircuit, reader, "SHET", transCircuits);
                gotoSheet(reader,"B-2-1b");
                loadCircuits(Circuit.TableType.TransmissionCircuit, reader, "SPT", transCircuits);
                gotoSheet(reader,"B-2-1c");
                loadCircuits(Circuit.TableType.TransmissionCircuit, reader, "NGET", transCircuits);
                gotoSheet(reader,"B-2-1d");
                loadCircuits(Circuit.TableType.OffshoreTransmissionCircuit, reader, "OFTO", transCircuits);
                // transformers
                gotoSheet(reader,"B-3-1a");
                loadCircuits(Circuit.TableType.Transformer, reader, "SHET", transCircuits);
                gotoSheet(reader,"B-3-1b");
                loadCircuits(Circuit.TableType.Transformer, reader, "SPT", transCircuits);
                gotoSheet(reader,"B-3-1c");
                loadCircuits(Circuit.TableType.Transformer, reader, "NGET", transCircuits);
                gotoSheet(reader,"B-3-1d");
                loadCircuits(Circuit.TableType.OffshoreTransformer, reader, "OFTO", transCircuits);
            }
        }
        return transCircuits;
    }

    private void loadCircuits(Circuit.TableType type, IExcelDataReader reader, string owner, List<Circuit> list) {
        // Skip first 2 rows
        reader.Read();
        reader.Read();
        //
        while( reader.Read() ) {
            var circuit = new Circuit(type, reader, owner);
            if ( addCircuit( circuit) ) {
                list.Add(circuit);
            }
        }
    }

    private bool addCircuit(Circuit circuit) {
        if ( _loadOptions==LoadOptions.All ) {
            return true;
        } else if ( _loadOptions==LoadOptions.OnlyHighVoltageCircuits && circuit.IsHighVoltage) {
            return true;
        } else {
            return false;
        }
    }

    private void gotoSheet(IExcelDataReader reader, string sheetName) {
        do {
            var name = reader.Name;
            //
            if ( name==sheetName) {
                return;
            }
        } while (reader.NextResult());
        //
        throw new Exception($"Could not find sheet [{sheetName}]");
    }
    
    private HttpClient getHttpClient()
    {
        if (_httpClient == null) {
            lock (_httpClientLock) {
                _httpClient = new HttpClient();
            }
        }
        //
        return _httpClient;
    }

    private void saveToFile(Stream stream, string filename) {

        var buffer = new byte[32768];
        using( var fs = new FileStream(filename,FileMode.Create)) {
            int nRead;
            while( (nRead = stream.Read(buffer, 0, buffer.Length)) !=0) {
                fs.Write(buffer,0,nRead);
            };
        }
    }

    private HttpRequestMessage getRequestMessage(HttpMethod httpMethod, string method, object data = null)
    {
        HttpRequestMessage message = new HttpRequestMessage(httpMethod, method);
        if (data != null) {
            string reqStr;
            if (data is string) {
                reqStr = (string)data;
            } else {
                reqStr = JsonSerializer.Serialize(data);
                message.Content = new StringContent(reqStr, Encoding.UTF8, "application/json");
            }
        }
        return message;
    }

    private class Circuit {
        public enum TableType { TransmissionCircuit, OffshoreTransmissionCircuit, Transformer, OffshoreTransformer}
        public Circuit( TableType type, IExcelDataReader reader, string owner=null) {
            if ( type == TableType.TransmissionCircuit) {
                int index=0;
                Node1 = reader.GetString(index++);
                Node2 = reader.GetString(index++);
                OhlLength = reader.GetDouble(index++);
                CableLength = reader.GetDouble(index++);
                CircuitType = reader.GetString(index++);
                R = reader.GetDouble(index++);
                X = reader.GetDouble(index++);
                B = reader.GetDouble(index++);
                WinterRating = reader.GetDouble(index++);
                SpringRating = reader.GetDouble(index++);
                SummerRating = reader.GetDouble(index++);
                AutumnRating = reader.GetDouble(index++);
            } else if ( type == TableType.OffshoreTransmissionCircuit) {
                int index=2;
                Node1 = reader.GetString(index++);
                Node2 = reader.GetString(index++);
                OhlLength = reader.GetDouble(index++);
                CableLength = reader.GetDouble(index++);
                CircuitType = reader.GetString(index++);
                R = reader.GetDouble(index++);
                X = reader.GetDouble(index++);
                B = reader.GetDouble(index++);
                Rating = reader.GetDouble(index++);
            } else if ( type == TableType.Transformer || type == TableType.OffshoreTransformer) {
                int index= type == TableType.Transformer ? 0 : 2;
                Node1 = reader.GetString(index++);
                Node2 = reader.GetString(index++);
                CircuitType = "Transformer";
                R = reader.GetDouble(index++);
                X = reader.GetDouble(index++);
                B = reader.GetDouble(index++);
                Rating = reader.GetDouble(index++);
            }
            Owner = owner;
        }
        public Circuit( IExcelDataReader reader) {
        }
        public string Node1 {get; set;}
        public string Node2 {get; set;}
        public double OhlLength {get; set;}
        public double CableLength {get; set;}
        public string CircuitType {get; set;}
        public double R {get; set;}
        public double X {get; set;}
        public double B {get; set;}
        public double Rating {get; set;}
        public double WinterRating {get; set;}
        public double SpringRating {get; set;}
        public double SummerRating {get; set;}
        public double AutumnRating {get; set;}
        public string Owner {get; set;}

        public bool IsHighVoltage {
            get {
                return isNodeHighVoltage(Node1) && isNodeHighVoltage(Node2);
            }
        }

        private static bool isNodeHighVoltage(string name) {
            var voltageIndex = name[4];
            var indeces = new char[]{'1','2','4'};
            return indeces.Contains(voltageIndex);
        }
    }

    private class NodeDemand {
        public NodeDemand(IExcelDataReader reader, List<int> years) {
            int index=0;
            Node = reader.GetString(index++);
            // eat space column
            index++;
            DemandDict = new Dictionary<int, double>();
            foreach( var year in years) {
                DemandDict.Add(year, reader.GetDouble(index));
                index++;
            }
        }

        public string Node {get; private set; }
        public Dictionary<int,double> DemandDict { get; private set;}
    }  

}
