using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;
using static SmartEnergyLabDataApi.Models.GoogleMapsGISFinder;
using System.Text.RegularExpressions;
using static SmartEnergyLabDataApi.Models.NationalGridNetworkLoader;

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
        public void LoadNetwork(NationalGridNetworkSource source) {
            try {
                var m = new NationalGridNetworkLoader();
                m.Load(source);
            } catch ( Exception e) {                
                Logger.Instance.LogErrorEvent($"Error: loading national grid network [{e.Message}]");
                throw;
            }
        }

        /// <summary>
        /// Loads network data from NationalGrid's website
        /// </summary>
        [HttpPost]        
        [Route("DeleteNetwork")]
        public void DeleteNetwork(GridSubstationLocationSource source) {
            try {
                var m = new NationalGridNetworkLoader();
                m.Delete(source);
            } catch ( Exception e) {                
                Logger.Instance.LogErrorEvent($"Error: loading national grid network [{e.Message}]");
                throw;
            }
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
        /// Deletes substation locations
        /// </summary>
        [HttpPost]        
        [Route("SubstationLocations/Delete")]
        public void DeleteLocations(GridSubstationLocationSource source=GridSubstationLocationSource.GoogleMaps) {
            using( var da = new DataAccess()) {
                da.NationalGrid.DeleteLocations(source);
                da.CommitChanges();
            }
        }

        /// <summary>
        /// Looksup places using google places api
        /// </summary>
        [HttpGet]        
        [Route("GoogleMapsLookup")]
        public PlaceLookupContainer GoogleMapsLookup(string text) {
            var gmaps = new GoogleMapsGISFinder();
            var response = gmaps.PlaceLookup(text);
            return response;
        }

        /// <summary>
        /// Looksup places using google places api
        /// </summary>
        [HttpGet]        
        [Route("GooglePlacesLookup")]
        public TextSearch GooglePlacesLookup(string searchText) {
            var gmaps = new GoogleMapsGISFinder();
            var response = gmaps.TextSearchNew(searchText);
            return response;
        }

        /// <summary>
        /// Update location with given reference using string [lat],[lng]
        /// </summary>
        [HttpGet]        
        [Route("SubstationLocation/Update")]
        public void Update(string reference, string location) {
            using( var da = new DataAccess()) {
                var loc = da.NationalGrid.GetGridSubstationLocation(reference);
                if ( loc!=null ) {
                    var cpnts = location.Split(",");
                    if ( cpnts.Length==2) {
                        var lat = double.Parse(cpnts[0]);
                        var lng = double.Parse(cpnts[1]);
                        loc.GISData.Latitude = lat;
                        loc.GISData.Longitude = lng;
                        da.CommitChanges();
                    } else {
                        throw new Exception($"Could not find lat/long from string [{location}]");
                    }
                } else {
                    throw new Exception($"Could not find location with reference [{reference}]");
                }
            }
        }

        /// <summary>
        /// Deletes location with given reference
        /// </summary>
        [HttpPost]        
        [Route("SubstationLocation/Delete")]
        public void Delete(string reference) {
            using( var da = new DataAccess()) {
                var loc = da.NationalGrid.GetGridSubstationLocation(reference);
                if ( loc!=null ) {
                    da.NationalGrid.Delete(loc);
                    da.CommitChanges();
                } else {
                    throw new Exception($"Could not find location with reference [{reference}]");
                }
            }
        }
    }
}