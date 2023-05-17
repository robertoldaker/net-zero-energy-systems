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

        public GridSupplyPoint GetGridSupplyPointByNrAndName(string nr, string name) {
            return Session.QueryOver<GridSupplyPoint>().Where( m=>m.NR == nr || m.Name == name).Take(1).SingleOrDefault();
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
            return Session.QueryOver<GridSupplyPoint>().
                    Fetch(SelectMode.Fetch,m=>m.GISData).
                    List();
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
    }


}