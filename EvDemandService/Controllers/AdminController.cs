using Microsoft.AspNetCore.Mvc;
using EvDemandService.Models;

namespace EvDemandService.Controllers;

[ApiController]
[Route("/Admin")]
public class AdminController : ControllerBase
{
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

    [HttpGet]
    [Route("Logs")]
    public LogData Logs()
    {
        return AdminModel.Instance.LoadLogFile();
    }


}
    