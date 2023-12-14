using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;
using SmartEnergyLabDataApi.Elsi;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace SmartEnergyLabDataApi.Controllers;

[Route("EVDemand")]
[ApiController]
public class EVDemandController : ControllerBase
{
    private EVDemandBackgroundTask _backgroundTask;

    public EVDemandController(IBackgroundTasks backgroundTasks) {
        _backgroundTask = backgroundTasks.GetTask<EVDemandBackgroundTask>(EVDemandBackgroundTask.Id);
    }

    [HttpGet]
    [Route("Download/DistributionSubstation")]
    public ActionResult DownloadDistributionSubstation(int id) 
    {
        using( var da = new DataAccess()) {
            var dss = da.Substations.GetDistributionSubstation(id);
            if ( dss!=null) {
                var edi = EVDemandInput.CreateFromDistributionId(id);
                var json=JsonSerializer.Serialize(edi);
                var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                var fsr = new FileStreamResult(ms, "application/json");
                fsr.FileDownloadName = $"EvDemandInput (dist)({dss.Name}).json";
                return fsr;
            } else {
                throw new Exception($"Cannot find distribution substation with is=[{id}]");
            }
        }
    }

    [HttpGet]
    [Route("Download/PrimarySubstation")]
    public ActionResult DownloadPrimarySubstation(int id) 
    {
        using( var da = new DataAccess()) {
            var dss = da.Substations.GetPrimarySubstation(id);
            if ( dss!=null) {
                var edi = EVDemandInput.CreateFromPrimaryId(id);
                var json=JsonSerializer.Serialize(edi);
                var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                var fsr = new FileStreamResult(ms, "application/json");
                fsr.FileDownloadName = $"EvDemandInput (primary)({dss.Name}).json";
                return fsr;
            } else {
                throw new Exception($"Cannot find primary substation with is=[{id}]");
            }
        }
    }

    [HttpGet]
    [Route("Download/GridSupplyPoint")]
    public ActionResult DownloadGridSupplyPoint(int id) 
    {
        using( var da = new DataAccess()) {
            var dss = da.SupplyPoints.GetGridSupplyPoint(id);
            if ( dss!=null) {
                var edi = EVDemandInput.CreateFromGridSupplyPointId(id);
                var json=JsonSerializer.Serialize(edi);
                var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                var fsr = new FileStreamResult(ms, "application/json");
                fsr.FileDownloadName = $"EvDemandInput (GSP)({dss.Name}).json";
                return fsr;
            } else {
                throw new Exception($"Cannot find distribution substation with is=[{id}]");
            }
        }
    }

    /// <summary>
    /// Runs a EV Demand prediction for the specified distribution substation
    /// </summary>
    /// <param>Db id of the substation</param>
    [HttpPost]
    [Route("Run/DistributionSubstation")]
    public void RunDistributionSubstation(int id)
    {
        _backgroundTask.RunDistributionSubstation(id);
    }

    /// <summary>
    /// Runs a EV Demand prediction for the specified primary substation
    /// </summary>
    /// <param>Db id of the substation</param>
    [HttpPost]
    [Route("Run/PrimarySubstation")]
    public void RunPrimarySubstation(int id)
    {
        _backgroundTask.RunPrimarySubstation(id);
    }    

    /// <summary>
    /// Runs a EV Demand prediction for the specified primary substation
    /// </summary>
    /// <param>Db id of the supply point</param>
    [HttpPost]
    [Route("Run/GridSupplyPoint")]
    public void RunGridSupplyPoint(int id)
    {
        _backgroundTask.RunGridSupplyPoint(id);
    }    

    /// <summary>
    /// Gets the current status of the EV demand tool
    /// </summary>
    [HttpGet]
    [Route("Status")]
    public object GetStatus()
    {
        return EVDemandRunner.Instance.GetStatus();
    }    

    /// <summary>
    /// Restarts EV demand tool
    /// </summary>
    [HttpGet]
    [Route("Restart")]
    public void Restart()
    {
        EVDemandRunner.Instance.Restart();
    }    
}
