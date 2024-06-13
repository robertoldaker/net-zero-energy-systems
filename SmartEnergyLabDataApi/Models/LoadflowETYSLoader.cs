using System.Text;
using System.Text.Json;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NLog.LayoutRenderers;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Loadflow;
public class LoadflowETYSLoader
{
    private HttpClient _httpClient;
    private object _httpClientLock = new object();

    private string APPENDIX_B_URL = "https://www.nationalgrideso.com/document/304986/download";
    private string APPENDIX_G_URL = "https://www.nationalgrideso.com/document/294506/download";
    private string DATASET_BASE_NAME = "GB network";
    private int BASE_YEAR = 2023;
    
    public LoadflowETYSLoader()
    {

    }

    public void Load() {
        string fn = saveAppendix(APPENDIX_B_URL);
        loadAppendixB(fn);
        fn = saveAppendix(APPENDIX_G_URL);
        loadAppendixG(fn);
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
                    Logger.Instance.LogInfoEvent($"Cannot find any GSP nodes for nodeDemand [{nodeDemand.Node}]");
                    continue;
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

    private void loadAppendixB(string fn) {
        var transCircuits = loadAllCircuits(fn);
        var nodeNames = getNodeNames(transCircuits);
        updateNodes(nodeNames);
        updateBranches(transCircuits);
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
        var datasetName = $"{DATASET_BASE_NAME} {year}";
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

    private List<Circuit> loadAllCircuits(string fn) {
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
            list.Add(new Circuit(type, reader, owner));
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
