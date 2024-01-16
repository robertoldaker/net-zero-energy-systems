
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

    public void Generate() {
        _sourceDsd = getSpreadsheetLoadedDistributionData();
        Logger.Instance.LogInfoEvent($"Started generating missing load profiles ...");
        int total;
        do {
            total = generateNextBatch();
        } while( total>0);
        Logger.Instance.LogInfoEvent($"Finished generating missing load profiles");
    }

    private int generateNextBatch() {
        int total=0;
        int batchSize=2000;
        using( var da = new DataAccess() ) {
            var dssDict = new Dictionary<DistributionSubstation,int>();
            var targetDist = da.Substations.GetDistributionSubstationsWithoutLoadProfiles(batchSize, out total);
            Logger.Instance.LogInfoEvent($"Processing next batch, remaining=[{total}]");
            foreach( var dss in targetDist) {
                var sourceId = getClosestSubstationId(dss);
                dssDict.Add(dss, sourceId);
            }
            // get distinct list of distribution ids to lookup
            var ids = dssDict.Values.Distinct().ToArray();
            var allLoadProfiles = da.SubstationLoadProfiles.GetDistributionSubstationLoadProfiles(ids,LoadProfileSource.LV_Spreadsheet);
            foreach( var dss in targetDist) {
                var sourceId = dssDict[dss];
                var lps = allLoadProfiles.Where(m=>m.DistributionSubstation.Id==sourceId).ToList();
                foreach( var lp in lps) {
                    var newLp = lp.Copy(dss);
                    newLp.IsDummy = true;
                    da.SubstationLoadProfiles.Add(newLp);
                }
            }

            da.CommitChanges();
        }
        return total;
    }

    private int getClosestSubstationId(DistributionSubstation dss) {        
        var targetCustomers = dss.SubstationData.NumCustomers;
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

    public void ClearDummy() {
        Logger.Instance.LogInfoEvent($"Clearing all dummy load profiles ...");
        DataAccessBase.RunSql("delete from substation_load_profiles slp where slp.isdummy=true;");
        Logger.Instance.LogInfoEvent($"Finished clearing dummy load profiles");
    }
}