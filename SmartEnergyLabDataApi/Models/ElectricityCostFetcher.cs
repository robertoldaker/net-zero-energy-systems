using System.Text.Json;

namespace SmartEnergyLabDataApi.Models
{
    public interface IElectricityCostFetcher {
        public ElectricityCost Fetch();
    }

    public class ElectricityCostFetcher : IElectricityCostFetcher {
        private HttpClient _httpClient;
        private ElectricityCost _cache;
        private TimeSpan _maxAge = new TimeSpan(2,0,0,0);

        public ElectricityCostFetcher() {
            _httpClient = new HttpClient();
        }

        public ElectricityCost Fetch() {
            // See if we have a cached value less than maximum age and return that
            if ( _cache!=null && (DateTime.Now - _cache.Date) < _maxAge) {
                return _cache;
            } else {
                Console.WriteLine("Fetching new cost data");
                // Otherwise fetch new value
                var electricityCost = fetch();                
                if ( electricityCost!=null ) {
                    _cache = electricityCost;
                    return electricityCost;
                } else {
                    throw new Exception("Problem obtaining up-to-date carbon intensity");
                }
            }
        }

        private ElectricityCost fetch() {
            var url = getUrl(out DateTime startDate);
            var response = _httpClient.GetStringAsync(url).Result;
            var data = JsonSerializer.Deserialize<OuterDataContainer>(response);
            if ( data == null) {
                throw new Exception("Problem deserializing Carbon intensity response");
            }
            
            return toElectricityCost(data.data, startDate);
        }

        private string getUrl(out DateTime startDate) {
            var oneDay = new TimeSpan(24,0,0);
            //var twoDays = new TimeSpan(48,0,0);
            DateTime start = DateTime.Now - oneDay;
            startDate = new DateTime(start.Year,start.Month,start.Day);
            string startDateStr = startDate.ToString("dd-MM-yyyy");
            var endDate = startDate + oneDay;
            string endDateStr = endDate.ToString("dd-MM-yyyy");
            string url = $"https://odegdcpnma.execute-api.eu-west-2.amazonaws.com/development/prices?dno=22&voltage=LV-Sub&start={startDateStr}&end={endDateStr}";
            return url;
        }

        private ElectricityCost toElectricityCost( DataContainer data, DateTime startDate ) {
            var ci = new ElectricityCost(startDate);
            ci.Date = startDate;
            for( int i=0;i<48;i++) {
                var mins = i*30;
                DateTime dt = startDate + new TimeSpan(0,mins, 0);
                var ts = dt.ToString("HH:mm dd-MM-yyyy");
                var entry = data.data.Where(m=>m.Timestamp == ts).FirstOrDefault();
                if ( entry!=null ) {
                    ci.Details.Add( new ElectricityCostDetail() { Num = i+1, Cost=entry.Overall });
                } else {
                    throw new Exception($"Could not find ElectrictyCostDetails for [{ts}]");
                }
            }
            return ci;
        }
        
        private class OuterDataContainer {
            public string status {get; set;}
            public DataContainer data {get; set;}
        }

        private class DataContainer {
            public string dnoRegion {get; set;}
            public string voltageLevel {get; set;}
            public CostDetail[] data {get; set;}
        }

        private class CostDetail {
            public double Overall {get; set;}
            public string Timestamp {get; set;}
        }

    }

    public class ElectricityCost {

        public ElectricityCost(DateTime date) {
            Date = date;
            Details = new List<ElectricityCostDetail>();
        }
        public DateTime Date {get; set;}

        public IList<ElectricityCostDetail> Details {get; set;}
    }

    public class ElectricityCostDetail {
        public double Cost {get; set;}
        public int Num {get; set;}
    }
}

