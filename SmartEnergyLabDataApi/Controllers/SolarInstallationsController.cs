using Microsoft.AspNetCore.Mvc;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;
using System.Text.Json;

namespace SmartEnergyLabDataApi.Controllers;

/// <summary>
/// Api to manage solar installations
/// </summary>
[ApiController]
[Route("SolarInstallations")]
public class SolarInstallationsController : ControllerBase
{
    public SolarInstallationsController()
    {

    }

    /// <summary>
    /// Loads solar installations from .json file
    /// </summary>
    /// <param name="file"></param>
    [HttpPost]
    [Route("LoadFromGeoJson")]
    [DisableRequestSizeLimit] 
    public IActionResult LoadFromGeoJson(IFormFile file ) {
        var m = new SolarInstallationLoader();
        var msg = m.Load(file);
        return this.Ok(msg);
    }

    /// <summary>
    /// Gets solar installations connected to the specified GSP installed on or before the given year
    /// </summary>
    /// <param name="gspId">Id of grid supply point</param>
    /// <param name="year">Installed on or before given year</param>
    [HttpGet]
    [Route("SolarInstallationsByGridSupplyPoint")]
    public IList<SolarInstallation> GetByGridSupplyPoint( int gspId, int year ) {
        using( var da = new DataAccess() ) {
            return da.SolarInstallations.GetSolarInstallationsByGridSupplyPoint(gspId,year);
        }
    }

    /// <summary>
    /// Gets solar installations connected to the specified Primary substation installed on or before the given year
    /// </summary>
    /// <param name="pssId">Id of primary substation</param>
    /// <param name="year">Installed on or before given year</param>
    [HttpGet]
    [Route("SolarInstallationsByPrimarySubstation")]
    public IList<SolarInstallation> GetByPrimarySubstation( int pssId, int year ) {
        using( var da = new DataAccess() ) {
            return da.SolarInstallations.GetSolarInstallationsByPrimarySubstation(pssId,year);
        }
    }

    /// <summary>
    /// Gets solar installations connected to the specified distribution substation installed on or before the given year
    /// </summary>
    /// <param name="dssId">Id of distribution substation</param>
    /// <param name="year">Installed on or before given year</param>
    [HttpGet]
    [Route("SolarInstallationsByDistributionSubstation")]
    public IList<SolarInstallation> GetByDistributionSubstation( int dssId, int year ) {
        using( var da = new DataAccess() ) {
            return da.SolarInstallations.GetSolarInstallationsByDistributionSubstation(dssId,year);
        }
    }
}
