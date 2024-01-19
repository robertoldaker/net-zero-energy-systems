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
using CommonInterfaces.Models;
using CommonInterfaces.Clients;

namespace SmartEnergyLabDataApi.Controllers;

[Route("EVDemand")]
[ApiController]
public class EVDemandController : ControllerBase
{

    public EVDemandController() {
    }

    [HttpGet]
    [Route("Input/DistributionSubstation")]
    public EVDemandInput InputDistributionSubstation(int id) 
    {
        using( var da = new DataAccess()) {
            var dss = da.Substations.GetDistributionSubstation(id);
            if ( dss!=null) {
                var edi = EVDemandUtils.CreateFromDistributionId(id);
                return edi;
            } else {
                throw new Exception($"Cannot find distribution substation with is=[{id}]");
            }
        }
    }

    [HttpGet]
    [Route("Download/DistributionSubstation")]
    public ActionResult DownloadDistributionSubstation(int id) 
    {
        using( var da = new DataAccess()) {
            var dss = da.Substations.GetDistributionSubstation(id);
            if ( dss!=null) {
                var edi = EVDemandUtils.CreateFromDistributionId(id);
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
    [Route("Input/PrimarySubstation")]
    public EVDemandInput InputPrimarySubstation(int id) 
    {
        using( var da = new DataAccess()) {
            var dss = da.Substations.GetPrimarySubstation(id);
            if ( dss!=null) {
                var edi = EVDemandUtils.CreateFromPrimaryId(id);
                return edi;
            } else {
                throw new Exception($"Cannot find primary substation with is=[{id}]");
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
                var edi = EVDemandUtils.CreateFromPrimaryId(id);
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
    [Route("Input/GridSupplyPoint")]
    public EVDemandInput InputGridSupplyPoint(int id) 
    {
        using( var da = new DataAccess()) {
            var dss = da.SupplyPoints.GetGridSupplyPoint(id);
            if ( dss!=null) {
                var edi = EVDemandUtils.CreateFromGridSupplyPointId(id);
                return edi;
            } else {
                throw new Exception($"Cannot find distribution substation with is=[{id}]");
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
                var edi = EVDemandUtils.CreateFromGridSupplyPointId(id);
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
    public string RunDistributionSubstation(int id)
    {
        var m = new EVDemandClient();        
        var input  = this.InputDistributionSubstation(id);
        return m.Predictor.Run(input);
    }

    /// <summary>
    /// Runs a EV Demand prediction for the specified primary substation
    /// </summary>
    /// <param>Db id of the substation</param>
    [HttpPost]
    [Route("Run/PrimarySubstation")]
    public string RunPrimarySubstation(int id)
    {
        var m = new EVDemandClient();        
        var input  = this.InputPrimarySubstation(id);
        return m.Predictor.Run(input);
    }    

    /// <summary>
    /// Runs a EV Demand prediction for the specified primary substation
    /// </summary>
    /// <param>Db id of the supply point</param>
    [HttpPost]
    [Route("Run/GridSupplyPoint")]
    public string RunGridSupplyPoint(int id)
    {
        var m = new EVDemandClient();        
        var input  = this.InputGridSupplyPoint(id);
        return m.Predictor.Run(input);
    }    

    /// <summary>
    /// Gets the current status of the EV demand tool
    /// </summary>
    [HttpGet]
    [Route("Status")]
    public object GetStatus()
    {
        var m = new EVDemandClient();
        return m.Admin.Status();
    }    

    /// <summary>
    /// Restarts EV demand tool
    /// </summary>
    [HttpGet]
    [Route("Restart")]
    public void Restart()
    {
        var m = new EVDemandClient();
        m.Admin.Restart();
    }    
}
