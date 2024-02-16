
using System.Net.Http.Headers;
using System.Xml.Schema;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate.Util;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Controllers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models;

public class LoadProfileGenerator {

    private IList<DistributionSubstationData> _sourceDsd;
    private Dictionary<DistributionSubstation,DistData> _distDataDict;

    public void Generate(LoadProfileType type) {
        Logger.Instance.LogInfoEvent($"Started generating missing load profiles for type [{type}]...");
        var source = getSource(type);
        _distDataDict = getRealDataUsingClassifications(type);
        Logger.Instance.LogInfoEvent($"After getting source _distDataDict");
        int total;
        do {
            total = generateNextBatch(source);
        } while( total>0);
        Logger.Instance.LogInfoEvent($"Finished generating missing load profiles");
    }

    private LoadProfileSource getSource(LoadProfileType type) {
        LoadProfileSource source;
        if ( type==LoadProfileType.Base) {
            source = LoadProfileSource.LV_Spreadsheet;
        } else if ( type ==LoadProfileType.EV) {
            source = LoadProfileSource.EV_Pred;
        } else if ( type ==LoadProfileType.HP) {
            source = LoadProfileSource.HP_Pred;
        } else {
            throw new Exception($"Unexpected value for type [{type}]");
        }
        return source;
    }

    private int generateNextBatch(LoadProfileSource source) {
        int total=0;
        int batchSize=5000;
        using( var da = new DataAccess() ) {
            var diDict = new Dictionary<Substations.DistributionInfo,int>();
            var toAdd = new List<SubstationLoadProfile>();
            //
            var targetDis = da.Substations.GetDistributionSubstationsWithoutLoadProfiles(source, batchSize, out total);
            Logger.Instance.LogInfoEvent($"Processing next batch, remaining=[{total}]");            
            foreach( var di in targetDis) {
                var sourceId = getClosestSubstationId(di);
                diDict.Add(di, sourceId);
            }
            // get distinct list of distribution ids to lookup
            var ids = diDict.Values.Distinct().ToArray();
            var allLoadProfiles = da.SubstationLoadProfiles.GetDistributionSubstationLoadProfiles(ids,source);
            Logger.Instance.LogInfoEvent($"After AllLoadProfiles");            
            foreach( var di in targetDis) {
                var sourceId = diDict[di];
                var dss = da.Substations.GetDistributionSubstation(di.Id);
                var lps = allLoadProfiles.Where(m=>m.DistributionSubstation.Id==sourceId).ToList();
                foreach( var lp in lps) {
                    var newLp = lp.Copy(dss);
                    newLp.IsDummy = true;
                    toAdd.Add(newLp);
                }                
            }
            Logger.Instance.LogInfoEvent($"Before LoadProfile add");            
            foreach( var lp in toAdd) {
                da.SubstationLoadProfiles.Add(lp);
            }
            Logger.Instance.LogInfoEvent($"Before Commit");            
            da.CommitChanges();
        }
        return total;
    }

    /*private int getClosestSubstationId(Substations.DistributionInfo di) {        
        var targetCustomers = di.NumCustomers;
        var dsd = _sourceDsd.OrderBy(m=>Math.Abs(m.NumCustomers-targetCustomers)).FirstOrDefault();
        return dsd.DistributionSubstation.Id;
    } */  

    private int getClosestSubstationId(Substations.DistributionInfo di) {        
        DistData dsd=null;
        if ( di.DayMaxDemand!=0) {
            dsd = _distDataDict.Values.Where(m=>m.MaxLoad>0).OrderBy(m=>Math.Abs(m.MaxLoad-di.DayMaxDemand)).FirstOrDefault();
        } else if ( di.NumCustomers!=0) {
            dsd = _distDataDict.Values.OrderBy(m=>Math.Abs(m.NumCustomers-di.NumCustomers)).FirstOrDefault();
        } else {

        }
        return dsd!=null ? dsd.DistId : 0;
    }   

    private IList<DistributionSubstationData> getSpreadsheetLoadedDistributionData() {
        using (var da = new DataAccess() ) {
            var dsIds = da.Substations.GetDistributionSubstationIdsWithLoadProfiles(LoadProfileSource.LV_Spreadsheet);
            var dsd = da.Substations.GetDistributionSubstationData(dsIds.ToArray());
            return dsd;
        }
    }

    private IList<DistributionSubstationData> getDistributionDataBySource(LoadProfileSource source) {
        using (var da = new DataAccess() ) {
            var dsIds = da.Substations.GetDistributionSubstationIdsWithLoadProfiles(source);
            var dsd = da.Substations.GetDistributionSubstationData(dsIds.ToArray());
            return dsd;
        }
    }

    public class DistData {
        public int DistId {get; set;}
        public int NumCustomers {get; set;}
        public double MaxLoad {get; set;}
    }

    private Dictionary<DistributionSubstation,DistData> getRealDataUsingClassifications(LoadProfileType type) {
        Logger.Instance.LogInfoEvent("Started load getCustomerDataUsingClassifications ...");
        var dict = new Dictionary<DistributionSubstation,DistData>();
        using (var da = new DataAccess() ) {
            var cs = da.Substations.GetSubstationClassifications(type);
            foreach( var c in cs) {
                if ( !dict.ContainsKey(c.DistributionSubstation)) {
                    dict.Add(c.DistributionSubstation,new DistData() {
                        //
                        DistId = c.DistributionSubstation.Id
                    });
                }
                dict[c.DistributionSubstation].NumCustomers+=c.NumberOfCustomers;
            }
            //
            int[] ids = dict.Values.Select(m=>m.DistId).ToArray();
            //
            var lpss = da.SubstationLoadProfiles.GetSubstationLoadProfiles(ids,LoadProfileSource.LV_Spreadsheet);
            foreach( var dd in dict.Values ) {
                //
                var lps = lpss.Where(m=>m.DistributionSubstation.Id==dd.DistId).ToList();
                //
                double maxLoad =0;
                foreach( var lp in lps) {
                    var cmax = lp.Data.Max();
                    if ( cmax>maxLoad) {
                        maxLoad=cmax;
                    }
                }
                //
                dd.MaxLoad = maxLoad;
            }
            
        }
        Logger.Instance.LogInfoEvent("Finished load getCustomerDataUsingClassifications");
        return dict;
    }

    private Dictionary<DistributionSubstation,DistData> getRealDistProfileData(LoadProfileType type) {
        Logger.Instance.LogInfoEvent("Started load getRealDistProfileData ...");
        var dict = new Dictionary<DistributionSubstation,DistData>();
        using (var da = new DataAccess() ) {
            var lpss = da.SubstationLoadProfiles.GetSubstationLoadProfiles(type,false);
            foreach( var lp in lpss ) {
                //
                if ( !dict.ContainsKey(lp.DistributionSubstation)) {
                    dict.Add(lp.DistributionSubstation,new DistData() {
                        //
                        DistId = lp.DistributionSubstation.Id
                    });
                }
                //
                var distData = dict[lp.DistributionSubstation];
                //
                var cmax = lp.Data.Max();
                if ( cmax>distData.MaxLoad) {
                    distData.MaxLoad=cmax;
                }
            }            
        }
        Logger.Instance.LogInfoEvent("Finished load getRealDistProfileData");
        return dict;
    }

    public void ClearDummy(LoadProfileType type) {
        Logger.Instance.LogInfoEvent($"Clearing all dummy load profiles ...");
        int intType=(int) type;
        var tableName = "substation_load_profiles";
        DataAccessBase.RunSql($"delete from {tableName} slp where slp.isdummy=true and slp.type={intType};");
        Logger.Instance.LogInfoEvent($"Finished clearing dummy load profiles");
    }

    public DistributionSubstation GetClosestProfileDistSubstation(int distId, LoadProfileType type) {
        using (var da = new DataAccess() ) {
            var targetDist = da.Substations.GetDistributionSubstation(distId);
            _distDataDict = getRealDataUsingClassifications(type);
            if (targetDist.SubstationData==null) {
                throw new Exception("Null distribution data");
            } else if ( targetDist.SubstationData.NumCustomers==0 && targetDist.SubstationData.DayMaxDemand==0 ) {
                throw new Exception("NumCustomers and DayMaxDemand are 0");
            }
            //
            var distInfo = new Substations.DistributionInfo() {
                Id = targetDist.Id,
                NumCustomers = targetDist.SubstationData.NumCustomers,
                DayMaxDemand = targetDist.SubstationData.DayMaxDemand
            };
            //
            int sourceId = getClosestSubstationId(distInfo);
            var dss = da.Substations.GetDistributionSubstation(sourceId);
            return dss;
        }       
    }
}