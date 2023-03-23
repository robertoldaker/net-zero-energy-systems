using Microsoft.AspNetCore.Mvc;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Controllers
{
        /// <summary>
    /// Api to manage vehicle chargers
    /// </summary>
    [ApiController]
    [Route("VehicleCharging")]
    public class VehicleChargingController : ControllerBase
    {
        /// <summary>
        /// Imports vehicle charging points from Open Charge Map for a given geographical area
        /// </summary>
        /// <param name="geographicalAreaName">Geographical area name</param>
        /// <param name="searchRadiusKm">Search radius from center of geographical area</param>
        [HttpGet]
        [Route("ImportFromOpenChargeMap")]        
        public void ImportFromOpenChargeMap(string geographicalAreaName, double searchRadiusKm) {
            using( var da = new DataAccess() ) {
                da.VehicleCharging.ImportFromOpenChargeMap(geographicalAreaName, searchRadiusKm);
                da.CommitChanges();
            }
        }

        /// <summary>
        /// Gets all of the vehicle charging stations associated with the geographical area
        /// </summary>
        /// <param name="gaId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("VehicleChargingStations")]        
        public IList<VehicleChargingStation> GetVehicleChargingStations(int gaId) {
            using ( var da = new DataAccess()) {
                var vcss = da.VehicleCharging.GetVehicleChargingStations(gaId);
                return vcss;
            }
        }
    }
}
