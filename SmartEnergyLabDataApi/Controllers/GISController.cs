using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Controllers
{
    [Route("GIS")]
    [ApiController]
    public class GISController : ControllerBase
    {
        IHubContext<NotificationHub> _hubContext;
        public GISController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpGet]
        [Route("Boundaries")]
        public IList<GISBoundary> GetBoundaries(int gisDataId) {
            using( var m = new GISModel(this)) {
                return m.GetBoundaries(gisDataId);
            }
        }


    }
}