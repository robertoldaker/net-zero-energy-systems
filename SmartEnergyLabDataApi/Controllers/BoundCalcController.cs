using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.BoundCalc;
using static SmartEnergyLabDataApi.Models.LoadflowReference;
using Org.BouncyCastle.Crypto.Signers;
using System.Text.Json;
using System.Linq.Expressions;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.Controllers
{
    [Route("BoundCalc")]
    [ApiController]
    public class BoundCalcController : ControllerBase
    {
        IHubContext<NotificationHub> _hubContext;
        public BoundCalcController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Loads data from BoundCalc spreadsheet
        /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("LoadFromXlsm")]
        public string LoadFromXlsm(IFormFile file)
        {
            using( var da = new DataAccess()) {
                return da.BoundCalc.LoadFromXlsm(file);
            }
        }

        /// <summary>
        /// Returns a list of boundaries that have been defined
        /// </summary>
        /// <returns>List of boundaries</returns>
        [HttpGet]
        [Route("Boundaries")]
        public DatasetData<BoundCalcBoundary> Boundaries(int datasetId) {
            using ( var da = new DataAccess() ) {
                var q = da.Session.QueryOver<BoundCalcBoundary>();
                var ds = new DatasetData<BoundCalcBoundary>(da, datasetId,m=>m.Id.ToString(),q);
                // add zones they belong to
                var boundDict = da.BoundCalc.GetBoundaryZoneDict(ds.Data);
                foreach( var b in ds.Data) {
                    if ( boundDict.ContainsKey(b) ) {
                        b.Zones = boundDict[b];
                    } else {
                        b.Zones = new List<BoundCalcZone>();
                    }
                }
                return ds;
            }
        }

        
        /// <summary>
        /// Runs a base load flow
        /// </summary>
        [HttpPost]
        [Route("RunBase")]
        public IActionResult RunBase(int datasetId)
        {
            using( var lf = new BoundCalc.BoundCalc(datasetId) ) {
                //lf.RunBaseCase("Auto");
                lf.RunBoundCalc(null,null,BoundCalc.BoundCalc.SPAuto,false, false);
                var resp = new BoundCalcResults(lf);
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
        [Route("RunTripBoundCalc")]
        public BoundCalcResults RunTripBoundCalc(int datasetId,List<string> linkNames) {
            if ( linkNames==null ) {
                linkNames = new List<string>() { "ABH4A4:EXET40:A833"};
            }
            using( var lf = new BoundCalc.BoundCalc(datasetId) ) {
                //??lf.RunTrip(linkNames);
                return new BoundCalcResults(lf);
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
                fsr = da.BoundCalc.SaveBranchesAsCsv(region);                
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
                fsr = da.BoundCalc.SaveNodesAsCsv(region);                
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
                fsr = da.BoundCalc.SaveBoundaryZonesAsCsv();                
            }

            return fsr;
        }

        /// <summary>
        /// Gets BoundCalc network data
        /// </summary>
        /// <param name="datasetId">Id of dataset</param>
        /// /// <returns></returns>
        [HttpGet]
        [Route("NetworkData")]
        public BoundCalcNetworkData NetworkData(int datasetId) {
            using( var lf = new BoundCalc.BoundCalc(datasetId) ) {
                return new BoundCalcNetworkData(lf);
            }
        }

        /// <summary>
        /// Stores Loadflow base reference from spreadsheet
        /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("Reference/UploadBase")]
        public void UploadBaseReference(IFormFile file) {
            //?? Needs updating for BoundCalc
            /*
            var m=new LoadflowReference();
            m.LoadBase(file);
            */
        }

        /// <summary>
        /// Stores Loadflow Boundary B8 reference from spreadsheet
        /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("Reference/UploadB8")]
        public void UploadB8Reference(IFormFile file) {
            //?? Needs updating for BoundCalc
            /*
            var m=new LoadflowReference();
            m.LoadB8(file);
            */
        }

        /// <summary>
        /// Runs Loadflow base against reference
        /// </summary>
        [HttpGet]
        [Route("Reference/RunBase")]
        public IActionResult RunBaseReference(bool showAllErrors=false) {
            //?? Needs updating for BoundCalc
            /*
            var m=new LoadflowReference();
            return m.RunBase(showAllErrors);
            */
            return Ok();
        }

        /// <summary>
        /// Runs Loadflow B8 (AllTrips) against reference
        /// </summary>
        [HttpGet]
        [Route("Reference/RunB8")]
        public IActionResult RunB8Reference(bool showAllErrors=false) {
            //?? Needs updating for BoundCalc
            /*
            var m=new LoadflowReference();
            return m.RunB8(showAllErrors);
            */
            return Ok();
        }

        /// <summary>
        /// Load data from ESO ETYS
        /// </summary>
        /// <param name="loadOptions"> (0-All circuits, 1-only high voltage)</param>
        [HttpPost]
        [Route("Load/ETYS")]
        public void LoadETYS(BoundCalcETYSLoader.BoundCalcLoadOptions loadOptions = BoundCalcETYSLoader.BoundCalcLoadOptions.OnlyHighVoltageCircuits) {
            var m=new BoundCalcETYSLoader(loadOptions);
            m.Load();
            
        }

        /// <summary>
        /// Fix missing zones
        /// </summary>
        [HttpPost]
        [Route("FixMissingZones")]
        public void FixMissingZones(BoundCalcETYSLoader.BoundCalcLoadOptions loadOptions = BoundCalcETYSLoader.BoundCalcLoadOptions.OnlyHighVoltageCircuits) {
            var m=new BoundCalcETYSLoader(loadOptions);
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
                var dataset = da.Datasets.GetDataset(DatasetType.BoundCalc, datasetName);
                if ( dataset==null ) {
                    throw new Exception($"Cannot find loadflow dataset with name [{datasetName}]");
                }
                da.BoundCalc.SetNodeVoltagesAndLocations(dataset.Id);
                da.CommitChanges();
            }
        }

        /// <summary>
        /// Create missing locations for a BoundCalc dataset
        /// </summary>
        /// <param name="datasetName">Name of dataset</param>
        [HttpPost]
        [Route("Nodes/UpdateLocations")]
        public void UpdateLocations(string datasetName) {
            using( var da = new DataAccess()) {
                var dataset = da.Datasets.GetDataset(DatasetType.BoundCalc, datasetName);
                if ( dataset==null ) {
                    throw new Exception($"Cannot find loadflow dataset with name [{datasetName}]");
                }
                //?? Needs updating for boundCalc
                //??var locUpdater = new LoadflowLocationUpdater();
                //??locUpdater.Update(dataset.Id);
            }
        }

    }
}
