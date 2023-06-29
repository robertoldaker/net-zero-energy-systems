using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Web;

namespace SmartEnergyLabDataApi.Data
{
    public class SupplyPoints : DataSet
    {
        public SupplyPoints(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
        }

        public void Add(GridSupplyPoint gsp)
        {
            Session.Save(gsp);
        }

        public GridSupplyPoint GetGridSupplyPoint(int id) {
            return Session.Get<GridSupplyPoint>(id);
        }

        public GridSupplyPoint GetGridSupplyPointByNRId(string nrId) {
            return Session.QueryOver<GridSupplyPoint>().Where( m=>m.NRId == nrId).Take(1).SingleOrDefault();
        }

        public GridSupplyPoint GetGridSupplyPointByNrOrName(string nr, string name) {
            var gsp=Session.QueryOver<GridSupplyPoint>().Where( m=>m.NR == nr).Take(1).SingleOrDefault();
            if ( gsp!=null) {
                return gsp;
            } else {
                return Session.QueryOver<GridSupplyPoint>().Where( m=>m.Name == name).Take(1).SingleOrDefault();
            }
        }

        public GridSupplyPoint GetGridSupplyPoint(ImportSource source, string externalId, string externalId2=null, string name=null) {
            GridSupplyPoint gsp=null;
            if ( externalId!=null) {
                gsp = Session.QueryOver<GridSupplyPoint>().Where( m=>m.Source == source && m.ExternalId == externalId).Take(1).SingleOrDefault();
            }
            if ( gsp==null && externalId2!=null ) {
                gsp = Session.QueryOver<GridSupplyPoint>().Where( m=>m.Source == source && m.ExternalId2 == externalId2).Take(1).SingleOrDefault();
            }
            if ( gsp==null && name!=null ) {
                gsp = Session.QueryOver<GridSupplyPoint>().Where( m=>m.Source == source && m.Name == name).Take(1).SingleOrDefault();
            }
            return gsp;
        }

        public GridSupplyPoint GetGridSupplyPointByName(string name) {
            return Session.QueryOver<GridSupplyPoint>().Where( m=>m.Name == name).Take(1).SingleOrDefault();
        }

        public GridSupplyPoint GetGridSupplyPoint(GeographicalArea ga, string name) {
            return Session.QueryOver<GridSupplyPoint>().Where( m=>m.Name.IsInsensitiveLike(name) && m.GeographicalArea == ga).Take(1).SingleOrDefault();
        }

        public IList<GridSupplyPoint> GetGridSupplyPoints(int gaId) {
            return Session.QueryOver<GridSupplyPoint>().
                    Fetch(SelectMode.Fetch,m=>m.GISData).
                    Where( m=>m.GeographicalArea.Id == gaId).
                    List();
        }

        public IList<GridSupplyPoint> GetGridSupplyPoints() {
            var q = Session.QueryOver<GridSupplyPoint>().Fetch(SelectMode.Fetch,m=>m.GISData);
            return q.List();
        }

        public IList<GridSupplyPoint> GetGridSupplyPoints(DistributionNetworkOperator dno) {
            return Session.QueryOver<GridSupplyPoint>().Where( m=>m.DistributionNetworkOperator == dno).List();
        }

        public string LoadGridSupplyPointsFromGeoJson(string geographicalAreaName, IFormFile file) {

            var gan = DataAccess.Organisations.GetGeographicalArea(geographicalAreaName);
            if (gan == null) {
                throw new Exception($"Unknow geographical area [{geographicalAreaName}]");
            }
            var loader = new GridSupplyPointLoader(DataAccess, gan);
            return loader.Load(file);
        }

        public int GetNumGridSupplyPoints(int gaId) {
            var num = Session.QueryOver<GridSupplyPoint>().Where( m=>m.GeographicalArea.Id == gaId).RowCount();
            return num;
        }
    }


}