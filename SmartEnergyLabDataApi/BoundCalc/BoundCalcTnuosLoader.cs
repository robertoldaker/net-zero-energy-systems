using System.Text.RegularExpressions;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using Microsoft.VisualBasic;
using NHibernate.Driver;
using NHibernate.Loader.Custom;
using NHibernate.Mapping.Attributes;
using Org.BouncyCastle.Crypto.Parameters;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.BoundCalc;

public class BoundCalcTnuosLoader {
    private DataAccess _da;
    private Dataset _dataset;

    private Regex _nameRegEx = new Regex(@"^External (\d\d\d\d)(\d\d)");

    private enum GridGroup {Ireland,Continent,Iceland}

    private class InterConnector {
        public InterConnector( string code, GridGroup group, int index, int extCode=0) {
            BranchCode1 = code;
            ExtCode = extCode;
            Group = group;
            Index = index;
        }
        public InterConnector( string code1, string code2, GridGroup group, int index, int extCode=0) {
            BranchCode1 = code1;
            BranchCode2 = code2;
            ExtCode = extCode;
            Group = group;
            Index = index;
        }
        public string BranchCode1 {get; set;}
        public string BranchCode2 {get; set;}
        public int ExtCode {get ;set;}
        public bool IsValid {get; set;}

        // These are used to know which other interconnector to connect to
        public GridGroup Group {get; set;} 
        public int Index {get; set;} 
    }    

    private class NodeData {
        public string Code {get; set;}
        public double Demand {get; set;}
        public double GenA {get; set;}
        public double GenB {get; set;}
        public string ZoneCode {get; set;}
        public int? Gen_Zone {get ;set;}
        public int? Dem_Zone {get; set;}
        public bool Used {get; set;}
    }

    // this holds node data read the spreadsheet - used when reading branches
    private Dictionary<string,NodeData> _nodeDataDict = new Dictionary<string, NodeData>();

    private Dictionary<string,InterConnector> _interConnectors = new Dictionary<string, InterConnector>() {
        // Island of Ireland
        {"Greenlink",new InterConnector("GreenLink",GridGroup.Ireland,10)},
        {"MARES",new InterConnector("MARES",GridGroup.Ireland,20)},
        {"East West Interconnector",new InterConnector("EWLink",GridGroup.Ireland,30)},        
        {"Auchencrosh (interconnector CCT)",new InterConnector("Moyle",GridGroup.Ireland,40)},
        {"LIRIC Interconnector",new InterConnector("LIRIC",GridGroup.Ireland,50)},

        // Continental europe
        {"FAB Link Interconnector",new InterConnector("FabLink",GridGroup.Continent,10)},
        {"IFA2 Interconnector",new InterConnector("IFA2",GridGroup.Continent,20)},
        {"Aquind Interconnector",new InterConnector("AQUIND",GridGroup.Continent,30)},
        {"IFA Interconnector",new InterConnector("IFA1",GridGroup.Continent,40)},
        {"ElecLink",new InterConnector("ElecLink",GridGroup.Continent,50)},
        {"Kulizumboo Interconnector",new InterConnector("Kulizumboo",GridGroup.Continent,60)},
        {"Gridlink Interconnector",new InterConnector("GridLink",GridGroup.Continent,70)},
        {"Nemo Link",new InterConnector("NemoLink",GridGroup.Continent,80)},
        {"Cronos",new InterConnector("CRONOS",GridGroup.Continent,90)},
        {"Britned",new InterConnector("BritNed",GridGroup.Continent,100)},
        {"Southernlink",new InterConnector("SouthernLink",GridGroup.Continent,110,3)},
        {"Lion (EuroLink)",new InterConnector("LionLink",GridGroup.Continent,120)},
        {"Nautilus",new InterConnector("Nautilus",GridGroup.Continent,130)},
        {"NeuConnect Interconnector",new InterConnector("NeuConnect",GridGroup.Continent,140,2)},
        {"Viking Link Denmark Interconnector",new InterConnector("VikingLink1","VikingLink2",GridGroup.Continent,150)},
        {"Continental Link",new InterConnector("ContinentalLink1","ContinentalLink2",GridGroup.Continent,160,2)},
        {"NS Link",new InterConnector("NSLA1","NSLA2",GridGroup.Continent,170)},
        {"NorthConnect",new InterConnector("NorthConnect",GridGroup.Continent,180)},

        // Iceland
        {"The Superconnection",new InterConnector("AtlanticSuperConnection",GridGroup.Iceland,10)},
    };

    private ObjectCache<Node> _nodeCache;
    private ObjectCache<Zone> _zoneCache;
    private ObjectCache<Branch> _branchCache;
    private ObjectCache<Ctrl> _ctrlCache;
    private ObjectCache<Boundary> _boundaryCache;
    private ObjectCache<BoundaryZone> _boundaryZoneCache;
    public BoundCalcTnuosLoader() {

    }

    public string Load(IFormFile formFile, int year) {
        string msg = loadDataFromSpreadsheet(formFile, year) + "\n";
        //?? Since we are loading all nodes now, this should not be required
        //??msg+=removeUnconnectedNetworks() + "\n";
        msg+=addCtrls() + "\n";
        msg+=addInterConnectorLinks() + "\n";
        // Also update locations
        var locUpdater = new BoundCalcLocationUpdater();
        msg += locUpdater.Update(_dataset.Id) + "\n";
        msg+=checkNetwork() + "\n";
        return msg;
    }

    private string checkNetwork() {
        string msg="";
        using( var da = new DataAccess() ) {
            var dataset = da.Datasets.GetDataset(_dataset.Id);
            var nodes = da.BoundCalc.GetNodes(dataset);
            var branches = da.BoundCalc.GetBranches(dataset);
            //
            var totalDemand = nodes.Sum( m=>m.Demand);
            var totalGenA = nodes.Sum( m=>m.Generation_A);
            var totalGenB = nodes.Sum( m=>m.Generation_B);
            if ( Math.Abs(totalDemand-totalGenA) > 1e-6) {
                msg += $"Total demand [{totalDemand}] does not equal total generation A [{totalGenA}]\n";
            }
            if ( Math.Abs(totalDemand-totalGenB) > 1e-6) {
                msg += $"Total demand [{totalDemand}] does not equal total generation B [{totalGenB}]\n";
            }
            //
            var networkChecker = new NetworkChecker(branches);
            var networks = networkChecker.Check();
            if ( networks.Count!=1) {
                msg = $"Unexpected detached network found, num networks={networks.Count}";
            }
            //
            da.CommitChanges();
        }
        return msg;
    }

    private string removeUnconnectedNetworks() {
        string msg="";
        using( var da = new DataAccess() ) {
            var dataset = da.Datasets.GetDataset(_dataset.Id);
            var nodes = da.BoundCalc.GetNodes(dataset);
            var branches = da.BoundCalc.GetBranches(dataset);
            //
            var networkChecker = new NetworkChecker(branches);
            var networks = networkChecker.Check();
            if ( networks.Count>1) {
                // these are ordered by descending count - if the first one has more than 90% of nodes remove the rest
                if ( networks[0].Count>0.9*nodes.Count ) {
                    msg += "Multiple networks found, deleting smaller detached networks\n";
                    for( int i=1;i<networks.Count;i++) {
                        (int numNodes, int numBranches) = removeNodes(da,networks[i], branches);
                        msg+=$"Removed {numNodes} nodes and {numBranches} branches from detached network";
                    }
                } else {
                    msg+="Detached networks found that could be removed";
                }
            } else {
                msg+="No detached networks found";
            }
            //
            networkChecker = new NetworkChecker(branches);
            networks = networkChecker.Check();
            if ( networks.Count!=1) {
                throw new Exception("Unexpected detached network found");
            }
            //
            da.CommitChanges();
        }
        return msg;
    }

    private (int,int) removeNodes(DataAccess da, List<Node> nodes, IList<Branch> branches) {
        int numNodes=0;
        int numBranches=0;
        foreach( var n in nodes) {
            var bs = branches.Where( m=>m.Node1 == n || m.Node2 == n).ToList();
            foreach( var b in bs) {
                da.BoundCalc.Delete(b);
                branches.Remove(b);
                numBranches++;
            }
            da.BoundCalc.Delete(n);
            numNodes++;
        }
        return (numNodes,numBranches);
    }

    private string loadDataFromSpreadsheet(IFormFile formFile, int year) {
        string msg = "";

        var name = getDatasetName(year,formFile.FileName);
        msg += deleteIfExists(name);
        using( _da = new DataAccess() ) {
            _dataset = _da.Datasets.GetDataset(DatasetType.BoundCalc,name);
            if ( _dataset==null) {
                var root = _da.Datasets.GetRootDataset(DatasetType.BoundCalc);
                _dataset = new Dataset() { Type = DatasetType.BoundCalc, Parent = root, Name = name };                
                _da.Datasets.Add(_dataset);
            } else {
                throw new Exception("Dataset unexpectedly already exists");
            }
            // Caches of existing objects
            var existingNodes = _da.BoundCalc.GetNodes(_dataset);
            _nodeCache = new ObjectCache<Node>(_da, existingNodes, m=>m.Code, (m,code)=>m.Code=code );
            //
            var existingZones = _da.BoundCalc.GetZones(_dataset);
            _zoneCache = new ObjectCache<Zone>(_da, existingZones, m=>m.Code, (m,code)=>m.Code=code );
            //
            var existingBranches = _da.BoundCalc.GetBranches(_dataset);
            _branchCache = new ObjectCache<Branch>(_da, existingBranches, m=>m.GetKey(), (m,key)=>m.SetCode(key) );
            //
            var existingCtrls = _da.BoundCalc.GetCtrls(_dataset);
            _ctrlCache = new ObjectCache<Ctrl>(_da, existingCtrls, m=>m.Code, (m,code)=>{} );

            var existingBoundaries = _da.BoundCalc.GetBoundaries(_dataset);
            _boundaryCache = new ObjectCache<Boundary>(_da, existingBoundaries, m=>m.Code, (m,code)=>m.Code=code );
            //
            var existingBoundaryZones = _da.BoundCalc.GetBoundaryZones(_dataset);
            _boundaryZoneCache = new ObjectCache<BoundaryZone>(_da, existingBoundaryZones, m=>$"{m.Boundary.Code}:{m.Zone.Code}", (m,key)=>{
                var cpnts = key.Split(':');
                var boundaryCode = cpnts[0];
                var zoneCode = cpnts[1];
                m.Boundary = _boundaryCache.GetOrCreate(boundaryCode, out bool created);
                if ( created ) {
                    m.Boundary.Dataset = _dataset;
                }
                m.Zone = _zoneCache.GetOrCreate(zoneCode, out created);
                if ( created ) {
                    m.Zone.Dataset = _dataset;
                }
            } );

            msg+=loadBoundaries(formFile) + "\n";
            loadNodes(formFile);
            msg+=loadBranches(formFile) + "\n";
            msg+=loadInterconnectors(formFile);
            //
            _da.CommitChanges();
        }
        return msg;
    }

    private string deleteIfExists(string name) {
        string msg = "";
        using( var da = new DataAccess() ) {
            var dataset = da.Datasets.GetDataset(DatasetType.BoundCalc,name);
            if ( dataset!=null) {
                da.Datasets.Delete(dataset);
                da.CommitChanges();            
                msg = $"Deleted existing dataset [{name}]\n";
            }
        }
        return msg;
    }

    private string getDatasetName(int year, string fileName) {

        var match = _nameRegEx.Match(fileName);
        if ( match.Success && match.Groups.Count>1) {
            string yearStr= match.Groups[1].Value;
            if ( int.TryParse(yearStr, out int targetYear) ) {
                return $"GB network {targetYear}/{targetYear-1999} ({year})";
            } else {
                throw new Exception($"Problem parsing year from year string [{yearStr}]");
            }
        } else {
            throw new Exception($"Problem extracting year from file name [{fileName}]");
        }
    }

    private string loadInterconnectors(IFormFile file) {
        using (var stream = file.OpenReadStream()) {
            using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                moveToSheet(reader,"GenInput");
                return readInterconnectors(reader);
            }
        }
    }

    private string readInterconnectors(IExcelDataReader reader) {
        bool foundStart=false;
        while(reader.Read()) {
            var firstCol = reader.GetString(0);
            if ( firstCol=="Station") {
                foundStart = true;
                break;
            }
        }

        if ( !foundStart ) {
            throw new Exception("Cannot find start of interconnector data");
        }
        int numAdded = 0;
        int numUpdated = 0;
        while (reader.Read()) {
            var name = reader.GetString(0);
            if ( string.IsNullOrEmpty(name)) {
                break;
            }
            var type = reader.GetString(1);
            // ignore everything bar interconnectors
            if ( type!="Interconnectors") {
                continue;
            }
            //
            var maxTEC = reader.GetDouble(2);
            var node1Code = reader.GetString(4);
            var node2Code = reader.GetString(5);
            if ( maxTEC>0 ) {
                if ( _interConnectors.ContainsKey(name) ) {                    
                    //
                    var ic = _interConnectors[name];
                    // mark it as being used for later use when adding interconnector links
                    ic.IsValid = true;
                    // if we have a second node then capacity is divided evenly between them
                    if ( !string.IsNullOrEmpty(node2Code) ) {
                        maxTEC = maxTEC/2;
                    }
                    if ( addInterConnector(name,maxTEC,node1Code,ic.BranchCode1,ic.ExtCode)) {
                        numAdded++;
                    } else {
                        numUpdated++;
                    }
                    //
                    if ( !string.IsNullOrEmpty(node2Code) ) {
                        if ( addInterConnector(name,maxTEC,node1Code,ic.BranchCode2,ic.ExtCode) ) {
                            numAdded++;
                        } else {
                            numUpdated++;
                        }
                    }
                    //
                } else {
                    throw new Exception($"Interconnector [{name}] has not been configured");
                }
            }
        }
        string msg = $"{numAdded} interconnectors added, {numUpdated} interconnectors updated";
        Logger.Instance.LogInfoEvent($"End reading interconnectors, {msg}");
        return msg;

    }

    private bool addInterConnector(string name, double maxTEC, string nodeCode, string branchCode, int extNodeNum) {

        bool added = false;

        var extNodeCode = getExtNodeCode(nodeCode, extNodeNum);
        // this is a node to represent connection to the external grid
        var branch = _branchCache.GetOrCreate($"{nodeCode}-{extNodeCode}:{branchCode}", out bool created);
        if ( created ) {
            branch.Dataset = _dataset;
            branch.Type = BoundCalcBranchType.HVDC;
            branch.Cap = maxTEC;
            // this node should exist and is the entry point to the UK network
            if ( !_nodeCache.TryGetValue(nodeCode, out var node1) ) {
                throw new Exception($"Could not find node [{nodeCode}] for interconnector [{name}]");
            }
            branch.Node1 = node1;
            // remove from node1
            node1.Generation_B-=maxTEC;
            // this is a node to represent connection to the external grid
            var node2 =  _nodeCache.GetOrCreate(extNodeCode, out bool extNodeCreated );
            if ( extNodeCreated ) {
                node2.Dataset = _dataset;
                node2.Ext = true;
                node2.Zone = node1.Zone;
                node2.SetVoltage();
                node2.SetLocation(_da);
            }
            // add to external node, node2
            node2.Generation_B+=maxTEC;
            branch.Node2 = node2;

            // add Ctrl
            Ctrl ctrl = new Ctrl(_dataset,branch);
            ctrl.Type = BoundCalcCtrlType.HVDC;
            ctrl.MinCtrl = -maxTEC;
            ctrl.MaxCtrl = +maxTEC;
            ctrl.Cost = 10;
            _da.BoundCalc.Add(ctrl);
            branch.SetCtrl(ctrl);
            added = true;
        } else {
            branch.Cap = maxTEC;
        }
        return added;
    }

    private string getExtNodeCode(string nodeCode, int nodeNum) {
        return nodeCode.Substring(0,5) + "X" + ( (nodeNum>0) ? nodeNum.ToString() : "");
    }

    private string loadNodes(IFormFile file) {
        using (var stream = file.OpenReadStream()) {
            using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                moveToSheet(reader,"Transport");
                return readNodes(reader);
            }
        }
    }

    private string readNodes(IExcelDataReader reader) {

        bool foundStart=false;
        while(reader.Read()) {
            var firstCol = reader.GetString(0);
            if ( firstCol=="Bus ID") {
                foundStart = true;
                break;
            }
        }

        if ( !foundStart ) {
            throw new Exception("Cannot find start of node data");
        }

        int nodeIndex=0;
        _nodeDataDict.Clear();
        while (reader.Read()) {
            var code = reader.GetString(nodeIndex+1);
            if ( string.IsNullOrEmpty(code)) {
                break;
            }
            var demand = reader.GetDouble(nodeIndex+4);
            var genA = reader.GetDouble(nodeIndex+5);
            var genB = reader.GetDouble(nodeIndex+6);
            var zoneCode = reader.GetString(nodeIndex+7);
            int? gen_zone;
            try {
                gen_zone = (int?) reader.GetDouble(nodeIndex+8);
            } catch( Exception e) {
                gen_zone = null;
            }
            int? dem_zone;
            try {
                dem_zone = (int?) reader.GetDouble(nodeIndex+9);
            } catch( Exception e) {
                dem_zone = null;
            }
            // check we haven't seen this one before
            if ( _nodeDataDict.ContainsKey(code ) ) {
                throw new Exception($"Repeated node code found [{code}]");
            } else {
                _nodeDataDict.Add(code, new NodeData() {
                    Code = code,
                    Demand = demand,
                    GenA = genA,
                    GenB = genB,
                    ZoneCode = zoneCode,
                    Gen_Zone = gen_zone,
                    Dem_Zone = dem_zone
                });
            }
            //
        }
        return "";
    }

    private void moveToSheet(IExcelDataReader reader, string sheetName) {
        do {
            var name = reader.Name;
            //
            if ( name==sheetName) {
                return;
            }
        } while (reader.NextResult());
        //
        throw new Exception($"Could not find {sheetName} sheet");
    }

    private string loadBranches(IFormFile file) {
        using (var stream = file.OpenReadStream()) {
            using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                moveToSheet(reader,"Transport");
                string msg = readBranches(reader);
                return msg;
            }
        }
    }

    private string addCtrls() {
        string msg="";
        using( var da = new DataAccess() ) {
            msg = addQuadBoosters(da);
            da.CommitChanges();
        }
        return msg;
    }

    private string addInterConnectorLinks() {
        string msg="";
        using( var da = new DataAccess() ) {
            msg = addInterConnectorLinks(da);
            da.CommitChanges();
        }
        return msg;
    }

    private string addInterConnectorLinks(DataAccess da) {
        int numAdded = 0;
        // get current ones
        var dataset = da.Datasets.GetDataset(_dataset.Id);
        var branches = da.BoundCalc.GetBranches(dataset);
        var interConnectorLinkBranches = branches.Where( m=>m.Node1.Ext && m.Node2.Ext).ToList();
        var interConnectorBranches = branches.Where( m=>!m.Node1.Ext && m.Node2.Ext).ToList();
        var extNodeIds = new List<int>();
        foreach( var ib in interConnectorBranches ) {
            var extNode = ib.Node2;
            // this stops adding a repeated branch for dual link inter connectors
            if ( extNodeIds.Contains(extNode.Id) ) {
                continue;
            }
            extNodeIds.Add(extNode.Id);
            var linkBranch = interConnectorLinkBranches.Where( m=>m.Node1.Id == extNode.Id  || m.Node2.Id == extNode.Id ).FirstOrDefault();
            if ( linkBranch==null ) {
                // find interconnector 
                var ic = getInterConnector(ib.Code);
                var nextIc = getNextInterConnector(ic);
                if ( nextIc!=null) {
                    var nextBranch=interConnectorBranches.Where(m=>m.Code == nextIc.BranchCode1).FirstOrDefault();
                    if ( nextBranch!=null ) {
                        var nextNode = nextBranch.Node2;
                        //
                        double cap = Math.Min(ib.Cap,nextBranch.Cap);
                        //
                        var branch = new Branch(_dataset);
                        branch.Code = $"{ib.Code}-{nextBranch.Code}";
                        branch.Type = BoundCalcBranchType.HVDC;
                        branch.Cap = cap;
                        branch.Node1 = extNode;
                        branch.Node2 = nextNode;
                        // add Ctrl
                        Ctrl ctrl = new Ctrl(_dataset,branch);
                        ctrl.Type = BoundCalcCtrlType.HVDC;
                        ctrl.MinCtrl = -cap;
                        ctrl.MaxCtrl = +cap;
                        ctrl.Cost = 20;
                        da.BoundCalc.Add(ctrl);
                        da.BoundCalc.Add(branch);
                        branch.SetCtrl(ctrl);
                        numAdded++;
                    }
                }
            }
        }
        return $"{numAdded} interconnector links added";
    }

    private InterConnector getInterConnector( string code ) {
        var ic = _interConnectors.Values.FirstOrDefault(m=>m.BranchCode1 == code || m.BranchCode2 == code);
        if ( ic!=null ) {
            return ic;
        } else {
            throw new Exception($"Cannot find interconnector with code [{code}]");
        }
    }

    private InterConnector? getNextInterConnector(InterConnector ic) {
        var nextIcs = _interConnectors.Values.Where(m=>m.Group == ic.Group && m.Index > ic.Index && m.IsValid).OrderBy(m=>m.Index).ToList();
        if ( nextIcs.Count > 0) {
            return nextIcs[0];
        } else {
            return null;
        }
    }

    private string addQuadBoosters(DataAccess da) {
        int numAdded = 0;
        // get current ones
        var dataset = da.Datasets.GetDataset(_dataset.Id);
        var existingBranches = da.BoundCalc.GetBranches(dataset);
        // this is a transformer between 2 nodes that have the same voltage
        var ctrlBranches=existingBranches.Where( m=>
            m.Node1.Voltage==m.Node2.Voltage && 
            m.Type == BoundCalcBranchType.Transformer && m.Code.StartsWith("Q")).ToList();
        foreach( var b in ctrlBranches) {
            Logger.Instance.LogInfoEvent($"QB Ctrl branch?=[{b.Node1.Code}] [{b.Node2.Code}]");
        }
        // add any that do not exist
        foreach( var b in ctrlBranches) {
            if ( b.Ctrl==null )  {
                var voltage=b.Node1.Code[4];
                var ctrl = new Ctrl() {
                    Branch = b,
                    //
                    MinCtrl = (voltage == '4') ? -0.2 : -0.15,
                    MaxCtrl = (voltage == '4') ?  0.2 :  0.15,
                    //
                    Type = BoundCalcCtrlType.QB,                        
                    Cost = 10.0,
                    //
                    Dataset = dataset,
                };
                //
                b.SetCtrl(ctrl);
                //
                da.BoundCalc.Add(ctrl);
                numAdded++;
                Logger.Instance.LogInfoEvent($"Adding ctrl=[{ctrl.Code} [{ctrl.Node1.Code}] [{ctrl.Node2.Code}]");
            }
        }

        //
        return $"{numAdded} quad boosters added";
    }

    private string readBranches(IExcelDataReader reader)
    {
        Logger.Instance.LogInfoEvent("Start reading branches");
        int branchIndex=-1;
        while(reader.Read() && branchIndex == -1 ) {
            for(int i=0;i<reader.FieldCount;i++) {
                var data = reader.GetValue(i);
                if ( data is string && ((string) data) == "TO Region") {
                    branchIndex = i;
                    break;
                }
            }
        }
        if ( branchIndex<0 ) {
            throw new Exception("Cannot find start of branch data");
        }
        //
        int numBranchesAdded = 0;
        int numBranchesUpdated = 0;
        int numNodesAdded = 0;
        //
        string overloadMsg = "\n";
        // Read data by row
        while (reader.Read()) {
            var region = reader.GetString(branchIndex);
            if ( string.IsNullOrEmpty(region)) {
                break;
            }
            var node1Code = reader.GetString(branchIndex+1);
            var node2Code = reader.GetString(branchIndex+2);
            var r = reader.GetDouble(branchIndex+3);
            var x = reader.GetDouble(branchIndex+4);
            //
            var ohl = reader.GetDouble(branchIndex+6);
            var cableLength = reader.GetDouble(branchIndex+7);
            var cap = reader.GetDouble(branchIndex+8);
            var code = reader.GetString(branchIndex+9);
            var linkType = reader.GetString(branchIndex+10);
            // node1
            Node node1, node2;
            if ( _nodeDataDict.ContainsKey(node1Code) ) {
                var nodeData = _nodeDataDict[node1Code];
                (node1,var added) = addNode(nodeData);
                if ( added ) {
                    numNodesAdded++;                    
                } 
            } else {
                throw new Exception($"Cannot find node [{node1Code}], referenced in branch [{code}]");
            }
            // node2
            if ( _nodeDataDict.ContainsKey(node2Code) ) {
                var nodeData = _nodeDataDict[node2Code];
                (node2,var added) = addNode(nodeData);
                if ( added ) {
                    numNodesAdded++;                    
                } 
            } else {
                throw new Exception($"Cannot find node [{node2Code}], referenced in branch [{code}]");
            }
            // branches
            var branch = _branchCache.GetOrCreate($"{node1Code}-{node2Code}:{code}", out bool created);
            if ( created ) {
                branch.Dataset = _dataset;                
                numBranchesAdded++;
            } else {
                numBranchesUpdated++;
            }
            branch.Cap = cap;
            branch.LinkType = linkType;
            branch.Node1 = node1;
            branch.Node2 = node2;
            branch.OHL = ohl;
            branch.CableLength = cableLength;
            branch.R = r;
            branch.X = x;
            branch.Region = region;
            if ( created ) {
                branch.SetType();
                if ( branch.Type == BoundCalcBranchType.HVDC ) {
                    var ctrl = new Ctrl(_dataset,branch);
                    ctrl.Type = BoundCalcCtrlType.HVDC;
                    ctrl.MinCtrl = -branch.Cap;
                    ctrl.MaxCtrl = branch.Cap;
                    ctrl.Cost = 10;
                    branch.X = 0; // HVDC branches have no reactance
                    branch.SetCtrl(ctrl);
                    _da.BoundCalc.Add(ctrl);                
                }                
            }
            // Compare link flows from the spreadsheet with line capacities
            var linkFlowPS = reader.GetDouble(branchIndex+14);
            var linkFlowYR = reader.GetDouble(branchIndex+18);
            var maxLinkFlow = Math.Max(Math.Abs(linkFlowPS),Math.Abs(linkFlowYR));
            // Look for instances where the link flow exceeds the capacity
            if ( branch.Cap>0 && maxLinkFlow>branch.Cap) {
                overloadMsg+=$"Link flow [{maxLinkFlow:F0}] exceeds capacity [{branch.Cap}] for branch [{code}]\n";
                //?? This get models 2025/26, 2026/27, 2027/28, 2028/29 going but 29/30 needs extra work.
                //??branch.Cap = maxLinkFlow*1.1;
            }

        }
        // These are nodes lised but not referenced by branched
        var numNodesNotUsed = _nodeDataDict.Values.Where( m=>!m.Used).Count();

        string msg = $"{numNodesAdded} nodes added\n";
        msg += $"{numBranchesAdded} branches added, {numBranchesUpdated} branches updated\n";
        if ( numNodesNotUsed>0) {
            msg += $"{numNodesNotUsed} nodes were not referenced by a branch and were ignored";
        }
        msg+=overloadMsg;
        Logger.Instance.LogInfoEvent($"End reading branches, {msg}");
        return msg;
    }

    private (Node,bool) addNode(NodeData nodeData) {
        bool added = false;
        nodeData.Used = true;
        var node = _nodeCache.GetOrCreate(nodeData.Code, out bool created);
        if ( created ) {
            node.Dataset = _dataset;
            node.SetVoltage();
            node.SetLocation(_da);
            added = true;
        } 
        var zone = _zoneCache.GetOrCreate(nodeData.ZoneCode, out created);
        if ( created ) {
            zone.Dataset = _dataset;
        } 
        node.Demand = nodeData.Demand;
        node.Generation_A = nodeData.GenA;
        node.Generation_B = nodeData.GenB;
        node.Zone = zone;
        node.Gen_Zone = nodeData.Gen_Zone;
        node.Dem_zone = nodeData.Dem_Zone;
        node.Ext = false;
        //
        return (node,added);
    }

    private string loadBoundaries(IFormFile file) {
        var sheetName = "ETYS Boundaries";
        using (var stream = file.OpenReadStream()) {
            using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                moveToSheet(reader,sheetName);
                return readBoundaries(reader);
            }
        }
    }

    private string readBoundaries(IExcelDataReader reader)
    {
        Logger.Instance.LogInfoEvent("Start reading boundaries");
        //
        int numBoundariesAdded = 0;
        int numZonesAdded = 0;
        int numBoundaryZonesUpdated = 0;
        //

        // Eat first  row
        reader.Read();
        //
        reader.Read();
        var boundaryCodes = new List<string>();
        for( int i=1; i<reader.FieldCount;i++) {
            var boundaryCode = reader.GetString(i);
            if ( string.IsNullOrEmpty(boundaryCode)) {
                break;
            }
            // B8 has a trailing space - so trim all 
            boundaryCode = boundaryCode.Trim();
            var boundary = _boundaryCache.GetOrCreate(boundaryCode, out bool created);
            if ( created ) {
                boundary.Dataset = _dataset;
                numBoundariesAdded++;
            }
            boundaryCodes.Add(boundaryCode);
        }

        // Read data by row
        while (reader.Read()) {
            var zoneCode = reader.GetString(0);
            if (string.IsNullOrEmpty(zoneCode)) {
                break;
            }
            // 
            var zone = _zoneCache.GetOrCreate(zoneCode, out bool created);
            if ( created ) {
                zone.Dataset = _dataset;
                numZonesAdded++;
            }
            //
            for( int i=0; i<boundaryCodes.Count;i++ ) {
                double entry;
                try {
                    entry = reader.GetDouble(i+1);
                }catch(Exception e) {
                    break;
                }
                var boundaryCode = boundaryCodes[i];
                var key=$"{boundaryCode}:{zoneCode}";
                if ( entry==1) {
                    // Create it if it doesn't exist
                    var bz = _boundaryZoneCache.GetOrCreate(key, out bool bzCreated);
                    if ( bzCreated ) {
                        bz.Dataset = _dataset;
                        numBoundaryZonesUpdated++;
                    }
                } else if ( entry == 0) {
                    // Delete it if it exists
                    if ( _boundaryZoneCache.TryGetValue(key, out BoundaryZone bz) ) {
                        _da.BoundCalc.Delete(bz);
                        numBoundaryZonesUpdated++;
                    }
                } 
            }
        }
        string msg = $"{numZonesAdded} zones added, {numBoundariesAdded} boundaries added, {numBoundaryZonesUpdated} boundary/zones entries updated";
        Logger.Instance.LogInfoEvent($"End reading boundaries, {msg}");
        return msg;
    }



}