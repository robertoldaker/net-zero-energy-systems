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
        /// <param name="id">Distribution substation id</param>
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
                return da.SubstationLoadProfiles.GetGridSupplyPointLoadProfiles(id, source, year, _carbonFetcher, _electricityCostFetcher);
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
        /// <param name="forecastsFile">Json file containing forecast data</param>
        /// <param name="profileFile">Json file containing profile data</param>
        [HttpPost]
        [Route("LoadEVData")]
        public string LoadEVData(IFormFile forecastsFile, IFormFile profileFile) {
            using( var da = new DataAccess() ) {
                var gspName = "Melksham  S.G.P.";
                var gsp = da.SupplyPoints.GetGridSupplyPointByName(gspName);
                if ( gsp!=null ) {
                    var loader = new EVDataLoader(da, gsp.Id);
                    loader.Load(forecastsFile, profileFile);
                    da.CommitChanges();
                    return $"Added [{loader.NumAdded}], updated[{loader.NumUpdated}]";
                } else {
                    throw new Exception($"Cannot find grid supply point [{gspName}]");
                }
            }
        }

        /// <summary>
        /// Uploads HP load profile prediction data
        /// </summary>
        /// <param name="file">Heat pump json file containing prediciotn data</param>
        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [Route("LoadHPData")]
        public string LoadHPData( IFormFile file) {
            using( var da = new DataAccess() ) {                
                var gspName = "Melksham  S.G.P.";
                var gsp = da.SupplyPoints.GetGridSupplyPointByName(gspName);
                if ( gsp!=null) {
                    var loader = new HPDataLoader(da, gsp.Id);
                    loader.Load(file);
                    da.CommitChanges();
                    return $"Added [{loader.NumAdded}], updated[{loader.NumUpdated}]";
                } else {
                    throw new Exception($"Cannot find grid supply point [{gspName}]");
                }
            }
        }

        /// <summary>
        /// Generates missing base load profile data for distribution substations without load profile data for a given type
        /// </summary>
        /// <param name="type">Type of load profile  (0-base, 1-EVs, 2- HPs)</param>
        [HttpPost]
        [Route("GenerateMissingForType")]
        public void GenerateMissingForType(LoadProfileType type) {
            var m = new LoadProfileGenerator();
            m.Generate(type);
        }

        /// <summary>
        /// Generates missing base load profile data for distribution substations without load profile data
        /// </summary>
        [HttpPost]
        [Route("GenerateMissing")]
        public void GenerateMissing() {
            var m = new LoadProfileGenerator();
            m.Generate(LoadProfileType.Base);
            m.Generate(LoadProfileType.EV);
            m.Generate(LoadProfileType.HP);
        }

        /// <summary>
        /// Clears dummy load profiles for a given type
        /// </summary>
        /// <param name="type">Type of load profile (0-base, 1-EVs, 2- HPs)</param>
        [HttpPost]
        [Route("ClearDummy")]
        public void ClearDummy(LoadProfileType type) {
            var m = new LoadProfileGenerator();
            m.ClearDummy(type);
        }

        /// <summary>
        /// Clears all dummy load profiles
        /// </summary>
        [HttpPost]
        [Route("ClearAllDummy")]
        public void ClearAllDummy() {
            var m = new LoadProfileGenerator();
            m.ClearAllDummy();
        }

        /// <summary>
        /// Finds closest matching dist. substation based on numCustomers and dayMaxDemand
        /// </summary>
        /// <param name="distId">Id of distribution substation</param>
        /// <param name="type">Type of load profile</param>
        [HttpGet]
        [Route("FindClosestDistributionSubstation")]
        public DistributionSubstation FindClosestDistributionSubstation(int distId, LoadProfileType type) {
            var m = new LoadProfileGenerator();
            return m.GetClosestProfileDistSubstation(distId, type);
        }
    }

}