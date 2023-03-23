using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SmartEnergyLabDataApi.Controllers
{
    /// <summary>
    /// Api to generate and list substations.
    /// </summary>
    [ApiController]
    [Route("Substations")]
    public class SubstationsController : ControllerBase
    {
        public SubstationsController()
        {
        }

        /// <summary>
        /// Gets primary substations for a geographical area
        /// </summary>
        /// <param name="gaId">Geographical area id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("PrimarySubstationsByGeographicalAreaId")]
        public IEnumerable<PrimarySubstation> GetPrimarySubstationsByGeographicalAreaId(int gaId)
        {
            using( var da = new DataAccess()) {
                return da.Substations.GetPrimarySubstationsByGeographicalAreaId(gaId);
            }
        }

        /// <summary>
        /// Gets primary substations for a Grid Supply Point
        /// </summary>
        /// <param name="gspId">Grid Supply Point id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("PrimarySubstationsByGridSupplyPointId")]
        public IEnumerable<PrimarySubstation> GetPrimarySubstationsByGridSupplyPointId(int gspId)
        {
            using( var da = new DataAccess()) {
                return da.Substations.GetPrimarySubstationsByGridSupplyPointId(gspId);
            }
        }

        /// <summary>
        /// Gets all distribution substations associated with a primary substation
        /// </summary>
        /// <returns>DistributionSubstation</returns>
        [HttpGet]
        [Route("DistributionSubstations")]
        public IEnumerable<DistributionSubstation> GetDistributionSubstations(int primaryId)
        {
            using (var da = new DataAccess()) {
                return da.Substations.GetDistributionSubstations(primaryId);
            }
        }


        /// <summary>
        /// Gets a distribution substation by name
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DistributionSubstationByName")]
        public DistributionSubstation GetDistributionSubstationByName(string areaName, string substationName)
        {
            using (var da = new DataAccess()) {
                var ga = da.Organisations.GetGeographicalArea(areaName);
                if (ga == null) {
                    throw new Exception($"Cannot find geographical area [{areaName}]");
                }
                var dss = da.Substations.GetDistributionSubstation(ga, substationName);
                var jsonStr = JsonSerializer.Serialize(dss);
                return dss;
            }
        }

        /// <summary>
        /// Gets a distribution substation by database id
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DistributionSubstation")]
        public DistributionSubstation GetDistributionSubstation(int id)
        {
            using (var da = new DataAccess()) {
                var dss = da.Substations.GetDistributionSubstation(id);
                var jsonStr = JsonSerializer.Serialize(dss);
                return dss;
            }
        }

        /// <summary>
        /// Loads distribution substations from geoJson file
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("LoadDistributionStations")]
        [DisableRequestSizeLimit] 
        public IActionResult LoadDistributionStations(IFormFile file)
        {
            string msg = "";
            using (var da = new DataAccess()) {
                msg = da.Substations.LoadDistributionSubstations(file);
                da.CommitChanges();
            }
            return Content(msg);
        } 

        /// <summary>
        /// Uploads a file containing substation info
        /// </summary>
        /// 
        [HttpPost]
        [Route("LoadFromSpreadsheet")]
        public IActionResult LoadFromSpreadeheet(string geographicalAreaName, IFormFile file)
        {
            Logger.Instance.LogInfoEvent($"Started loading spreadheet for area [{geographicalAreaName}] from file [{file.FileName}]");
            using (var da = new DataAccess()) {
                da.Substations.LoadFromSpreadsheet(geographicalAreaName, file);
                da.CommitChanges();
                Logger.Instance.LogInfoEvent($"Ended loading spreadsheet data");
            }
            return Content("OK");
        }

        /// <summary>
        /// Uploads a file containing primary substation info
        /// </summary>
        /// 
        [HttpPost]
        [Route("LoadPrimarySubstationsFromSpreadsheet")]
        public IActionResult LoadPrimarySubstationsFromSpreadsheet(string geographicalAreaName, IFormFile file)
        {
            Logger.Instance.LogInfoEvent($"Started loading primary spreadsheet data for [{geographicalAreaName}] [{file.FileName}]");
            using (var da = new DataAccess()) {
                da.Substations.LoadPrimarySubstationsFromSpreadsheet(geographicalAreaName, file);
                da.CommitChanges();
                Logger.Instance.LogInfoEvent($"Ended loading primary spreadsheet data");
            }
            return Content("OK");
        }

        /// <summary>
        /// Loads primary substations from geoJson file
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("LoadPrimaryStations")]        
        public IActionResult LoadPrimaryStations(string geographicalAreaName, IFormFile file)
        {
            string msg = "";
            using (var da = new DataAccess()) {
                msg = da.Substations.LoadPrimarySubstations(geographicalAreaName, file);
                da.CommitChanges();
            }
            return Content(msg);
        } 

        /// <summary>
        /// Prints GeoJson data from the given file
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("PrintGeoJsonData")] 
        [DisableRequestSizeLimit]        
        public IActionResult PrintGeoJsonData(IFormFile file) {
            using (var da = new DataAccess()) {
                var gan = da.Organisations.GetGeographicalArea("Bath");

                var loader = new PrimarySubstationLoader(da, gan);
                loader.Print(file);
            }
            return this.Content("OK");
        }

        /// <summary>
        /// Auto fills missing GIS data using google maps api.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("AutoFillGISData")]        
        public IActionResult AutoFillGISData(string geographicalAreaName)
        {
            Logger.Instance.LogInfoEvent($"Started auto file of GIS data");
            using (var da = new DataAccess()) {
                da.Substations.AutoFillGISData(geographicalAreaName);
                da.CommitChanges();
                Logger.Instance.LogInfoEvent($"Ended auto file of GIS data");
            }
            return Content("OK");
        } 

        /// <summary>
        /// Search for substation
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Search")]        
        public IList<DistributionSubstation> Search(string str, int maxResults)
        {
            using (var da = new DataAccess()) {
                var results = da.Substations.Search(str, maxResults);
                return results;
            }
        } 
        
        /// <summary>
        /// Set substation params
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("SetSubstationParams")]        
        public void SetSubstationParams(int id, [FromBody] SubstationParams sParams)
        {
            using (var da = new DataAccess()) {
                da.Substations.SetSubstationParams(id, sParams);
                da.CommitChanges();
            }
        }         

        /// <summary>
        /// Set substation charging params
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("SetSubstationChargingParams")]        
        public void SetSubstationChargingParams(int id, [FromBody] SubstationChargingParams sParams)
        {
            using (var da = new DataAccess()) {
                da.Substations.SetSubstationChargingParams(id, sParams);
                da.CommitChanges();
            }
        }         

        /// <summary>
        /// Set substation charging params
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("SetSubstationHeatingParams")]        
        public void SetSubstationHeatingParams(int id, [FromBody] SubstationHeatingParams sParams)
        {
            using (var da = new DataAccess()) {
                da.Substations.SetSubstationHeatingParams(id, sParams);
                da.CommitChanges();
            }
        }         
    }
}
