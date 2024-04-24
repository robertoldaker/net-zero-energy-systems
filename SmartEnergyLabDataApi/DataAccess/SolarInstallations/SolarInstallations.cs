using HaloSoft.DataAccess;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data;

public class SolarInstallations : DataSet {
    public SolarInstallations(DataAccess da) : base(da) {

    }

    public void Add(SolarInstallation si) {
        this.Session.Save(si);
    }

    public void Delete(SolarInstallation si) {
        this.Session.Delete(si);
    }

    public SolarInstallation GetSolarInstallation(int year, double lat, double lng) {
        GISData gisData = null;
        var q = Session.QueryOver<SolarInstallation>().Left.JoinAlias(m=>m.GISData,()=>gisData).
            Where( m=>m.Year == year).
            And(()=>gisData.Latitude == lat).
            And(()=>gisData.Longitude == lng);
        return q.Take(1).SingleOrDefault();
    }

    public IList<SolarInstallation> GetSolarInstallationsByGridSupplyPoint(int gspId, int year) {
        var q = Session.QueryOver<SolarInstallation>().
            Where( m=>m.GridSupplyPoint.Id == gspId).
            And(m=>m.Year<=year);
        return q.List();
    }

    public IList<SolarInstallation> GetSolarInstallationsByPrimarySubstation(int pssId, int year) {
        var q = Session.QueryOver<SolarInstallation>().
            Where( m=>m.PrimarySubstation.Id == pssId).
            And(m=>m.Year<=year);
        return q.List();
    }
    
    public IList<SolarInstallation> GetSolarInstallationsByDistributionSubstation(int dssId, int year) {
        var q = Session.QueryOver<SolarInstallation>().
            Where( m=>m.DistributionSubstation.Id == dssId).
            And(m=>m.Year<=year);
        return q.List();
    }
}