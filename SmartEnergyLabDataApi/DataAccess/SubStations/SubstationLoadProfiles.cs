using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Util;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;
using SmartEnergyLabDataApi.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Web;

namespace SmartEnergyLabDataApi.Data
{
    public class SubstationLoadProfiles : DataSet
    {
        public SubstationLoadProfiles(DataAccess da) : base (da)
        {

        }

        public void Add(SubstationLoadProfile hhc)
        {
            Session.Save(hhc);
        }
        public void Delete(SubstationLoadProfile hhc)
        {
            Session.Delete(hhc);
        }

        public void PopulateMissingKeys() {
            var dsss = Session.QueryOver<DistributionSubstation>().List();
            foreach( var dss in dsss) {
                // Loop over each one and save 
                int total=0;
                using( var da = new DataAccess()) {
                    var slps = da.Session.QueryOver<SubstationLoadProfile>().Where(m=>m.DistributionSubstation.Id==dss.Id).List();
                    total+=slps.Count;
                    Console.WriteLine($"Populating keys for [{dss.Id}], lps=[{slps.Count}], total=[{total}]");
                    foreach( var slp in slps) {
                        slp.PrimarySubstation = slp.DistributionSubstation.PrimarySubstation;
                        slp.GeographicalArea = slp.DistributionSubstation.PrimarySubstation.GeographicalArea;
                    }
                    da.CommitChanges();
                }
            }
        }

        public IList<SubstationLoadProfile> GetDistributionSubstationLoadProfiles(int id, 
            LoadProfileSource source,
            int year,
            ICarbonIntensityFetcher? carbonFetcher=null,
            IElectricityCostFetcher? costFetcher=null
            )
        {
            var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m=>m.DistributionSubstation.Id == id).
                And(m=>m.Source == source);
            var list = q.List();
            if ( source == LoadProfileSource.EV_Pred || source == LoadProfileSource.HP_Pred) {
                // adjust load profiles so they give the correct data for the given year
                foreach( var lp in list) {
                    lp.AdjustForYear(year);
                }
            }
            if( carbonFetcher!=null) {
                foreach( var lp in list) {
                    lp.AddCarbonData(carbonFetcher);
                }
            }
            if( costFetcher!=null) {
                foreach( var lp in list) {
                    lp.AddCostData(costFetcher);
                }
            }

            return list;
        }

        public IList<SubstationLoadProfile> GetDistributionSubstationLoadProfiles(int[] dssIds, LoadProfileSource source)
        {
            var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m=>m.DistributionSubstation.Id.IsIn(dssIds)).
                And(m=>m.Source == source);
            var list = q.List();
            return list;
        }

        public IList<SubstationLoadProfile> GetSubstationLoadProfile(DistributionNetworkOperator dno)
        {
            PrimarySubstation pss = null;
            DistributionSubstation dss = null;
            var q = Session.QueryOver<SubstationLoadProfile>().
                Left.JoinAlias(m => m.DistributionSubstation, () => dss).
                Left.JoinAlias(m=>dss.PrimarySubstation, ()=>pss).
                Where(m => pss.DistributionNetworkOperator == dno).
                And(m=>m.Source == LoadProfileSource.LV_Spreadsheet);

            return q.List();
        }

        public IList<SubstationLoadProfile> GetSubstationLoadProfiles(DistributionSubstation dss)
        {
            var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m => m.DistributionSubstation == dss).
                And(m=>m.Source == LoadProfileSource.LV_Spreadsheet);

            return q.List();
        }

        public IList<SubstationLoadProfile> GetSubstationLoadProfiles(int[] dssIds, LoadProfileSource source)
        {
            var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m => m.DistributionSubstation.Id.IsIn(dssIds)).
                And(m=>m.Source==source);

            return q.List();
        }

        public IList<SubstationLoadProfile> GetSubstationLoadProfiles(LoadProfileType type, LoadProfileSource source)
        {
            var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m => m.Type == type).
                And(m=>m.Source == source).
                Fetch(SelectMode.Fetch,m=>m.DistributionSubstation);

            return q.List();
        }

        public IList<SubstationLoadProfile> GetPrimarySubstationLoadProfiles(int id, LoadProfileSource source, int year,
                ICarbonIntensityFetcher? carbonFetcher=null,
                IElectricityCostFetcher? costFetcher=null
                )
        {
            var q = getPrimarySubstationQuery(id,source);
            // This is the default load profile
            var dlp = q.Take(1).SingleOrDefault();
            //
            IList<SubstationLoadProfile> list;
            if ( dlp!=null ) {
                string sql;
                if (  source == LoadProfileSource.LV_Spreadsheet ) {
                    sql = getPrimarySubstationSQLQuery(id, source, dlp);  
                } else {
                    sql = getPrimarySubstationSQLQuery(id, source, year, dlp);
                }
                //
                var objs = Session.CreateSQLQuery(sql).List<object>();
                // Create a list of load profiles
                list = getLoadProfiles(objs,dlp, carbonFetcher, costFetcher);
            } else {
                list= new List<SubstationLoadProfile>();
            }
            return list;
        }

        IQueryOver<SubstationLoadProfile,SubstationLoadProfile> getPrimarySubstationQuery(
            int pssId, LoadProfileSource source) {
                var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m => m.PrimarySubstation.Id == pssId).
                And(m=>m.Source == source);
            return q;
        }

        public IList<SubstationLoadProfile> GetAllGeographicalAreaLoadProfiles(int gaId, 
                        LoadProfileSource source, 
                        int year)
        {
            //
            // Always return 2016 when source is spreadsheet
            if ( source==LoadProfileSource.LV_Spreadsheet) {
                year = 2016;
            }
            var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m=>m.Source == source).
                Where(m=>m.Year == year).            
                Where(m=>m.GeographicalArea.Id == gaId);

            var list = q.List();
            return list;
        }

        public IList<SubstationLoadProfile> GetGeographicalAreaLoadProfiles(int gaId, 
                        LoadProfileSource source, 
                        int year,
                        ICarbonIntensityFetcher? carbonFetcher=null,
                        IElectricityCostFetcher? costFetcher=null
                        )
        {
            //
            var q = getGeographicalAreaQuery(gaId, source);
            
            // This is the default load profile
            var dlp = q.Take(1).SingleOrDefault();
            //
            IList<SubstationLoadProfile> list;
            if ( dlp!=null ) {
                string sql;
                if ( source == LoadProfileSource.LV_Spreadsheet) {
                    sql = getGeographicalAreaSQLQuery(gaId, source, dlp);
                } else {
                    sql = getGeographicalAreaSQLQuery(gaId, source, year, dlp);
                }
                //
                var objs=Session.CreateSQLQuery(sql).List<object>();
                // Get aggregated load profiles by day and month
                list = getLoadProfiles(objs,dlp, carbonFetcher, costFetcher);
            } else {
                list= new List<SubstationLoadProfile>();
            }
            return list;
        }
        public IList<SubstationLoadProfile> GetGridSupplyPointLoadProfiles(int gspId, 
                        LoadProfileSource source, 
                        int year,
                        ICarbonIntensityFetcher? carbonFetcher=null,
                        IElectricityCostFetcher? costFetcher=null
                        )
        {
            //
            var q = getGridSupplyPointQuery(gspId, source);
            
            // This is the default load profile
            var dlp = q.Take(1).SingleOrDefault();
            //
            IList<SubstationLoadProfile> list;
            if ( dlp!=null ) {
                string sql;
                if ( source==LoadProfileSource.LV_Spreadsheet ) {
                    sql = getGridSupplyPointSQLQuery(gspId, source, dlp);
                } else {
                    sql = getGridSupplyPointSQLQuery(gspId, source, year, dlp);
                }
                //
                var objs=Session.CreateSQLQuery(sql).List<object>();
                // Get aggregated load profiles by day and month
                list = getLoadProfiles(objs,dlp, carbonFetcher, costFetcher);
            } else {
                list= new List<SubstationLoadProfile>();
            }
            return list;
        }

        private string getPrimarySubstationSQLQuery(int pssId, LoadProfileSource source, SubstationLoadProfile slp) {
            int nData = slp.Data.Length;
            string sql="select slp.day,slp.monthnumber,\n";
            int i;
            for(i=1;i<nData;i++) {
                sql+=$"sum(data[{i}]) as data{i},\n";
            }
            sql+=$"sum(data[{i}]) as data{i}\n";
            sql+="from substation_load_profiles slp\n";
            sql+=$"where primarysubstationid={pssId} and slp.\"source\"={(int)source} group by slp.day,slp.monthnumber";
            //
            return sql;
        }

        private string getPrimarySubstationSQLQuery(int pssId, LoadProfileSource source, int year, SubstationLoadProfile slp) {
            int yearOffset = year - slp.Year + 1;
            int nData = slp.Data.Length;
            string sql=$"select slp.day,slp.monthnumber,sum(scalingfactors[{yearOffset}]) as devicecount,\n";
            int i;
            for(i=1;i<nData;i++) {
                sql+=$"sum(data[{i}]*scalingfactors[{yearOffset}]) as data{i},\n";
            }
            sql+=$"sum(data[{i}]*scalingfactors[{yearOffset}]) as data{i}\n";
            sql+="from substation_load_profiles slp\n";
            sql+=$"where primarysubstationid={pssId} and slp.\"source\"={(int)source} group by slp.day,slp.monthnumber";
            //
            return sql;
        }

        private string getGeographicalAreaSQLQuery(int gaId, LoadProfileSource source, SubstationLoadProfile slp) {
            int nData = slp.Data.Length;
            string sql="select slp.day,slp.monthnumber,\n";
            int i;
            for(i=1;i<nData;i++) {
                sql+=$"sum(data[{i}]) as data{i},\n";
            }
            sql+=$"sum(data[{i}]) as data{i}\n";
            sql+="from substation_load_profiles slp\n";
            sql+=$"where geographicalareaid={gaId} and slp.\"source\"={(int)source} group by slp.day,slp.monthnumber";
            //
            return sql;
        }

        private string getGeographicalAreaSQLQuery(int gaId, LoadProfileSource source, int year, SubstationLoadProfile slp) {
            int yearOffset = year - slp.Year + 1;
            int nData = slp.Data.Length;
            string sql=$"select slp.day,slp.monthnumber,sum(scalingfactors[{yearOffset}]) as devicecount,\n";
            int i;
            for(i=1;i<nData;i++) {
                sql+=$"sum(data[{i}]*scalingfactors[{yearOffset}]) as data{i},\n";
            }
            sql+=$"sum(data[{i}]*scalingfactors[{yearOffset}]) as data{i}\n";
            sql+="from substation_load_profiles slp\n";
            sql+=$"where geographicalareaid={gaId} and slp.\"source\"={(int)source} group by slp.day,slp.monthnumber";
            //
            return sql;
        }

        private string getGridSupplyPointSQLQuery(int gspId, LoadProfileSource source, SubstationLoadProfile slp) {
            int nData = slp.Data.Length;
            string sql="select slp.day,slp.monthnumber,\n";
            int i;
            for(i=1;i<nData;i++) {
                sql+=$"sum(data[{i}]) as data{i},\n";
            }
            sql+=$"sum(data[{i}]) as data{i}\n";
            sql+="from substation_load_profiles slp\n";
            sql+=$"where slp.gridsupplypointid={gspId} and slp.\"source\"={(int)source} group by slp.day,slp.monthnumber";
            //
            return sql;
        }

        private string getGridSupplyPointSQLQuery(int gspId, LoadProfileSource source, int year, SubstationLoadProfile slp) {
            int yearOffset = year - slp.Year + 1;
            int nData = slp.Data.Length;
            string sql=$"select slp.day,slp.monthnumber,sum(scalingfactors[{yearOffset}]) as devicecount,\n";
            int i;
            for(i=1;i<nData;i++) {
                sql+=$"sum(data[{i}]*scalingfactors[{yearOffset}]) as data{i},\n";
            }
            sql+=$"sum(data[{i}]*scalingfactors[{yearOffset}]) as data{i}\n";
            sql+="from substation_load_profiles slp\n";
            sql+=$"where slp.gridsupplypointid={gspId} and slp.\"source\"={(int)source} group by slp.day,slp.monthnumber";
            //
            return sql;
        }

        IQueryOver<SubstationLoadProfile,SubstationLoadProfile> getGeographicalAreaQuery(
            int gaId, LoadProfileSource source) {
                var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m => m.GeographicalArea.Id == gaId).
                Where(m=>m.Source == source);
            return q;
        }

        IQueryOver<SubstationLoadProfile,SubstationLoadProfile> getGridSupplyPointQuery(
            int gspId, LoadProfileSource source) {
                var q = Session.QueryOver<SubstationLoadProfile>().
                Where(m=>m.GridSupplyPoint.Id==gspId).
                Where(m=>m.Source == source);
            return q;
        }

        IList<SubstationLoadProfile> getLoadProfiles( IList<object> objs, SubstationLoadProfile dlp, ICarbonIntensityFetcher? carbonFetcher, IElectricityCostFetcher? costFetcher ) {            
            //
            int dataOffset = (dlp.Source == LoadProfileSource.LV_Spreadsheet) ? 2 : 3;
            // Create a set of load profiles for each Day/month combination
            List<SubstationLoadProfile> list = new List<SubstationLoadProfile>();
            foreach( var obj in objs) {
                var objArray = (object[]) obj;
                var lp = new SubstationLoadProfile();
                //
                lp.Day=(Day) objArray[0];
                lp.MonthNumber=(int) objArray[1];
                lp.Year = dlp.Year;
                lp.Source = dlp.Source;
                lp.IntervalMins = dlp.IntervalMins;
                lp.Type = dlp.Type;
                //
                if ( dataOffset==3) {
                    lp.DeviceCount =  objArray[2]!=null?(double) objArray[2]:0;
                }
                int dataLength = objArray.Length-dataOffset;
                lp.Data = new double[dataLength];
                for(int i=0;i<dataLength;i++) {                    
                    lp.Data[i] = objArray[i+dataOffset]!=null ? (double) objArray[i+dataOffset] : 0;
                }
                if ( carbonFetcher!=null ) {
                    lp.AddCarbonData(carbonFetcher);
                }
                if ( costFetcher!=null ) {
                    lp.AddCostData(costFetcher);
                }
                list.Add(lp);
            }
            //
            return list;
        }

        public IList<SubstationLoadProfile> GetAllLoadProfilesByGridSupplyPoint(GridSupplyPoint gsp) {
            return Session.QueryOver<SubstationLoadProfile>().Where(m=>m.GridSupplyPoint==gsp).List();
        }
    }
}

