using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;
using static SmartEnergyLabDataApi.Models.LoadflowReference;

namespace SmartEnergyLabDataApi.Controllers
{
    [Route("Loadflow")]
    [ApiController]
    public class LoadflowController : ControllerBase
    {
        IHubContext<NotificationHub> _hubContext;
        public LoadflowController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Loads Loadflow geometry data from NationalGrid's website
        /// </summary>
        [HttpPost]        
        [Route("LoadNodeGeometry")]
        public void LoadNodeGeometry() {
            var m = new LoadflowNodeGeometry();
            m.Run();
        }


        /// <summary>
        /// Loads data from Loadflow spreadsheet
        /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("LoadFromXlsm")]
        public string LoadFromXlsm(IFormFile file)
        {
            using( var da = new DataAccess()) {
                return da.Loadflow.LoadFromXlsm(file);
            }
        }

        /// <summary>
        /// Gets a list of branches - these are connections between nodes
        /// </summary>
        /// <value></value>
        [HttpGet]
        [Route("Branches")]
        public IList<Branch> Branches()
        {
            using( var da = new DataAccess()) {
                return da.Loadflow.GetBranches();
            }
        }

        /// <summary>
        /// Returns a list of boundaries that have been defined
        /// </summary>
        /// <returns>List of boundaries</returns>
        [HttpGet]
        [Route("Boundaries")]
        public IList<Data.Boundary> Boundaries() {
            using ( var da = new DataAccess() ) {
                return da.Loadflow.GetBoundaries();
            }
        }

        
        /// <summary>
        /// Runs a base load flow
        /// </summary>
        [HttpPost]
        [Route("RunBaseLoadflow")]
        public LoadflowResults RunBaseLoadflow()
        {
            using( var lf = new Loadflow.Loadflow() ) {
                lf.RunBaseCase("Auto");
                return new LoadflowResults(lf);
            }
        }

        /// <summary>
        /// Runs a multi-branch trip scenario with tripped branches defined by a list of their link names
        /// </summary>
        /// <param name="linkNames">List of link names of branches to trip</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RunTripLoadflow")]
        public LoadflowResults RunTripLoadflow(List<string> linkNames) {
            //??
            if ( linkNames==null ) {
                linkNames = new List<string>() { "ABH4A4:EXET40:A833"};
            }
            using( var lf = new Loadflow.Loadflow() ) {
                lf.RunTrip(linkNames);
                return new LoadflowResults(lf);
            }
        }

        /// <summary>
        /// Saves branches to csv for particular region
        /// </summary>
        [HttpPost]
        [Route("SaveBranchesAsCsv")]
        public IActionResult SaveBranchesAsCsv(string region)
        {
            FileStreamResult fsr;
            using( var da = new DataAccess()) {
                fsr = da.Loadflow.SaveBranchesAsCsv(region);                
            }

            return fsr;
        }

        /// <summary>
        /// Saves nodes to csv for particular region
        /// </summary>
        [HttpPost]
        [Route("SaveNodesAsCsv")]
        public IActionResult SaveNodesAsCsv(string region)
        {
            FileStreamResult fsr;
            using( var da = new DataAccess()) {
                fsr = da.Loadflow.SaveNodesAsCsv(region);                
            }

            return fsr;
        }

        /// <summary>
        /// Saves boundary/zones to csv
        /// </summary>
        [HttpPost]
        [Route("SaveBoundaryZonesAsCsv")]
        public IActionResult SaveBoundaryZonesAsCsv()
        {
            FileStreamResult fsr;
            using( var da = new DataAccess()) {
                fsr = da.Loadflow.SaveBoundaryZonesAsCsv();                
            }

            return fsr;
        }

        /// <summary>
        /// Gets loadflow network data
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("NetworkData")]
        public LoadflowNetworkData NetworkData() {
            using( var lf = new Loadflow.Loadflow() ) {
                lf.NetCheck();
                return new LoadflowNetworkData(lf);
            }
        }

        /// <summary>
        /// Gets loadflow location data
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("LocationData")]
        public LoadflowLocationData LocationData() {
            var m = new LoadflowLocationData();
            return m;
        }

        /// <summary>
        /// Setup boundary data prior to trip analysis
        /// </summary>
        /// <param name="boundaryName">Name of boundary</param>
        /// <returns></returns>
        [HttpPost]
        [Route("SetBound")]
        public LoadflowResults SetBound(string boundaryName) {
            using( var lf = new Loadflow.Loadflow() ) {
                var bfr = lf.Boundary.SetBound(boundaryName);
                return new LoadflowResults(lf,bfr,lf.Boundary.BoundaryTrips);
            }
        }
        
        /// <summary>
        /// Run all boundary trips.
        /// </summary>
        /// <param name="boundaryName">Name of boundary</param>
        /// <param name="connectionId">SignalR connection id to receive progress messages</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RunAllBoundaryTrips")]
        public LoadflowResults RunAllBoundaryTrips(string boundaryName, string? connectionId=null) {
            using( var lf = new Loadflow.Loadflow() ) {
                if ( connectionId!=null ) {
                    lf.Boundary.AllTripsProgress+=(t,p)=>{                    
                        _hubContext.Clients.Client(connectionId).SendAsync("Loadflow_AllTripsProgress",new {trip=t,percent=p});
                    };
                }
                var bfr = lf.Boundary.RunAllBoundaryTrips(boundaryName, out List<AllTripsResult> singleTrips, out List<AllTripsResult> doubleTrips);
                var results = new LoadflowResults(lf,bfr);
                results.SingleTrips = singleTrips;
                results.DoubleTrips = doubleTrips;
                return results;
            }
        }

        /// <summary>
        /// Run a single boundary trip
        /// </summary>
        /// <param name="boundaryName">Name of boundary</param>
        /// <param name="tripName">Name of trip</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RunBoundaryTrip")]
        public LoadflowResults RunBoundTrip(string boundaryName,string tripName) {
            using( var lf = new Loadflow.Loadflow() ) {
                var bfr = lf.Boundary.RunBoundaryTrip(boundaryName, tripName);
                return new LoadflowResults(lf, bfr);
            }
        }


        /// Stores Loadflow base reference from spreadsheet
        /// /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("Reference/UploadBase")]
        public void UploadBaseReference(IFormFile file) {
            var m=new LoadflowReference();
            m.LoadBase(file);
        }

        /// Stores Loadflow Boundary B8 reference from spreadsheet
        /// /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("Reference/UploadB8")]
        public void UploadB8Reference(IFormFile file) {
            var m=new LoadflowReference();
            m.LoadB8(file);
        }

        /// <summary>
        /// Runs Loadflow base against reference
        /// </summary>
        [HttpGet]
        [Route("Reference/RunBase")]
        public LoadflowErrors RunBaseReference() {
            var m=new LoadflowReference();
            return m.RunBase();
        }

        /// <summary>
        /// Runs Loadflow B8 (AllTrips) against reference
        /// </summary>
        [HttpGet]
        [Route("Reference/RunB8")]
        public LoadflowErrors RunB8Reference() {
            var m=new LoadflowReference();
            return m.RunB8();
        }
    }
}
