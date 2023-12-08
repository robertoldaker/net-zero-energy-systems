using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowXlsmReader
    {

        private Dictionary<string, NodeResult> _nodeResults;
        private Dictionary<string, BranchResult> _branchResults;
        private Dictionary<string, CtrlResult> _ctrlResults;

        private Dictionary<string,TripResult> _singleTripResults;
        private Dictionary<string,TripResult> _dualTripResults;
        public LoadflowXlsmReader()
        {

        }

        public void LoadResults(string xlsmFile,string boundaryName=null) {
            loadNodeResults(xlsmFile);
            loadBranchResults(xlsmFile);
            loadCtrlResults(xlsmFile);
            if ( boundaryName!=null) {
                loadSingleTripResults(xlsmFile,boundaryName);
                loadDualTripResults(xlsmFile,boundaryName);
            }
        }

        private void loadNodeResults(string xlsmFile) {
            using (var stream = new FileStream(xlsmFile,FileMode.Open)) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Base") {
                            loadNodeData(reader);
                            return;
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Base\" sheet");
        }

        private void loadNodeData(IExcelDataReader reader) {
            int branchIndex, ctrlIndex;
            moveToStartRow(reader, out branchIndex, out ctrlIndex);
            _nodeResults = readNodeResults(reader);
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


        public class NodeResult {
            public double? Mismatch {get; set;}
        }

        private Dictionary<string,NodeResult> readNodeResults(IExcelDataReader reader) {
            var results=new Dictionary<string,NodeResult>();
            // 
            while (reader.Read()) {
                var code = reader.GetString(0);
                if ( string.IsNullOrEmpty(code)) {
                    break;
                }
                var mismatch = reader.GetDouble(9);
                results.Add(code,new NodeResult() { Mismatch = mismatch} );
            }
            return results;
        }

        private void loadBranchResults(string xlsmFile) {
            using (var stream = new FileStream(xlsmFile,FileMode.Open)) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Base") {
                            loadBranchData(reader);
                            return;
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Base\" sheet");
        }

        private void loadCtrlResults(string xlsmFile) {
            using (var stream = new FileStream(xlsmFile,FileMode.Open)) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Base") {
                            loadCtrlData(reader);
                            return;
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Base\" sheet");
        }

        private void loadBranchData(IExcelDataReader reader) {
            int branchIndex, ctrlIndex;
            moveToStartRow(reader, out branchIndex, out ctrlIndex);
            _branchResults=readBranches(reader, branchIndex);
        }

        private void loadCtrlData(IExcelDataReader reader) {
            int branchIndex, ctrlIndex;
            moveToStartRow(reader, out branchIndex, out ctrlIndex);
            _ctrlResults=readCtrls(reader, ctrlIndex);
        }

        public class BranchResult {
            public double? freePower {get; set;}
            public double? bFlow {get; set;}
        }

        private Dictionary<string,BranchResult> readBranches(IExcelDataReader reader, int branchIndex)
        {
            var results = new Dictionary<string,BranchResult>();
            // Read data by row
            while (reader.Read()) {
                var region = reader.GetString(branchIndex);
                if ( string.IsNullOrEmpty(region)) {
                    break;
                }
                var node1Code = reader.GetString(branchIndex+1);
                var node2Code = reader.GetString(branchIndex+2);
                var code = reader.GetString(branchIndex+3);
                var free = getNullableDouble(reader,branchIndex+10);
                var flow = getNullableDouble(reader,branchIndex+11);
                var key = $"{node1Code}-{node2Code}:{code}";
                results.Add(key,new BranchResult() { freePower = free, bFlow = flow });
            }
            return results;
        }

        private double? getNullableDouble(IExcelDataReader reader, int index) {
            var val = reader.GetValue(index);
            if ( val is double) {
                return (double) val;
            } else {
                return null;
            }
        }


        public class CtrlResult {
            public double SetPoint {get ;set;}
        }


        private Dictionary<string,CtrlResult> readCtrls(IExcelDataReader reader, int ctrlIndex) 
        {
            var results = new Dictionary<string,CtrlResult>();
            // Read data by row
            while (reader.Read()) {
                var region = reader.GetString(ctrlIndex+0);
                if ( string.IsNullOrEmpty(region) ) {
                    break;
                }
                var code = reader.GetString(ctrlIndex+3);
                var setPoint = reader.GetDouble(ctrlIndex+12);
                results.Add(code, new CtrlResult() { SetPoint = setPoint});
            }
            return results;
        }

        private void loadSingleTripResults(string xlsmFile, string boundaryName) {
            var sheetName = $"Base1{boundaryName}";
            using (var stream = new FileStream(xlsmFile,FileMode.Open)) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name.Trim()==sheetName) {
                            loadSingleTripData(reader);
                            return;
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Base\" sheet");
        }

        private void loadSingleTripData(IExcelDataReader reader) {
            moveToStartSingleTrips(reader);
            _singleTripResults=readTrips(reader);
        }

        private void moveToStartSingleTrips(IExcelDataReader reader) {
            bool singleFound = false;
            while (reader.Read()) {
                var node = reader.GetString(0);
                if ( !singleFound && node=="Single circuit trips") {
                    singleFound=true;
                } else if ( singleFound && node=="Surplus") {
                    return;
                }
            }
            throw new Exception("Could not find start row of single circuit trips");
        }

        private void loadDualTripResults(string xlsmFile, string boundaryName) {
            var sheetName = $"Base1{boundaryName}";
            using (var stream = new FileStream(xlsmFile,FileMode.Open)) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name.Trim()==sheetName) {
                            loadDualTripData(reader);
                            return;
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception($"Could not find sheet [{sheetName}]");
        }

        private void loadDualTripData(IExcelDataReader reader) {
            moveToStartDualTrips(reader);
            _dualTripResults=readTrips(reader);
        }

        private void moveToStartDualTrips(IExcelDataReader reader) {
            bool singleFound = false;
            while (reader.Read()) {
                var node = reader.GetValue(0);
                if ( !singleFound && node is string && (string) node=="Dual circuit trips") {
                    singleFound=true;
                } else if ( singleFound && node is string && (string) node=="Surplus") {
                    return;
                }
            }
            throw new Exception("Could not find start row of dual circuit trips");
        }

        private Dictionary<string,TripResult> readTrips(IExcelDataReader reader)
        {
            var results = new Dictionary<string,TripResult>();

            // get dictionary of ctrl names and column indedeces
            int colIndex=4;
            bool cont=true;
            var ctrlDict=new Dictionary<string,int>();
            while(colIndex<reader.FieldCount) {
                var ctrlName=reader.GetString(colIndex);
                if ( !string.IsNullOrEmpty(ctrlName) ) {
                    ctrlDict.Add(ctrlName,colIndex);
                } else {
                    break;
                }
                colIndex++;
            }
            
            // Read data by row
            while (reader.Read()) {
                var surplus = getNullableDouble(reader, 0);
                var capacity = getNullableDouble(reader, 1);
                if ( surplus==null || capacity==null) {
                    break;
                }
                var tripStr=reader.GetString(2);
                //
                var tripCpnts = tripStr.Split(':');
                var trip = tripCpnts[0];
                //
                var tripResult = new TripResult() { Surplus = (double) surplus, Capacity = (double) capacity };
                foreach( var cd in ctrlDict) {
                    var sp = reader.GetDouble(cd.Value);
                    tripResult.SetPointDict.Add(cd.Key,sp);
                }
                results.Add(trip,tripResult);
            }
            return results;
        }

        public class TripResult {
            public TripResult() {
                SetPointDict = new Dictionary<string, double>();
            }
            public double Surplus {get; set;}
            public double Capacity {get; set;}
            public Dictionary<string,double> SetPointDict {get; set;}
        }

        public Dictionary<string, NodeResult> NodeResults {
            get {
                return _nodeResults;
            }
        }

        public Dictionary<string, BranchResult> BranchResults {
            get {
                return _branchResults;
            }
        }
        
        public Dictionary<string, CtrlResult> CtrlResults {
            get {
                return _ctrlResults;
            }
        }
        public Dictionary<string, TripResult> SingleTripResults {
            get {
                return _singleTripResults;
            }
        }
        public Dictionary<string, TripResult> DualTripResults {
            get {
                return _dualTripResults;
            }
        }
    }
}
