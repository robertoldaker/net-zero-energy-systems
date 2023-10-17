using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class SubstationCache
    {
        private Dictionary<string, PrimarySubstation> _primSubsByExternalIdDict;
        private Dictionary<string, PrimarySubstation> _primSubsByNameDict;
        private Dictionary<string, DistributionSubstation> _distSubsByExternalIdDict;
        private Dictionary<string, DistributionSubstation> _distSubsByNameDict;
        private Dictionary<string, SubstationLoadProfile> _ssLoadProfilesDict;
        private Dictionary<string, SubstationClassification> _ssClassificationsDict;

        public SubstationCache(DataAccess da, DistributionNetworkOperator dno)
        {
            Logger.Instance.LogInfoEvent("Loading cache");
            //
            /*_primSubsByExternalIdDict = new Dictionary<string, PrimarySubstation>();
            _primSubsByNameDict = new Dictionary<string, PrimarySubstation>();
            var psss = da.Substations.GetPrimarySubstations(dno);
            foreach( var pss in psss) {
                if ( !string.IsNullOrEmpty(pss.ExternalId) ) {
                    if ( _primSubsByExternalIdDict.ContainsKey(pss.ExternalId)) {
                        Logger.Instance.LogInfoEvent($"Primary substation with externalId=[{pss.ExternalId}] already exists");
                    } else {
                        _primSubsByExternalIdDict.Add(pss.ExternalId,pss);
                    }
                }
                if ( !string.IsNullOrEmpty(pss.Name)) {
                    if ( _primSubsByNameDict.ContainsKey(pss.Name)) {
                        Logger.Instance.LogInfoEvent($"Primary substation with name=[{pss.Name}] already exists");
                    } else {
                        _primSubsByNameDict.Add(pss.Name,pss);
                    }
                }
            }
            */
            // Distribution substations by external id
            _distSubsByExternalIdDict = new Dictionary<string, DistributionSubstation>();
            _distSubsByNameDict = new Dictionary<string, DistributionSubstation>();
            var dsss = da.Substations.GetDistributionSubstations(dno);
            foreach( var dss in dsss) {
                if ( !string.IsNullOrEmpty(dss.ExternalId) ) {
                    if ( _distSubsByExternalIdDict.ContainsKey(dss.ExternalId) ) {
                        Logger.Instance.LogInfoEvent($"Distribution substation with externalId=[{dss.ExternalId}] already exists");
                    } else {
                        _distSubsByExternalIdDict.Add(dss.ExternalId, dss);
                    }
                }
                if ( !string.IsNullOrEmpty(dss.Name)) {
                    string key=$"{dss.Name}:{dss.PrimarySubstation.Name}";
                    if ( _distSubsByNameDict.ContainsKey(key) ) {
                        Logger.Instance.LogInfoEvent($"Distribution substation with key=[{key}] already exists");
                    } else {
                        _distSubsByNameDict.Add(key, dss);
                    }
                }
            }
            // Substation load profiles by distributionSubstation, monthNumber and day
            _ssLoadProfilesDict = new Dictionary<string, SubstationLoadProfile>();
            var ssCs = da.SubstationLoadProfiles.GetSubstationLoadProfile(dno);
            foreach (var ssC in ssCs) {
                var key = getKey(ssC.DistributionSubstation, ssC.MonthNumber, ssC.Day);
                if ( !_ssLoadProfilesDict.ContainsKey(key)) {
                    _ssLoadProfilesDict.Add(key, ssC);
                }
            }
            // Substation classifications by distributionSubstation, classification number
            _ssClassificationsDict = new Dictionary<string, SubstationClassification>();
            var ssCls = da.SubstationClassifications.GetSubstationClassifications(dno);
            foreach (var ssCl in ssCls) {
                var key = getKey(ssCl.DistributionSubstation, ssCl.Num);
                if (!_ssClassificationsDict.ContainsKey(key)) {
                    _ssClassificationsDict.Add(key, ssCl);
                }
            }
            Logger.Instance.LogInfoEvent("Cache loaded");
        }

        private string getKey(DistributionSubstation dc, int monthNumber, Day day)
        {
            return $"{dc.Id}_{monthNumber}_{day}";
        }

        private string getKey(DistributionSubstation dc, int num)
        {
            return $"{dc.Id}_{num}";
        }

        //??public void Add(PrimarySubstation pss)
        //??{
        //??    _primSubstationsDict.Add(pss.ExternalId, pss);
        //??}

        public void Add(DistributionSubstation pss)
        {
            _distSubsByExternalIdDict.Add(pss.ExternalId, pss);
        }

        public void Add(SubstationLoadProfile ssC)
        {
            var key = getKey(ssC.DistributionSubstation, ssC.MonthNumber, ssC.Day);
            if ( !_ssLoadProfilesDict.ContainsKey(key) ) {
                _ssLoadProfilesDict.Add(key, ssC);
            }
        }

        public void Add(SubstationClassification ssCl)
        {
            _ssClassificationsDict.Add(getKey(ssCl.DistributionSubstation, ssCl.Num), ssCl);
        }
        //??public PrimarySubstation GetPrimarySubstation(string externalId)
        //??{
        //??    PrimarySubstation pss = null;
        //??    if (string.IsNullOrEmpty(externalId)) {
        //??        return null;
        //??    }
        //??    _primSubstationsDict.TryGetValue(externalId, out pss);
        //??    return pss;
        //??}

        public DistributionSubstation GetDistributionSubstation(string externalId, string dssName, string primName)
        {
            DistributionSubstation dss = null;
            if ( string.IsNullOrEmpty(externalId) ) {
                return null;
            }
            if( _distSubsByExternalIdDict.TryGetValue(externalId, out dss)) {
                return dss;
            } else {
                _distSubsByNameDict.TryGetValue($"{dssName}:{primName}", out dss);
                return dss;
            }
        }

        public SubstationLoadProfile GetSubstationLoadProfile(DistributionSubstation dss, int monthNumber, Day day)
        {
            SubstationLoadProfile ssC = null;
            var key = getKey(dss, monthNumber, day);
            _ssLoadProfilesDict.TryGetValue(key, out ssC);
            return ssC;
        }

        public SubstationClassification GetSubstationClassification(DistributionSubstation dss, int num)
        {
            SubstationClassification ssC = null;
            var key = getKey(dss, num);
            _ssClassificationsDict.TryGetValue(key, out ssC);
            return ssC;
        }
    }

}