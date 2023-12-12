using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NHibernate.Util;
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

        /// <summary>
        /// Returns boundaries for the given GISData id
        /// </summary>
        /// <param name="gisDataId">GISData id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Boundaries")]
        public IList<GISBoundary> GetBoundaries(int gisDataId) {
            using( var m = new GISModel(this)) {
                return m.GetBoundaries(gisDataId);
            }
        }

        /// <summary>
        /// Returns boundaries given a list of GISData ids
        /// </summary>
        /// <param name="gisDataIds">List of GISData ids separated by commas</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Boundaries/List")]
        public IList<GISBoundary> GetBoundaries(string gisDataIds) {
            var ids = gisDataIds.Split(',').Select(m=>int.Parse(m)).ToArray();
            using( var m = new GISModel(this)) {
                return m.GetBoundaries(ids);
            }
        }

                /// <summary>
        /// Gets the number of multi-boundaries by substation type
        /// </summary>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("MultiBoundaries")]
        public Dictionary<string,Tuple<int,int>> GetNumberOfMultiBoundaries() {
            var results = new Dictionary<string,Tuple<int,int>>();
            using( var da = new DataAccess()) {
                results.Add("Distribution",da.GIS.GetNumMultiBoundariesDist());
                results.Add("Primary",da.GIS.GetNumMultiBoundariesPrimary());
                results.Add("GSP",da.GIS.GetNumMultiBoundariesGSP());
            }
            return results;
        }

    }
}