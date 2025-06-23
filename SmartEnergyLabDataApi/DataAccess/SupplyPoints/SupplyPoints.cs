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

        public void Delete(GridSupplyPoint gsp)
        {
            Session.Delete(gsp);
        }

        public void DeleteAllGridSupplyPointsInGeographicalArea(int gaId) {
            Logger.Instance.LogInfoEvent($"Started deletion of grid supply points for geographical area=[{gaId}]");
            var gsps = GetGridSupplyPoints(gaId);
            foreach( var gsp in gsps) {
                Session.Delete(gsp);
            }
            Logger.Instance.LogInfoEvent($"Finished deletion of grid supply points");
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

        public GridSupplyPoint GetGridSupplyPointLike(string name) {
            return Session.QueryOver<GridSupplyPoint>().Where( m=>m.Name.IsInsensitiveLike(name,MatchMode.Exact)).Take(1).SingleOrDefault();
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
            var q = Session.QueryOver<GridSupplyPoint>().
                Fetch(SelectMode.Fetch, m => m.GISData).
                Fetch(SelectMode.Fetch, m=> m.GeographicalArea);
            return q.List();
        }

        public IList<GridSupplyPoint> GetGridSupplyPoints(DistributionNetworkOperator dno) {
            return Session.QueryOver<GridSupplyPoint>().Where( m=>m.DistributionNetworkOperator == dno).List();
        }

        public int GetCustomersForGridSupplyPoint(int id) {
            // Get ids of primary substations attached to this grid supply point
            var pssIds = Session.QueryOver<PrimarySubstation>().Where(m=>m.GridSupplyPoint.Id==id).Select(m=>m.Id).List<int>().ToArray();
            // Get ids of distribution substations attached to this primary
            var dssIds = Session.QueryOver<DistributionSubstation>().Where(m=>m.PrimarySubstation.Id.IsIn(pssIds)).Select(m=>m.Id).List<int>().ToArray();
            // Find sum of number of customers
            var sum = Session.QueryOver<DistributionSubstationData>().Where(m=>m.DistributionSubstation.Id.IsIn(dssIds)).SelectList(l=>l.SelectSum(m=>m.NumCustomers)).List<int?>();
            if ( sum.Count>0 && sum[0]!=null) {
                return (int) sum[0];
            } else {
                return 0;
            }
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
