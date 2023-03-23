using System.Diagnostics;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;

namespace SmartEnergyLabDataApi.Data
{
    public static class SubstationLoadProfileMethods {

        public static List<SubstationLoadProfile> Aggregate( this IList<SubstationLoadProfile> loadProfiles, LoadProfileSource source) {
            var sscDict = new Dictionary<string,SubstationLoadProfile>();
            string key = "";
            foreach( var con  in loadProfiles) {
                if ( source == LoadProfileSource.Tool ) {
                    key = $"{con.Day}:{con.Season}";
                } else {
                    key = $"{con.Day}:{con.MonthNumber}";
                }

                if ( !sscDict.ContainsKey(key)) {                    
                    sscDict.Add(key,con);
                } else {
                    var sum = sscDict[key];
                    for( int i=0; i<sum.Data.Length;i++) {
                        sum.Data[i]+=con.Data[i];
                    }
                    sum.DeviceCount+=con.DeviceCount;
                }
            }

            //
            var list=sscDict.Values.ToList();
            return list;
        }

        public static void AddCarbonData(this SubstationLoadProfile lp, ICarbonIntensityFetcher cf )
        {
            var cis = cf.Fetch();
            var iMins = lp.IntervalMins;
            lp.Carbon = new double[lp.Data.Length];
            for( int i=0; i< lp.Data.Length;i++) { 
                int num = ((i*iMins)/30) + 1;
                var ci = cis.Rates.Where(m=>m.Num==num).FirstOrDefault();
                if ( ci!=null) {
                    lp.Carbon[i] = (lp.Data[i] * ci.Rate)/1000.0; // in kg / h
                } else {
                    throw new Exception($"Could not find CarbonIntensityRate for num=[{num}]");
                }
            }
        }
        public static void AddCostData(this SubstationLoadProfile lp, IElectricityCostFetcher cf )
        {
            var ecs = cf.Fetch();
            var iMins = lp.IntervalMins;
            lp.Cost = new double[lp.Data.Length];
            for( int i=0; i< lp.Data.Length;i++) { 
                int num = ((i*iMins)/30) + 1;
                var ec = ecs.Details.Where(m=>m.Num==num).FirstOrDefault();
                if ( ec!=null) {
                    lp.Cost[i] = (lp.Data[i] * ec.Cost)/100.0; // in £ / h
                } else {
                    throw new Exception($"Could not find ElectricityCost for num=[{num}]");
                }
            }
        }

    }
}