using HaloSoft.DataAccess;
using NHibernate;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.BoundCalc;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data;

public class Elexon : DataSet {
    public Elexon(DataAccess da) : base(da)
    {

    }

    public DataAccess DataAccess
    {
        get {
            return (DataAccess)_dataAccess;
        }
    }

    #region GSPPeriodData
    public void Add(GspDemandProfileData obj)
    {
        Session.Save(obj);
    }

    public DateTime? GetLatestDate()
    {
        DateTime? date = Session.QueryOver<GspDemandProfileData>().
            OrderBy(m => m.Date).Desc.
            Select(m => m.Date).
            Take(1).SingleOrDefault<DateTime>();
        return date;
    }

    public IList<GspDemandProfileData> GetGspDemandProfilesWithNoLocation()
    {
        return Session.QueryOver<GspDemandProfileData>().Where(m => m.Location == null).List();
    }

    public IList<GspDemandProfileData> GetGspDemandProfiles(DateTime startDate, DateTime endDate, string? code=null)
    {
        var q = Session.QueryOver<GspDemandProfileData>().Where(m => m.Date >= startDate && m.Date <= endDate);
        if (!string.IsNullOrEmpty(code)) {
            q = q.Where(m => m.GspCode == code);
        }
        return q.Fetch(SelectMode.Fetch, m => m.Location).List();
    }

    public double[] GetTotalGspDemandProfile(DateTime startDate, string? gspGroupId = null)
    {
        var sql = getTotalGspDemandSQLQuery(startDate, gspGroupId);
        var objs = Session.CreateSQLQuery(sql).List<object>();
        if (objs.Count > 0) {
            var objArray = (object[])objs[0];
            double[] demand = new double[48];
            int i = 0;
            foreach (var obj in objArray) {
                demand[i++] = obj != null ? (double)obj : 0;
            }
            return demand;
        } else {
            return [];
        }
    }

    private string getTotalGspDemandSQLQuery(DateTime startDate, string? gspGroupId)
    {
        string date = getSqlDate(startDate);
        string sql = "select \n";
        int nData = 48;
        int i;
        for (i = 1; i < nData; i++) {
            sql += $"sum(demand[{i}]) as demand{i},\n";
        }
        sql += $"sum(demand[{i}]) as demand{i}\n";
        sql += "from gsp_demand_profile_data gsp\n";
        sql += $"where date = '{date}'";
        if (gspGroupId != null) {
            sql += $" and gspGroupId = '{gspGroupId}'";
        }
        //
        return sql;
    }

    private string getSqlDate(DateTime dt)
    {
        return $"{dt.Year}-{dt.Month:00}-{dt.Day:00}";
    }

    public IList<GridSubstationLocation> GetGspDemandLocations()
    {
        var locIds = Session.QueryOver<GspDemandProfileData>().
            Where( m=>m.Location!=null).
            Select(Projections.Distinct(Projections.Property<GspDemandProfileData>(m => m.Location.Id))).
            List<int>().ToArray();
        var locs = Session.QueryOver<GridSubstationLocation>().
            Fetch(SelectMode.Fetch, m => m.GISData).
            Where(m => m.Id.IsIn(locIds)).
            List();
        return locs;
    }

    public IList<DateTime> GetGspDemandDates()
    {
        var dates = Session.QueryOver<GspDemandProfileData>().
            OrderBy(m => m.Date).Asc.
            Select(Projections.Distinct(Projections.Property<GspDemandProfileData>(m => m.Date))).
            List<DateTime>();

        return dates;
    }

    public IList<string> GetGspDemandCodes()
    {
        var codes = Session.QueryOver<GspDemandProfileData>().
            Select(Projections.Distinct(Projections.Property<GspDemandProfileData>(m => m.GspCode))).
            List<string>();

        return codes;
    }

    #endregion

}
