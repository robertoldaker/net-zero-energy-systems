
using HaloSoft.DataAccess;
using SmartEnergyLabDataApi.Models;
using NHibernate.Criterion;
using NHibernate;

namespace SmartEnergyLabDataApi.Data
{
    public class VehicleCharging : DataSet
    {
        public VehicleCharging(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
        }

        public void ImportFromOpenChargeMap(string geographicalAreaName, double radiusInKm) {
            var ga = DataAccess.Organisations.GetGeographicalArea(geographicalAreaName);
            if ( ga==null) {
                throw new Exception($"Could not find geographical area with name [{geographicalAreaName}]");
            }
            var loader = new OpenChargeMapLoader(DataAccess, ga);
            loader.Load(ga.GISData.Latitude, ga.GISData.Longitude, radiusInKm);
        }

        #region Vehicle charging stations
        public void Add(VehicleChargingStation vcs)
        {
            Session.Save(vcs);
        }
        
        public void Delete(VehicleChargingStation vcs)
        {
            Session.Delete(vcs);
        }

        public IList<VehicleChargingStation> GetVehicleChargingStations(int gaId) {
            PrimarySubstation pss = null;
            var q = Session.QueryOver<VehicleChargingStation>().
                JoinAlias(m=>m.PrimarySubstation,()=>pss).
                Where(()=>pss.GeographicalArea.Id == gaId);
        
            // List of charging stations
            var css = q.List();

            // Now link in the array of connections
            var ids = css.Select(m=>m.Id).ToArray<int>();
            var cons = Session.QueryOver<VehicleChargingConnection>().
                Where(m=>m.VehicleChargingStation.Id.IsIn(ids)).
                List();
            foreach( var cs in css) {
                cs.Connections = cons.Where(m=>m.VehicleChargingStation.Id == cs.Id).ToList();
            }
            return css;
        }

        #endregion

        #region Vehicle charging connection
        public void Add(VehicleChargingConnection vcc)
        {
            Session.Save(vcc);
        }

        public void Delete(VehicleChargingConnection vcc)
        {
            Session.Delete(vcc);
        }
        #endregion

        #region Vehicle charging connection type
        public void Add(VehicleChargingConnectionType vcct) {
            Session.Save(vcct);
        }
        public void Delete(VehicleChargingConnectionType vcct)
        {
            Session.Delete(vcct);
        }

        public IList<VehicleChargingConnectionType> GetVehicleChargingConnectionTypes() {
            var q = Session.QueryOver<VehicleChargingConnectionType>();
            return q.List();
        }
        #endregion

        #region Vehicle charging current type
        public void Add(VehicleChargingCurrentType vcct) {
            Session.Save(vcct);
        }
        public void Delete(VehicleChargingCurrentType vcct)
        {
            Session.Delete(vcct);
        }

        public IList<VehicleChargingCurrentType> GetVehicleChargingCurrentTypes() {
            var q = Session.QueryOver<VehicleChargingCurrentType>();
            return q.List();
        }
        #endregion

    }
}