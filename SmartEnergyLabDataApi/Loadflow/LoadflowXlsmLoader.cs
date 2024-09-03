using System.Diagnostics;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowXlsmLoader
    {
        private DataAccess _da;
        private Dataset _dataset;

        private ObjectCache<Node> _nodeCache;
        private ObjectCache<Zone> _zoneCache;
        private ObjectCache<Branch> _branchCache;
        private ObjectCache<Ctrl> _ctrlCache;
        private ObjectCache<Data.Boundary> _boundaryCache;
        private ObjectCache<BoundaryZone> _boundaryZoneCache;
        public LoadflowXlsmLoader()
        {
        }

        public string Load(IFormFile formFile) {
            string msg = "";
            using( _da = new DataAccess() ) {
                var name = "GB network";
                _dataset = _da.Datasets.GetDataset(DatasetType.Loadflow,name);
                if ( _dataset==null) {
                    var root = _da.Datasets.GetRootDataset(DatasetType.Loadflow);
                    _dataset = new Dataset() { Type = DatasetType.Loadflow, Parent = root, Name = name };                
                    _da.Datasets.Add(_dataset);
                }
                // Caches of existing objects
                var existingNodes = _da.Loadflow.GetNodes(_dataset);
                _nodeCache = new ObjectCache<Node>(_da, existingNodes, m=>m.Code, (m,code)=>m.Code=code );
                //
                var existingZones = _da.Loadflow.GetZones(_dataset);
                _zoneCache = new ObjectCache<Zone>(_da, existingZones, m=>m.Code, (m,code)=>m.Code=code );
                //
                var existingBranches = _da.Loadflow.GetBranches(_dataset);
                _branchCache = new ObjectCache<Branch>(_da, existingBranches, m=>m.GetKey(), (m,key)=>m.SetCode(key) );
                //
                var existingCtrls = _da.Loadflow.GetCtrls(_dataset);
                _ctrlCache = new ObjectCache<Ctrl>(_da, existingCtrls, m=>m.Code, (m,code)=>{} );

                var existingBoundaries = _da.Loadflow.GetBoundaries(_dataset);
                _boundaryCache = new ObjectCache<Data.Boundary>(_da, existingBoundaries, m=>m.Code, (m,code)=>m.Code=code );
                //
                var existingBoundaryZones = _da.Loadflow.GetBoundaryZones(_dataset);
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

                msg+=loadNodes(formFile) + "\n";
                msg+=loadBranches(formFile) + "\n";
                msg+=loadCtrls(formFile) + "\n";
                msg+=loadBoundaries(formFile) + "\n";
                //
                _da.CommitChanges();
            }
            return msg;
        }

        private string loadNodes(IFormFile file) {
            using (var stream = file.OpenReadStream()) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Base") {
                            return loadNodeData(reader);
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Base\" sheet");
        }

        private string loadNodeData(IExcelDataReader reader) {
            int branchIndex, ctrlIndex;
            moveToStartRow(reader, out branchIndex, out ctrlIndex);
            return readNodes(reader);
        }

        private void moveToStartRow(IExcelDataReader reader, out int branchIndex, out int ctrlIndex) {
            while (reader.Read()) {
                var node = reader.GetString(0);
                if ( node=="Node") {
                    branchIndex = 0;
                    ctrlIndex = 0;
                    for( int i=1;i<reader.FieldCount;i++) {
                        var columnHeader = reader.GetString(i);
                        if ( columnHeader=="Region") {
                            if ( branchIndex==0 ) {
                                branchIndex = i;
                            } else {
                                ctrlIndex = i;
                            }
                        }
                    }
                    //
                    if ( branchIndex==0 || ctrlIndex==0 ) {
                        throw new Exception("Cannot find \"Region\" column");
                    }
                    return;
                }
            }
            throw new Exception("Could not find start row to load data");
        }

        private string readNodes(IExcelDataReader reader) {
            Logger.Instance.LogInfoEvent("Start reading nodes");
            int numNodesAdded = 0;
            int numNodesUpdated = 0;
            int numZonesAdded = 0;
            while (reader.Read()) {
                var code = reader.GetString(0);
                if ( string.IsNullOrEmpty(code)) {
                    break;
                }
                var demand = reader.GetDouble(1);
                var genA = reader.GetDouble(2);
                var genB = reader.GetDouble(3);
                var zoneCode = reader.GetString(4);
                int? gen_zone;
                try {
                    gen_zone = (int?) reader.GetDouble(5);
                } catch( Exception e) {
                    gen_zone = null;
                }
                int? dem_zone;
                try {
                    dem_zone = (int?) reader.GetDouble(6);
                } catch( Exception e) {
                    dem_zone = null;
                }
                var ext = reader.GetBoolean(7);
                //
                var node = _nodeCache.GetOrCreate(code, out bool created);
                if ( created ) {
                    node.Dataset = _dataset;
                    node.SetVoltage();
                    node.SetLocation(_da);
                    numNodesAdded++;
                } else {
                    numNodesUpdated++;
                }
                var zone = _zoneCache.GetOrCreate(zoneCode, out created);
                if ( created ) {
                    zone.Dataset = _dataset;
                    numZonesAdded++;
                } 
                node.Demand = demand;
                node.Generation_A = genA;
                node.Generation_B = genB;
                node.Zone = zone;
                node.Gen_Zone = gen_zone;
                node.Dem_zone = dem_zone;
                node.Ext = ext;
                //                
            }
            string msg = $"{numNodesAdded} nodes added, {numNodesUpdated} nodes updated, {numZonesAdded} zones added";
            Logger.Instance.LogInfoEvent($"End reading nodes, {msg}");
            return msg;
        }

        private string loadBranches(IFormFile file) {
            using (var stream = file.OpenReadStream()) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Base") {
                            return loadBranchData(reader);
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Base\" sheet");
        }

        private string loadCtrls(IFormFile file) {
            using (var stream = file.OpenReadStream()) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Base") {
                            return loadCtrlData(reader);
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Base\" sheet");
        }

        private string loadBoundaries(IFormFile file) {
            using (var stream = file.OpenReadStream()) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Boundaries") {
                            return readBoundaries(reader);
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Base\" sheet");
        }

        private string loadBranchData(IExcelDataReader reader) {
            int branchIndex, ctrlIndex;
            moveToStartRow(reader, out branchIndex, out ctrlIndex);
            return readBranches(reader, branchIndex);
        }

        private string loadCtrlData(IExcelDataReader reader) {
            int branchIndex, ctrlIndex;
            moveToStartRow(reader, out branchIndex, out ctrlIndex);
            return readCtrls(reader, ctrlIndex);
        }

        private string readBranches(IExcelDataReader reader, int branchIndex)
        {
            Logger.Instance.LogInfoEvent("Start reading branches");
            //
            int numBranchesAdded = 0;
            int numBranchesUpdated = 0;
            //
            // Read data by row
            while (reader.Read()) {
                var region = reader.GetString(branchIndex);
                if ( string.IsNullOrEmpty(region)) {
                    break;
                }
                var node1Code = reader.GetString(branchIndex+1);
                var node2Code = reader.GetString(branchIndex+2);
                var code = reader.GetString(branchIndex+3);
                var r = reader.GetDouble(branchIndex+4);
                var x = reader.GetDouble(branchIndex+5);
                var ohl = reader.GetDouble(branchIndex+6);
                var cap = reader.GetDouble(branchIndex+8);
                var linkType = reader.GetString(branchIndex+9);
                // node1
                if ( !_nodeCache.TryGetValue(node1Code, out Node node1)) {
                    throw new Exception($"Cannot find node [{node1Code}]");
                }
                // node2
                if ( !_nodeCache.TryGetValue(node2Code, out Node node2)) {
                    throw new Exception($"Cannot find node [{node2Code}]");
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
                branch.R = r;
                branch.X = x;
                branch.Region = region;
                if ( created ) {
                    branch.SetType();
                }
            }
            string msg = $"{numBranchesAdded} branches added, {numBranchesUpdated} branches updated";
            Logger.Instance.LogInfoEvent($"End reading branches, {msg}");
            return msg;
        }

        private string readBoundaries(IExcelDataReader reader)
        {
            Logger.Instance.LogInfoEvent("Start reading boundaries");
            //
            int numBoundariesAdded = 0;
            int numBoundaryZonesUpdated = 0;
            //

            //
            reader.Read(); // Eat first row
            reader.Read();
            var zoneStr = reader.GetString(0);
            if ( zoneStr!="Zone") {
                throw new Exception($"Unexpected first header cell found [{zoneStr}], expecting [\"Zone\"]");
            }
            // Boundary coes are the header names
            var boundaryCodes = new string[reader.FieldCount];
            for( int i=1; i<reader.FieldCount;i++) {
                var boundaryCode = reader.GetString(i);
                if ( string.IsNullOrEmpty(boundaryCode)) {
                    break;
                }
                // B8 has a trailing space - so trim all 
                boundaryCode = boundaryCode.TrimEnd();
                var boundary = _boundaryCache.GetOrCreate(boundaryCode, out bool created);
                if ( created ) {
                    boundary.Dataset = _dataset;
                    numBoundariesAdded++;
                }
                boundaryCodes[i] = boundaryCode;
            }

            // Read data by row
            while (reader.Read()) {
                var zoneCode = reader.GetString(0);
                // node
                if ( !_zoneCache.TryGetValue(zoneCode, out Zone zone)) {
                    throw new Exception($"Cannot find zone [{zoneCode}]");
                }
                //
                for( int i=1; i<reader.FieldCount;i++ ) {
                    double entry;
                    try {
                        entry = reader.GetDouble(i);
                    }catch(Exception e) {
                        break;
                    }
                    var boundaryCode = boundaryCodes[i];
                    var key=$"{boundaryCode}:{zoneCode}";
                    if ( entry==1) {
                        // Create it if it doesn't exist
                        var bz = _boundaryZoneCache.GetOrCreate(key, out bool created);
                        if ( created ) {
                            bz.Dataset = _dataset;
                            numBoundaryZonesUpdated++;
                        }
                    } else if ( entry == 0) {
                        // Delete it if it exists
                        if ( _boundaryZoneCache.TryGetValue(key, out BoundaryZone bz) ) {
                            _da.Loadflow.Delete(bz);
                            numBoundaryZonesUpdated++;
                        }
                    } 
                }
            }
            string msg = $"{numBoundariesAdded} boundaries added, {numBoundaryZonesUpdated} boundary/zones entries updated";
            Logger.Instance.LogInfoEvent($"End reading boundaries, {msg}");
            return msg;
        }

        private string readCtrls(IExcelDataReader reader, int ctrlIndex) 
        {
            Logger.Instance.LogInfoEvent("Start reading ctrls");
            string msg="";
            //
            int numCtrlsAdded = 0;
            int numCtrlsUpdated = 0;
            //
            // Caches of existing objects
            var existingNodes = _da.Loadflow.GetNodes(_dataset);
            var nodeCache = new ObjectCache<Node>(_da, existingNodes, m=>m.Code, (m,code)=>m.Code=code );
            //
            // Read data by row
            while (reader.Read()) {
                var region = reader.GetString(ctrlIndex+0);
                if ( string.IsNullOrEmpty(region) ) {
                    break;
                }
                var node1Code = reader.GetString(ctrlIndex+1);
                var node2Code = reader.GetString(ctrlIndex+2);
                var code = reader.GetString(ctrlIndex+3);
                var type = (LoadflowCtrlType) Enum.Parse(typeof(LoadflowCtrlType),reader.GetString(ctrlIndex+4));
                var minCtrl = reader.GetDouble(ctrlIndex+5);
                var maxCtrl = reader.GetDouble(ctrlIndex+6);
                var cost = reader.GetDouble(ctrlIndex+11);
                // 
                var ctrl = _ctrlCache.GetOrCreate(code, out bool created);
                if ( created ) {
                    ctrl.Dataset = _dataset;
                    var key = $"{node1Code}-{node2Code}:{code}";
                    if ( _branchCache.TryGetValue(key, out Branch b) ) {
                        ctrl.Branch = b;
                    } else {
                        var m= $"Could not find branch for ctrl [{key}]";
                        Logger.Instance.LogInfoEvent(m);
                        msg+=m;
                    }
                    numCtrlsAdded++;
                } else {
                    numCtrlsUpdated++;
                }
                ctrl.Region = region;
                ctrl.Type = type;
                ctrl.MinCtrl = minCtrl;
                ctrl.MaxCtrl = maxCtrl;
                ctrl.Cost = cost;
                if ( created && ctrl.Branch!=null ) {
                        ctrl.Branch.SetCtrl(ctrl);
                }
            }
            var mm=$"{numCtrlsAdded} ctrls added, {numCtrlsUpdated} ctrls updated";
            Logger.Instance.LogInfoEvent($"End reading ctrls {mm}");
            msg+=mm;
            return msg;
        }

        public class CsvHeaders {
            public CsvHeaders() {
                Headers = new List<CsvHeader>();
            }
            public void Add(string name, int index) {
                Headers.Add( new CsvHeader(name, index));
            }
            public List<CsvHeader> Headers {get; set;}
            public void Check(IExcelDataReader reader) {
                // read header
                reader.Read();
                if ( reader.FieldCount!=Headers.Count ) {
                    throw new Exception($"Found {reader.FieldCount} columns but expecting {Headers.Count}");
                }
                for(int i=0; i<reader.FieldCount;i++) {
                    if ( reader.GetString(i)!=Headers[i].Name ) {
                        throw new Exception($"Unexpected header [{reader.GetString(i)}] found at index [{i}] but expecting [{Headers[i].Name}]");
                    }
                }
            }
        }

        public class CsvHeader {
            public CsvHeader( string name, int index) {
                Name = name;
                Index = index;
            }
            public string Name {get; set;}
            public int Index {get; set;}
        }

   }
} 