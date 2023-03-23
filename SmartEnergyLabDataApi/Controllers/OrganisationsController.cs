using Microsoft.AspNetCore.Mvc;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Controllers
{
    /// <summary>
    /// Api to generate and list substations.
    /// </summary>
    [ApiController]
    [Route("Organisations")]
    public class OrganisationsController : ControllerBase
    {
        public OrganisationsController() {

        }
        
        /// <summary>
        /// Gets geographical area
        /// </summary>
        /// <param name="areaName">Geographical area name</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GeographicalArea")]
        public GeographicalArea GetGeographicalArea(string areaName)
        {
            using( var da = new DataAccess()) {
                var ga = da.Organisations.GetGeographicalArea(areaName);
                if ( ga==null) {
                    throw new Exception($"Cannot find geographical area [{areaName}]");
                }
                return ga;
            }
        }

        /// <summary>
        /// Autofills GIS data for a geographical area
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("AutoFillGISData")]
        public void AutoFillGISData(string geoName)
        {
            using( var da = new DataAccess()) {
                da.Organisations.AutoFillGISData(geoName);
                da.CommitChanges();
            }
        }

    }
}

