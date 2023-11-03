using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SmartEnergyLabDataApi.Controllers
{
    /// <summary>
    /// Api to generate and list supply points.
    /// </summary>
    [ApiController]
    [Route("SupplyPoints")]
    public class SupplyPointsController : ControllerBase
    {
        public SupplyPointsController()
        {

        }

        /// <summary>
        /// Gets all grid supply points
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GridSupplyPoints/All")]
        public IEnumerable<GridSupplyPoint> GetAllGridSupplyPoints()
        {
            using( var da = new DataAccess()) {
                return da.SupplyPoints.GetGridSupplyPoints();
            }
        }

        /// <summary>
        /// Gets grid supply points for a geographical area
        /// </summary>
        /// <param name="gaId">Geographical area id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GridSupplyPoints")]
        public IEnumerable<GridSupplyPoint> GetGridSupplyPoints(int gaId)
        {
            using( var da = new DataAccess()) {
                return da.SupplyPoints.GetGridSupplyPoints(gaId);
            }
        }

        /// <summary>
        /// Gets a grid supply point by name
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GridSupplyPointByName")]
        public GridSupplyPoint GetGridSupplyPointByName(string areaName, string name)
        {
            using (var da = new DataAccess()) {
                var ga = da.Organisations.GetGeographicalArea(areaName);
                if (ga == null) {
                    throw new Exception($"Cannot find geographical area [{areaName}]");
                }
                var dss = da.SupplyPoints.GetGridSupplyPoint(ga, name);
                var jsonStr = JsonSerializer.Serialize(dss);
                return dss;
            }
        }

        /// <summary>
        /// Gets a grid supply point by id
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GridSupplyPoint")]
        public GridSupplyPoint GetGridSupplyPoint(int id)
        {
            using (var da = new DataAccess()) {
                var dss = da.SupplyPoints.GetGridSupplyPoint(id);
                var jsonStr = JsonSerializer.Serialize(dss);
                return dss;
            }
        }

        /// <summary>
        /// Gets number of customers associated with a grid supply point
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GridSupplyPoint/Customers")]
        public int GetCustomerForGridSupplyPoint(int id)
        {
            using (var da = new DataAccess()) {
                var customers = da.SupplyPoints.GetCustomersForGridSupplyPoint(id);
                return customers;
            }
        }

        /// <summary>
        /// Loads a set of grid supply points from a geojson file
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("LoadGridSupplyPointsFromGeoJson")]        
        public IActionResult LoadGridSupplyPointsFromGeoJson(string geographicalAreaName, IFormFile file)
        {
            string msg = "";
            using (var da = new DataAccess()) {
                msg = da.SupplyPoints.LoadGridSupplyPointsFromGeoJson(geographicalAreaName, file);
                da.CommitChanges();
            }
            return Content(msg);
        }         
    }
}