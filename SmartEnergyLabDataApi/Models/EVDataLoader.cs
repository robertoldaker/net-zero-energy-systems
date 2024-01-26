using System.Diagnostics;
using System.Text.Json;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class EVDataLoader
    {
        private DataAccess _da;

        private Dictionary<string,Dictionary<string,double>> _forecastsDict;
        private profile? _profile;
        private ObjectCache<SubstationLoadProfile> _cache;
        private Dictionary<string,DistributionSubstation> _dssDict;

        private int _numAdded,_numUpdated;

        public EVDataLoader(DataAccess da, int gspId)
        {
            _da = da;
            // Cache of any existing load profiles
            var existingLoadProfiles = _da.SubstationLoadProfiles.GetSubstationLoadProfiles(LoadProfileType.EV,LoadProfileSource.EV_Pred);
            _cache = new ObjectCache<SubstationLoadProfile>(_da, existingLoadProfiles, m=>GetKey(m.DistributionSubstation.ExternalId,m.MonthNumber,m.Day), (m,k)=>{} );
            // Cache of distribution substations
            var existingDistrictSubstations = _da.Substations.GetDistributionSubstationsByGSPId(gspId);
            _dssDict = new Dictionary<string,DistributionSubstation>();
            foreach( var dss in existingDistrictSubstations) {
                _dssDict.Add(dss.ExternalId,dss);
            }

        }

        public void Load(IFormFile forecastsFile, IFormFile profilesFile)
        {
            Logger.Instance.LogInfoEvent("Start loading EV data");

            loadProfile(profilesFile);            
            loadForecasts(forecastsFile);

            // 
            int baseYear = 2021;
            _numAdded = 0;
            _numUpdated = 0;
            int loopCount=0;
            foreach( var ssId in _forecastsDict.Keys) {
                if (_dssDict.ContainsKey(ssId)) {
                    var scalingFactors = getScalingFactors(baseYear,_forecastsDict[ssId]);
                    // loop over each month
                    for(int i=1;i<=12;i++) {
                        updateLoadProfile(ssId,scalingFactors,baseYear,i,Day.Saturday);
                        updateLoadProfile(ssId,scalingFactors,baseYear,i,Day.Sunday);
                        updateLoadProfile(ssId,scalingFactors,baseYear,i,Day.Weekday);
                    }
                } else {
                    Logger.Instance.LogInfoEvent($"Substation [{ssId}] not found");
                }
                loopCount++;
            }
            Logger.Instance.LogInfoEvent("End loading EV data");
        }

        public double[] getScalingFactors(int baseYear, Dictionary<string,double> factorsDict) {

            int finalYear=2050;
            double[] scalingFactors = new double[finalYear-baseYear+1];
            for( int i=0;i<scalingFactors.Length;i++) { 
                int year = baseYear+i;
                if (factorsDict.ContainsKey(year.ToString())) {
                    scalingFactors[i]=factorsDict[year.ToString()];
                } else {
                    scalingFactors[i]=0;
                }
            }
            return scalingFactors;
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

        private void updateLoadProfile(string ssId, double[] scalingFactors, int baseYear, int month, Day day) {
            // Scale profile by number of Evs
            double[] profile=getProfile(month,day);
            // Get hold of any existing db object
            var lp = _cache.GetOrCreate(GetKey(ssId,month,day),out bool created);
            if ( created ) {
                // If newly created ensure its initialised
                lp.setDistributionSubstation(_dssDict[ssId]);
                lp.Type = LoadProfileType.EV;
                lp.Source = LoadProfileSource.EV_Pred;
                lp.MonthNumber = month;
                lp.Year = baseYear;
                lp.Day = day;
                lp.IntervalMins=30;
                _numAdded++;
            } else {
                _numUpdated++;
            }
            lp.Data = profile;
            lp.ScalingFactors = scalingFactors;
        }

        private double[] getProfile(int m, Day day) {
            months months;
            if ( day==Day.Saturday) {
                months = _profile.BEV.Saturday;
            } else if ( day==Day.Sunday) {
                months = _profile.BEV.Sunday;
            } else if ( day==Day.Weekday) {
                months = _profile.BEV.Weekdays;
            } else {
                throw new Exception($"Unexpected Day [{day}]");
            }
            Dictionary<string,double> profileDict;
            if ( m==1 ) {
                profileDict = months.January;
            } else if ( m==2) {
                profileDict = months.February;
            } else if ( m==3) {
                profileDict = months.March;
            } else if ( m==4) {
                profileDict = months.April;
            } else if ( m==5) {
                profileDict = months.May;
            } else if ( m==6) {
                profileDict = months.June;
            } else if ( m==7) {
                profileDict = months.July;
            } else if ( m==8) {
                profileDict = months.August;
            } else if ( m==9) {
                profileDict = months.September;
            } else if ( m==10) {
                profileDict = months.October;
            } else if ( m==11) {
                profileDict = months.November;
            } else if ( m==12) {
                profileDict = months.December;
            } else {
                throw new Exception($"Unexpected month [{m}]");
            }
            return profileDict.Values.ToArray();
        }

        private string GetKey(string ssId, int month, Day day) {
            return $"{ssId}:{month}:{day}";
        }

        private void loadForecasts(IFormFile file) {
            _forecastsDict = new Dictionary<string, Dictionary<string, double>>();
            using (var stream = file.OpenReadStream()) {
                var dict = JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(stream);
                foreach( var d in dict) {
                    var ssdId = d.Key;
                    var  objDict=d.Value.Deserialize<Dictionary<string,JsonElement>>();
                    if ( objDict.TryGetValue("BEV", out var je)) {
                        var lpDict = je.Deserialize<List<Dictionary<string,double>>>();
                        _forecastsDict.Add(ssdId,lpDict[0]);
                    } else {
                        throw new Exception("Could not find \"BEV\" data");
                    }
                }
            }
        }

        private void loadProfile(IFormFile file) {
            using (var stream = file.OpenReadStream()) {
                _profile = JsonSerializer.Deserialize<profile>(stream);

            }

        }

        private class profile {
            public monthDay BEV { get; set; }
        }

        private class monthDay {
            public months Weekdays {get; set;}
            public months Saturday { get; set;}
            public months Sunday { get; set;}
        }

        private class months {
            public Dictionary<string,double> January {get; set;}
            public Dictionary<string,double> February {get; set;}
            public Dictionary<string,double> March {get; set;}
            public Dictionary<string,double> April {get; set;}
            public Dictionary<string,double> May {get; set;}
            public Dictionary<string,double> June {get; set;}
            public Dictionary<string,double> July {get; set;}
            public Dictionary<string,double> August {get; set;}
            public Dictionary<string,double> September {get; set;}
            public Dictionary<string,double> October {get; set;}
            public Dictionary<string,double> November {get; set;}
            public Dictionary<string,double> December {get; set;}
        }

   }
}