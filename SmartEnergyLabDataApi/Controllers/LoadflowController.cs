using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;
using static SmartEnergyLabDataApi.Models.LoadflowReference;
using Org.BouncyCastle.Crypto.Signers;
using System.Text.Json;
using System.Linq.Expressions;

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
        /// Returns a list of boundaries that have been defined
        /// </summary>
        /// <returns>List of boundaries</returns>
        [HttpGet]
        [Route("Boundaries")]
        public DatasetData<Data.Boundary> Boundaries(int datasetId) {
            using ( var da = new DataAccess() ) {
                var q = da.Session.QueryOver<Data.Boundary>();
                var ds = new DatasetData<Data.Boundary>(da, datasetId,m=>m.Id.ToString(),q);
                // add zones they belong to
                var boundDict = da.Loadflow.GetBoundaryZoneDict(ds.Data);
                foreach( var b in ds.Data) {
                    if ( boundDict.ContainsKey(b) ) {
                        b.Zones = boundDict[b];
                    } else {
                        b.Zones = new List<Zone>();
                    }
                }
                return ds;
            }
        }

        
        /// <summary>
        /// Runs a base load flow
        /// </summary>
        [HttpPost]
        [Route("RunBaseLoadflow")]
        public IActionResult RunBaseLoadflow(int datasetId)
        {
            using( var lf = new Loadflow.Loadflow(datasetId) ) {
                lf.RunBaseCase("Auto");
                var resp = new LoadflowResults(lf);
                return this.Ok(resp);
            }
        }

        /// <summary>
        /// Runs a multi-branch trip scenario with tripped branches defined by a list of their link names
        /// </summary>
        /// <param name="datasetId">Id of dataset to use</param>
        /// <param name="linkNames">List of link names of branches to trip</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RunTripLoadflow")]
        public LoadflowResults RunTripLoadflow(int datasetId,List<string> linkNames) {
            //??
            if ( linkNames==null ) {
                linkNames = new List<string>() { "ABH4A4:EXET40:A833"};
            }
            using( var lf = new Loadflow.Loadflow(datasetId) ) {
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
        /// <param name="datasetId">Id of dataset</param>
        /// /// <returns></returns>
        [HttpGet]
        [Route("NetworkData")]
        public LoadflowNetworkData NetworkData(int datasetId) {
            using( var lf = new Loadflow.Loadflow(datasetId) ) {
                return new LoadflowNetworkData(lf);
            }
        }

        /// <summary>
        /// Setup boundary data prior to trip analysis
        /// </summary>
        /// <param name="datasetId">Dataset id to use</param>
        /// <param name="boundaryName">Name of boundary</param>
        /// <returns></returns>
        [HttpPost]
        [Route("SetBound")]
        public LoadflowResults SetBound(int datasetId, string boundaryName) {
            using( var lf = new Loadflow.Loadflow(datasetId) ) {
                var bfr = lf.Boundary.SetBound(boundaryName);
                var lfr = new LoadflowResults(lf,bfr,lf.Boundary.BoundaryTrips);
                //?? No saving yet as waiting for new version to be implemented
                //?? lfr.Save();
                return lfr;
            }
        }

        /// <summary>
        /// Run all boundary trips.
        /// </summary>
        /// <param name="datasetId">Dataset id to use</param>
        /// <param name="boundaryName">Name of boundary</param>
        /// <param name="connectionId">SignalR connection id to receive progress messages</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RunAllBoundaryTrips")]
        public LoadflowResults RunAllBoundaryTrips(int datasetId, string boundaryName, string? connectionId=null) {
            using( var lf = new Loadflow.Loadflow(datasetId) ) {
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
        /// <param name="datasetId">Dataset id to use</param>
        /// <param name="boundaryName">Name of boundary</param>
        /// <param name="tripName">Name of trip</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RunBoundaryTrip")]
        public LoadflowResults RunBoundTrip(int datasetId, string boundaryName,string tripName) {
            using( var lf = new Loadflow.Loadflow(datasetId) ) {
                var bfr = lf.Boundary.RunBoundaryTrip(boundaryName, tripName);
                return new LoadflowResults(lf, bfr);
            }
        }


        /// <summary>
        /// Stores Loadflow base reference from spreadsheet
        /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("Reference/UploadBase")]
        public void UploadBaseReference(IFormFile file) {
            var m=new LoadflowReference();
            m.LoadBase(file);
        }

        /// <summary>
        /// Stores Loadflow Boundary B8 reference from spreadsheet
        /// </summary>
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
        public LoadflowErrors RunBaseReference(bool showAllErrors=false) {
            var m=new LoadflowReference();
            return m.RunBase(showAllErrors);
        }

        /// <summary>
        /// Runs Loadflow B8 (AllTrips) against reference
        /// </summary>
        [HttpGet]
        [Route("Reference/RunB8")]
        public LoadflowErrors RunB8Reference(bool showAllErrors=false) {
            var m=new LoadflowReference();
            return m.RunB8(showAllErrors);
        }

        /// <summary>
        /// Load data from ESO ETYS
        /// </summary>
        /// <param name="loadOptions"> (0-All circuits, 1-only high voltage)</param>
        [HttpPost]
        [Route("Load/ETYS")]
        public void LoadETYS(LoadflowETYSLoader.LoadOptions loadOptions = LoadflowETYSLoader.LoadOptions.OnlyHighVoltageCircuits) {
            var m=new LoadflowETYSLoader(loadOptions);
            m.Load();
        }

        /// <summary>
        /// Fix missing zones
        /// </summary>
        [HttpPost]
        [Route("FixMissingZones")]
        public void FixMissingZones(LoadflowETYSLoader.LoadOptions loadOptions = LoadflowETYSLoader.LoadOptions.OnlyHighVoltageCircuits) {
            var m=new LoadflowETYSLoader(loadOptions);
            m.FixMissingZones();
        }

        /// <summary>
        /// Set any missing voltages and locations for loadflow nodes
        /// </summary>
        /// <param name="datasetName">Name of dataset</param>
        [HttpPost]
        [Route("Nodes/SetLocationsAndVoltages")]
        public void SetNodeVoltages(string datasetName) {
            using( var da = new DataAccess()) {
                var dataset = da.Datasets.GetDataset(DatasetType.Loadflow, datasetName);
                if ( dataset==null ) {
                    throw new Exception($"Cannot find loadflow dataset with name [{datasetName}]");
                }
                da.Loadflow.SetNodeVoltagesAndLocations(dataset.Id);
                da.CommitChanges();
            }
        }

        /// <summary>
        /// Create missing locations for a loadflow dataset
        /// </summary>
        /// <param name="datasetName">Name of dataset</param>
        [HttpPost]
        [Route("Nodes/UpdateLocations")]
        public void UpdateLocations(string datasetName) {
            using( var da = new DataAccess()) {
                var dataset = da.Datasets.GetDataset(DatasetType.Loadflow, datasetName);
                if ( dataset==null ) {
                    throw new Exception($"Cannot find loadflow dataset with name [{datasetName}]");
                }
                var locUpdater = new LoadflowLocationUpdater();
                locUpdater.Update(dataset.Id);
            }
        }

    }
}
