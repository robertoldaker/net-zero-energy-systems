using HaloSoft.DataAccess;
using NHibernate;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.BoundCalc;

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

    public IList<GspDemandProfileData> GetGspDemandProfiles(DateTime startDate, DateTime endDate, string code)
    {
        return Session.QueryOver<GspDemandProfileData>().
            Where(m => m.GspCode == code).
            Where(m => m.Date >= startDate && m.Date <= endDate).
            Fetch(SelectMode.Fetch,m=>m.Location).
            List();
    }

    public double[] GetTotalGspDemandProfiles(DateTime startDate, string? gspGroupId = null)
    {
        var sql = getTotalGspDemandSQLQuery(startDate, gspGroupId);
        var objs = Session.CreateSQLQuery(sql).List<object>();
        if (objs.Count > 0) {
            /*object[] demandObjs = (object[])objs[0];
            double[] demand = new double[demandObjs.Length];
            int i = 0;
            foreach (var d in demandObjs) {
                demand[i++] = (double)d;
            }*/
            double[] demand = ((object[])objs[0]).Cast<double>().ToArray();
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


    #endregion

}
