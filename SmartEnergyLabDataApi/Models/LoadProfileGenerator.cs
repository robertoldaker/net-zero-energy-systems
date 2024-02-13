
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

    public void Generate(LoadProfileType type) {
        Logger.Instance.LogInfoEvent($"Started generating missing load profiles for type [{type}]...");
        var source = getSource(type);
        _sourceDsd = getDistributionDataBySource(source);
        Logger.Instance.LogInfoEvent($"After getting source DistributionData");
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
            //??Logger.Instance.LogInfoEvent($"After AllLoadProfiles");            
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
            //??Logger.Instance.LogInfoEvent($"Before LoadProfile add");            
            foreach( var lp in toAdd) {
                da.SubstationLoadProfiles.Add(lp);
            }
            //??Logger.Instance.LogInfoEvent($"Before Commit");            
            da.CommitChanges();
        }
        return total;
    }

    private int getClosestSubstationId(Substations.DistributionInfo di) {        
        var targetCustomers = di.NumCustomers;
        var dsd = _sourceDsd.OrderBy(m=>Math.Abs(m.NumCustomers-targetCustomers)).FirstOrDefault();
        return dsd.DistributionSubstation.Id;
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

    public void ClearDummy(LoadProfileType type) {
        Logger.Instance.LogInfoEvent($"Clearing all dummy load profiles ...");
        int intType=(int) type;
        var tableName = "substation_load_profiles";
        DataAccessBase.RunSql($"delete from {tableName} slp where slp.isdummy=true and slp.type={intType};");
        Logger.Instance.LogInfoEvent($"Finished clearing dummy load profiles");
    }
}