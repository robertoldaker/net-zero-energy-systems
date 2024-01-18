using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using Microsoft.VisualBasic;
using NHibernate.Linq.ReWriters;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Elsi;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class ElsiXlsmReader
    {
        
        public ElsiXlsmReader()
        {

        }

        public ElsiRefResults Load(string xlsmFile) {
            using (var stream = new FileStream(xlsmFile,FileMode.Open)) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Results") {
                            return loadResults(reader);
                        }
                    } while (reader.NextResult());
                }
            }
            throw new Exception("Could not find \"Results\" sheet");
        }

        private ElsiRefResults loadResults(IExcelDataReader reader) {
            var results = new ElsiRefResults();

            //
            results.Read(reader);

            return results;
        }

    }

    public class ElsiRefResults {
        public ElsiRefResults() {
            MarketMism = new ElsiRefEntry();
            BalanceMism = new ElsiRefEntry();
            Availabilities = new Dictionary<string, ElsiRefSection>();
            MarketPhase = new Dictionary<string, ElsiRefSection>();
            BalancePhase = new Dictionary<string, ElsiRefSection>();
            BalanceMechanism = new Dictionary<string, ElsiRefSection>();
        }
        public ElsiRefEntry MarketMism {get; set;}
        public ElsiRefEntry BalanceMism {get; set;}
        public Dictionary<string,ElsiRefSection> Availabilities {get; set;}
        public Dictionary<string,ElsiRefSection> MarketPhase {get; set;}
        public Dictionary<string,ElsiRefSection> BalancePhase {get; set;}
        public Dictionary<string,ElsiRefSection> BalanceMechanism {get; set;}

        private void moveTo(IExcelDataReader reader, string name) {
            while (reader.Read()) {
                var node = reader.GetString(0);
                if ( node==name) {
                    return;
                }
            }
            throw new Exception($"Could not find row with name [{name}]");
        }

        public void Read(IExcelDataReader reader) {
            //
            moveTo(reader, "Mkt mism");
            MarketMism.Read(reader);
            moveTo(reader, "Bal mism");
            BalanceMism.Read(reader);
            //
            while(reader.Read()) {
                var header = reader.GetValue(3);
                if ( IsValidDayHeader(header, out string headerTxt)) {
                    if ( headerTxt == "Dem") {
                        break;
                    }
                } 
            }

            Availabilities = loadSection(reader,new string[]{"Dem","Avail"});
            MarketPhase = loadSection(reader,new string[]{"Mkt"});
            BalancePhase = loadSection(reader,new string[]{"Bal"});
            BalanceMechanism = loadSection(reader,new string[]{"BOA","Costs"});
        }

        public static bool IsValidDayHeader( object header, out string headerTxt) {
            headerTxt = "";
            if ( header!=null && header is string ) {
                string headerStr = (string) header;
                if (headerStr.StartsWith("#1Pk")) {
                    if ( headerStr.Length>5 ) {
                        headerTxt = headerStr.Substring(5).TrimEnd();
                    }
                }
                return true;
            } else {
                return false;
            }
        }

        private Dictionary<string,ElsiRefSection> loadSection(IExcelDataReader reader, string[] validDayHeaders) {
            var results = new Dictionary<string,ElsiRefSection>();
            do {
                var header = reader.GetValue(3);
                if ( IsValidDayHeader(header, out string headerTxt)) {
                    if ( validDayHeaders.Contains(headerTxt)) {
                        var sectionName = reader.GetString(0);
                        if ( results.ContainsKey(sectionName)) {
                            sectionName+="_1";
                        }   
                        var refSection = new ElsiRefSection();
                        results.Add(sectionName,refSection);
                        if ( !refSection.Read(reader,validDayHeaders) ) {
                            break;
                        }
                    } else {
                        break;
                    }
                } else {
                    break;
                }
            } while( true );
            return results;
        }

        public class ElsiRefSection {
            public ElsiRefSection() {
            }
            public Dictionary<string,ElsiRefEntry> Entries {get; set;}

            public bool Read(IExcelDataReader reader, string[] validDayHeaders) {
                Entries = new Dictionary<string, ElsiRefEntry>(StringComparer.InvariantCultureIgnoreCase);
                while(reader.Read()) {
                    var header = reader.GetValue(3);
                    if ( IsValidDayHeader(header, out string headerTxt)) {
                        return true;
                    } else {
                        var rowName = reader.GetString(0);
                        var rowVal = reader.GetValue(3);
                        if ( !string.IsNullOrWhiteSpace(rowName) && rowVal!=null ) {
                            var refEntry = new ElsiRefEntry();
                            Entries.Add(rowName,refEntry);
                            refEntry.Read(reader);
                        }
                    }
                }
                //
                return false;
            }
        }

        public class ElsiRefEntry {
            public ElsiRefEntry() {
                DayValues = new Dictionary<int, double?[]>();
            }
            private double? getNullableDouble(IExcelDataReader reader, int index) {
                var val = reader.GetValue(index);
                if ( val is double) {
                    return (double) val;
                } else {
                    return null;
                }
            }
            public void Read(IExcelDataReader reader) {
                int index = 1;
                Cost = getNullableDouble(reader,index++);
                Capacity = getNullableDouble(reader,index++);
                int numPeriods = 5;
                for( int day=1; day<=365; day++) {
                    double?[] values = new double?[numPeriods];
                    for ( int i=0;i<numPeriods;i++) {
                        values[i] = getNullableDouble(reader,index++);
                    }
                    DayValues.Add(day,values);
                }
            }
            public double? Cost {get; set;}
            public double? Capacity {get; set;}
            public Dictionary<int,double?[]> DayValues {get; set;}
        }
    }

}
