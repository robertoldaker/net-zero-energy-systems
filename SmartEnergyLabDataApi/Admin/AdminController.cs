using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.SGT;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EnergySystemLabDataApi.SubStations
{
    /// <summary>
    /// Api to support admin operations
    /// </summary>
    [ApiController]
    [Route("Admin")]
    public class AdminController : ControllerBase
    {
        public AdminController()
        {
        }

        /// <summary>
        /// Obtains current system log
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Logs")]
        public string Get()
        {
            return (new AdminModel()).LoadLogFile();
        }

        /// <summary>
        /// Returns system info for the server
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("SystemInfo")]
        public object SystemInfo() {
            return new { ProcessorCount=Environment.ProcessorCount };
        }

    }

}
