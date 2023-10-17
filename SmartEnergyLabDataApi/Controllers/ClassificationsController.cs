using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc;
using SmartEnergyLabDataApi.Data;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SmartEnergyLabDataApi.Controllers
{
    /// <summary>
    /// Api to generate and list substations.
    /// </summary>
    [ApiController]
    [Route("Classifications")]
    public class ClassificationsController : ControllerBase
    {
        public ClassificationsController()
        {

        }

        /// <summary>
        /// Gets distribution substation classifications
        /// </summary>
        /// <param name="id">Primary substation id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("DistributionSubstationClassifications")]
        public IList<SubstationClassification> GetDistributionSubstationClassifications(int id)
        {
            using( var da = new DataAccess()) {
                return da.Substations.GetDistributionSubstationClassifications(id);
            }
        }

        /// <summary>
        /// Returns classifications given a list of distribution ids
        /// </summary>
        /// <param name="dssIds">List of distribution substation ids separated by commas</param>
        /// <returns></returns>
        [HttpGet]
        [Route("DistributionSubstationClassifications/List")]
        public IList<SubstationClassification> GetDistributionSubstationClassifications(string dssIds) {
            var ids = dssIds.Split(',').Select(m=>int.Parse(m)).ToArray();
            using( var m = new DataAccess()) {
                return m.Substations.GetDistributionSubstationClassifications(ids);
            }
        }

        /// <summary>
        /// Gets primary substation classifications
        /// </summary>
        /// <param name="id">Primary substation id</param>
        /// <param name="aggregateResults">Sum results over all distribution substations</param>
        /// <returns></returns>
        [HttpGet]
        [Route("PrimarySubstationClassifications")]
        public IList<SubstationClassification> GetPrimarySubstationClassifications(int id, bool aggregateResults)
        {
            using( var da = new DataAccess()) {
                return da.SubstationClassifications.GetPrimarySubstationClassifications(id, aggregateResults);
            }
        }

        /// <summary>
        /// Gets geographical area clasifications
        /// </summary>
        /// <param name="id">Geographical area id</param>
        /// <param name="aggregateResults">Sub results over all substations</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GeographicalAreaClassifications")]
        public IList<SubstationClassification> GetClassifications(int id, bool aggregateResults)
        {
            using( var da = new DataAccess()) {
                return da.SubstationClassifications.GetGeographicalAreaClassifications(id, aggregateResults);
            }
        }
    }
}
