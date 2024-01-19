using Microsoft.AspNetCore.Mvc;
using EvDemandService.Models;
using CommonInterfaces.Models;

namespace EvDemandService.Controllers;

[ApiController]
[Route("/Predictor")]
public class PredictorController : ControllerBase
{
    /// <summary>
    /// Runs an EvModel prediction
    /// </summary>
    [HttpPost]
    [Route("Run")]
    public string RunPredictor([FromBody] EVDemandInput input)
    {
        return EVDemandRunner.Instance.RunPredictor(input);
    }

}