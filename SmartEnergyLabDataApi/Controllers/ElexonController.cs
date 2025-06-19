using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Controllers;

[Route("Elexon")]
[ApiController]
public class ElexonController : ControllerBase {
    /// <summary>
    /// Imports GSP demand profiles from "https://www.elexon.co.uk/open-data/IPD_2025.zip"
    /// </summary>
    [HttpPost]
    [Route("ImportGspDemandProfiles")]
    public void ImportGspDemandProfiles()
    {
        var loader = new ElexonGspDemandProfileLoader();
        loader.Load();
    }

    /// <summary>
    /// Gets GSP demand profiles for a given start date, end date and 4-letter gspCode as JSON
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="gspCode">4-letter gspCode</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetGspDemandProfiles")]
    public IList<GspDemandProfileData> GetGspDemandProfiles(DateTime startDate, DateTime endDate, string? gspCode=null)
    {
        using (var da = new DataAccess()) {
            var profiles = da.Elexon.GetGspDemandProfiles(startDate, endDate, gspCode);
            return profiles;
        }
    }

    /// <summary>
    /// Gets total GSP demand profile for a given date and optional gspGroupId.
    /// If gspGroupId is null then demand profile for the whole of the uk is returned (total over all gsps)
    /// </summary>
    /// <param name="date">Date for the demand profile</param>
    /// <param name="gspGroupId">Leave blank for whole of UK or enter gspGroupId for gsp group only</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetTotalGspDemandProfile")]
    public double[] GetTotalGspDemandProfile(DateTime date, string? gspGroupId = null)
    {
        using (var da = new DataAccess()) {
            var profiles = da.Elexon.GetTotalGspDemandProfile(date, gspGroupId);
            return profiles;
        }
    }

    /// <summary>
    /// Gets a list of all locations that have Gsp demand profile data
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("GetGspDemandLocations")]
    public IList<GridSubstationLocation> GetGspDemandLocations()
    {
        using (var da = new DataAccess()) {
            var locs = da.Elexon.GetGspDemandLocations();
            return locs;
        }
    }
    /// <summary>
    /// Gets a list of all dates with Gsp demand location data
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("GetGspDemandDates")]
    public IList<DateTime> GetGspDemandDates()
    {
        using (var da = new DataAccess()) {
            var dates = da.Elexon.GetGspDemandDates();
            return dates;
        }
    }
}
