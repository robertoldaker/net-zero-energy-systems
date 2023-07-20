using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Controllers
{
    [Route("NationalGrid")]
    [ApiController]
    public class NationalGridController : ControllerBase
    {
        IHubContext<NotificationHub> _hubContext;
        public NationalGridController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Loads network data from NationalGrid's website
        /// </summary>
        [HttpPost]        
        [Route("LoadNetwork")]
        public void LoadNetwork() {
            var m = new NationalGridNetworkLoader();
            m.Load();
        }

        /// <summary>
        /// Gets substations
        /// </summary>
        [HttpGet]        
        [Route("Substations")]
        public IList<GridSubstation> GetSubstations() {
            using( var da = new DataAccess()) {
                return da.NationalGrid.GetGridSubstations();
            }
        }

        /// <summary>
        /// Gets loadflow substations
        /// </summary>
        [HttpGet]        
        [Route("Loadflow/GridSubstations")]
        public IList<GridSubstation> GetLoadflowGridSubstations() {
            using( var da = new DataAccess()) {
                return da.NationalGrid.GetLoadflowGridSubstations();
            }
        }
    }
}