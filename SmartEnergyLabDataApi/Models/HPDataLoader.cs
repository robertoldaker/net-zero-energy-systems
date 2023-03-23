using System.Diagnostics;
using System.Text.Json;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class HPDataLoader
    {
        private DataAccess _da;
        private int _gaId;

        private ObjectCache<SubstationLoadProfile> _cache;
        private Dictionary<string,DistributionSubstation> _dssDict;

        private enum MonthEnum {January=1, February, March, April, May, June, July, August, September, October, November, December};

        private enum DayEnum { weekday=Day.Weekday, saturday=Day.Saturday, sunday=Day.Sunday}

        private int _numAdded,_numUpdated;

        public HPDataLoader(DataAccess da, int gaId)
        {
            _da = da;
            _gaId = gaId;
            // Cache of any existing load profiles
            var existingLoadProfiles = _da.SubstationLoadProfiles.GetSubstationLoadProfiles(LoadProfileType.HP,LoadProfileSource.HP_Pred);
            _cache = new ObjectCache<SubstationLoadProfile>(_da, existingLoadProfiles, m=>GetKey(m.DistributionSubstation.ExternalId,m.Year,m.MonthNumber,m.Day), (m,k)=>{} );
            // Cache of distribution substations
            var existingDistrictSubstations = _da.Substations.GetDistributionSubstationsByGAId(_gaId);
            _dssDict = new Dictionary<string,DistributionSubstation>();
            foreach( var dss in existingDistrictSubstations) {
                _dssDict.Add(dss.ExternalId,dss);
            }

        }

        public void Load(IFormFile file)
        {

            // 
            _numAdded = 0;
            _numUpdated = 0;

            //
            var dataList = loadLoadProfileData(file);
            //
            int count=0;
            foreach( var lpd in dataList) {
                if (_dssDict.ContainsKey(lpd.ssId)) {
                    var lp = _cache.GetOrCreate(GetKey(lpd.ssId,lpd.year,(int) lpd.month,lpd.day),out bool created);
                    if ( created ) {
                        // If newly created ensure its initialised
                        lp.setDistributionSubstation(_dssDict[lpd.ssId]);
                        lp.Type = LoadProfileType.HP;
                        lp.Source = LoadProfileSource.HP_Pred;
                        lp.MonthNumber = (int) lpd.month;
                        lp.Year = lpd.year;
                        lp.Day = lpd.day;
                        lp.IntervalMins=30;
                        _numAdded++;
                    } else {
                        _numUpdated++;
                    }
                    lp.Data = lpd.data;
                    lp.DeviceCount = lpd.count;                    
                } else {
                    Console.WriteLine($"Ignoring substation with id=[{lpd.ssId}]");
                }
                Console.WriteLine($"Processed [{count++}] of [{dataList.Count}]");
            }

        }

        private List<loadProfileData> loadLoadProfileData(IFormFile file) {
            var dataList = new List<loadProfileData>();
            using (var stream = file.OpenReadStream()) {                
                var dict = JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(stream);
                foreach( var d in dict) {
                    var ssId = d.Key;
                    var  objDict=d.Value.Deserialize<Dictionary<string,JsonElement>>();
                    if ( objDict.TryGetValue("ASHP", out var je)) {
                        var yearDict = je.Deserialize<Dictionary<string,JsonElement>>();
                        foreach( var yd in yearDict) {
                            int year = int.Parse(yd.Key);
                            if ( year==0 ) {
                                year = 2021;
                            }
                            if ( yd.Value.ValueKind == JsonValueKind.Object) {
                                var dayDict = yd.Value.Deserialize<Dictionary<string,JsonElement>>();
                                double count=0;
                                if ( dayDict.ContainsKey("count")) {
                                    count = dayDict["count"].Deserialize<double>();
                                }
                                foreach( var dd in dayDict) {
                                    if ( dd.Key!="count") {
                                        Day day = (Day) Enum.Parse<DayEnum>(dd.Key);
                                        if ( dd.Value.ValueKind == JsonValueKind.Object) {
                                            var monthDict = dd.Value.Deserialize<Dictionary<string,List<double[]>>>();
                                            foreach( var md in monthDict) {
                                                var month = Enum.Parse<MonthEnum>(md.Key);
                                                var data = md.Value[0];
                                                dataList.Add(new loadProfileData(ssId,year,day,month,data,count));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } else {
                        throw new Exception("Could not find \"ASHP\" data");
                    }
                }
            }
            return dataList;
        }

        public int NumAdded  {
            get {
                return _numAdded;
            }
        }

        public int NumUpdated {
            get {
                return _numUpdated;
            }
        }

        private string GetKey(string ssId, int year, int month, Day day) {
            return $"{ssId}:{year}:{month}:{day}";
        }


        private class loadProfileData {

            public loadProfileData(string ssId, int year, Day day, MonthEnum month, double[] data, double count) {
                this.ssId = ssId;
                this.year = year;
                this.day = day;
                this.month = month;
                this.data = data;
                this.count = count;
            }
            public string ssId {get; set;}
            public int year {get; set;}
            public Day day {get; set;}
            public MonthEnum month {get; set;}
            public double[] data {get; set;}
            public double count {get; set;}
        }


   }
}