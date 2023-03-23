using System.Text.Json;

namespace SmartEnergyLabDataApi.Models
{
    public interface ICarbonIntensityFetcher {
        public CarbonIntensity Fetch();
    }

    public class CarbonIntensityFetcher : ICarbonIntensityFetcher {
        private HttpClient _httpClient;
        private CarbonIntensity _cache;
        private TimeSpan _maxAge = new TimeSpan(2,0,0,0);

        public CarbonIntensityFetcher() {
            _httpClient = new HttpClient();

        }

        public CarbonIntensity Fetch() {
            // See if we have a cached value less than maximum age and return that
            if ( _cache!=null && (DateTime.Now - _cache.Date) < _maxAge) {
                return _cache;
            } else {
                Console.WriteLine("Fetching new carbon data");
                // Otherwise fetch new value
                var carbonIntensity = fetch();
                if ( carbonIntensity!=null ) {
                    _cache = carbonIntensity;
                    return carbonIntensity;
                } else {
                    throw new Exception("Problem obtaining up-to-date carbon intensity");
                }
            }
        }

        private CarbonIntensity fetch() {
            var url = getUrl(out DateTime startDate);
            var response = _httpClient.GetStringAsync(url).Result;
            var data = JsonSerializer.Deserialize<DataContainer>(response);
            if ( data == null) {
                throw new Exception("Problem deserializing Carbon intensity response");
            }
            return toCarbonIntensity(data, startDate);
        }

        private CarbonIntensity toCarbonIntensity( DataContainer data, DateTime startDate ) {
            var ci = new CarbonIntensity(startDate);
            ci.Date = startDate;
            for( int i=0;i<48;i++) {
                var mins = i*30;
                DateTime dt = startDate + new TimeSpan(0,mins, 0);
                var entry = data.data.Where(m=>m.from.ToLocalTime() == dt).FirstOrDefault();
                if ( entry!=null ) {
                    ci.Rates.Add( new CarbonIntensityRate() { Num = i+1, Rate=entry.intensity.actual });
                } else {
                    throw new Exception($"Could not find CarbonIntensityRate for [{dt}]");
                }
            }
            return ci;
        }

        private string getUrl(out DateTime startDate) {
            var oneDay = new TimeSpan(24,0,0);
            DateTime yesterday = DateTime.Now - oneDay;
            startDate = new DateTime(yesterday.Year,yesterday.Month,yesterday.Day);
            string startDateStr = startDate.ToString("yyyy-MM-dd");
            string url = $"https://api.carbonintensity.org.uk/intensity/date/{startDateStr}";
            return url;
        }

        public class DataContainer {
            public IntensityData[] data{get; set;}
        }

        public class IntensityData {
            public DateTime from {get; set;}
            public DateTime to {get; set;}
            public IntensityDetail intensity { get; set;}
        }

        public class IntensityDetail {
            public double forecast {get; set;}
            public double actual {get; set;}
            public string index {get; set;}
        }
    }

    public class CarbonIntensity {

        public CarbonIntensity(DateTime date) {
            Rates = new List<CarbonIntensityRate>();
            Date = date;
        }
        public DateTime Date {get; set;}

        public IList<CarbonIntensityRate> Rates {get; set;}
    }

    public class CarbonIntensityRate {
        public double Rate {get; set;}
        public int Num {get ;set; }
    }


}