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
    [Route("LoadProfiles")]
    public class LoadProfilesController : ControllerBase
    {
        private ICarbonIntensityFetcher _carbonFetcher;
        private IElectricityCostFetcher _electricityCostFetcher;
        public LoadProfilesController(ICarbonIntensityFetcher carbonFetcher, IElectricityCostFetcher electricityCostFetcher)
        {
            _carbonFetcher = carbonFetcher;
            _electricityCostFetcher = electricityCostFetcher;
        }

        /// <summary>
        /// Gets distribution substation load profiles
        /// </summary>
        /// <param name="id">Primary substation id</param>
        /// <param name="source">Source of load profiles</param>
        /// <param name="year">Year for data</param>
        /// <returns></returns>
        [HttpGet]
        [Route("DistributionSubstationLoadProfiles")]
        public IList<SubstationLoadProfile> GetDistributionSubstationLoadProfiles(int id, LoadProfileSource source, int year)
        {
            using( var da = new DataAccess()) {
                return da.SubstationLoadProfiles.GetDistributionSubstationLoadProfiles(id, source, year, _carbonFetcher, _electricityCostFetcher);
            }
        }

        /// <summary>
        /// Gets primary substation load profiles
        /// </summary>
        /// <param name="id">Primary substation id</param>
        /// <param name="source">Load profile source</param>
        /// <param name="year">Year for data</param>
        /// <returns></returns>
        [HttpGet]
        [Route("PrimarySubstationLoadProfiles")]
        public IList<SubstationLoadProfile> GetPrimarySubstationLoadProfiles(int id, LoadProfileSource source, int year)
        {
            using( var da = new DataAccess()) {
                return da.SubstationLoadProfiles.GetPrimarySubstationLoadProfiles(id, source, year, _carbonFetcher, _electricityCostFetcher);
            }
        }

        /// <summary>
        /// Gets geographical area load profiles
        /// </summary>
        /// <param name="id">Geographical area id</param>
        /// <param name="source">Source of load profile</param>
        /// <param name="year">Year for data</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GeographicalAreaLoadProfiles")]
        public IList<SubstationLoadProfile> GetGeographicalAreaLoadProfiles(int id, LoadProfileSource source, int year)
        {
            using( var da = new DataAccess()) {
                return da.SubstationLoadProfiles.GetGeographicalAreaLoadProfiles(id, source, year, _carbonFetcher, _electricityCostFetcher);
            }
        }

        /// <summary>
        /// Gets grid supply point load profiles
        /// </summary>
        /// <param name="id">Grid supply point id</param>
        /// <param name="source">Source of load profile</param>
        /// <param name="year">Year for data</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GridSupplyPointLoadProfiles")]
        public IList<SubstationLoadProfile> GetGridSupplyPointLoadProfiles(int id, LoadProfileSource source, int year)
        {
            using( var da = new DataAccess()) {
                //?? hardwired to Melksham for time being
                if ( id==3) {
                    return da.SubstationLoadProfiles.GetGridSupplyPointLoadProfiles(id, source, year, _carbonFetcher, _electricityCostFetcher);
                } else {
                    return new List<SubstationLoadProfile>();
                }
            }
        }

        /// <summary>
        /// Get the current Carbon Intensity data
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("CarbonIntensity")]
        public CarbonIntensity GetCarbonIntensity() {
            return _carbonFetcher.Fetch();           
        }

        /// <summary>
        /// Get the current Electricity cost data
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ElectricityCost")]
        public ElectricityCost GetElectricityCost() {
            return _electricityCostFetcher.Fetch();           
        }

        /// <summary>
        /// Uploads EV load profile prediction data
        /// </summary>
        /// <param name="gaId">Id of geographical area</param>
        /// <param name="forecastsFile">Json file containing forecast data</param>
        /// <param name="profileFile">Json file containing profile data</param>
        [HttpPost]
        [Route("LoadEVData")]
        public string LoadEVData(int gaId, IFormFile forecastsFile, IFormFile profileFile) {
            using( var da = new DataAccess() ) {
                var loader = new EVDataLoader(da, gaId);
                loader.Load(forecastsFile, profileFile);
                da.CommitChanges();
                return $"Added [{loader.NumAdded}], updated[{loader.NumUpdated}]";
            }
        }

        /// <summary>
        /// Uploads HP load profile prediction data
        /// </summary>
        /// <param name="gaId">Id of geaographical area</param>
        /// <param name="file">Heat pump json file containing prediciotn data</param>
        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [Route("LoadHPData")]
        public string LoadHPData(int gaId, IFormFile file) {
            using( var da = new DataAccess() ) {
                var loader = new HPDataLoader(da, gaId);
                loader.Load(file);
                da.CommitChanges();
                return $"Added [{loader.NumAdded}], updated[{loader.NumUpdated}]";
            }
        }
    }

}