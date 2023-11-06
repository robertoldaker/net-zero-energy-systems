using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;
using SmartEnergyLabDataApi.Elsi;
using System.Runtime.CompilerServices;

namespace SmartEnergyLabDataApi.Controllers;

[Route("EVDemand")]
[ApiController]
public class EVDemandController : ControllerBase
{
    private EVDemandBackgroundTask _backgroundTask;

    public EVDemandController(IBackgroundTasks backgroundTasks) {
        _backgroundTask = backgroundTasks.GetTask<EVDemandBackgroundTask>(EVDemandBackgroundTask.Id);
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

}
