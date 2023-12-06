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
        public LoadflowXlsmReader()
        {

        }

        public void LoadResults(string baseFile) {
            loadNodeResults(baseFile);
            loadBranchResults(baseFile);
            loadCtrlResults(baseFile);
        }

        private void loadNodeResults(string baseFile) {
            using (var stream = new FileStream(baseFile,FileMode.Open)) {
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

        private void loadBranchResults(string baseFile) {
            using (var stream = new FileStream(baseFile,FileMode.Open)) {
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

        private void loadCtrlResults(string baseFile) {
            using (var stream = new FileStream(baseFile,FileMode.Open)) {
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
    }
}
