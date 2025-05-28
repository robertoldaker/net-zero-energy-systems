using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.BoundCalc;
using SmartEnergyLabDataApi.Data.BoundCalc;
using static SmartEnergyLabDataApi.BoundCalc.BoundCalc;

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
        /// Load data from TNUOS spreadsheet
        /// </summary>
        [HttpPost]
        [Route("Load/TNUOS")]
        public IActionResult LoadTNUOS(IFormFile file, int year=2024) {
            var m=new BoundCalcTnuosLoader();
            var msg = m.Load(file,year);
            //
            return this.Ok(msg);
        }

        /// <summary>
        /// Get list of branch names
        /// </summary>
        /// <param name="datasetId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("BranchNames")]
        public List<string> BranchNames(int datasetId) {
            using ( var da = new DataAccess() ) {
                var q = da.Session.QueryOver<Branch>();
                var ds = new DatasetData<Branch>(da, datasetId,m=>m.Id.ToString(),q);
                var bns = ds.Data.Select(m=>m.LineName).ToList<string>();
                return bns;
            }
        }

        /// <summary>
        /// Runs boundCalc
        /// </summary>
        [HttpPost]
        [Route("Run")]
        public IActionResult Run(int datasetId, SetPointMode setPointMode, int transportModelId, string? boundaryName=null, bool boundaryTrips=false, string? tripStr=null, string? connectionId=null )
        {
            try {
                var resp = BoundCalc.BoundCalc.Run(datasetId,setPointMode,transportModelId,boundaryName,boundaryTrips,tripStr,connectionId,_hubContext);
                return this.Ok(resp);
            } catch( Exception e) {
                return this.Ok(new BoundCalcResults(e.Message));
            }
        }

        /// <summary>
        /// Runs a specific boundary trip
        /// </summary>
        [HttpPost]
        [Route("RunBoundaryTrip")]
        public IActionResult RunBoundaryTrip(int datasetId, SetPointMode setPointMode, int transportModelId, string boundaryName, string tripName, string? tripStr )
        {
            try {
                var resp = BoundCalc.BoundCalc.RunBoundaryTrip(datasetId,setPointMode,transportModelId, boundaryName, tripName, tripStr);
                return this.Ok(resp);
            } catch( Exception e) {
                return this.Ok(new BoundCalcResults(e.Message));
            }
        }

        /// <summary>
        /// Sets up manual setpoint mode for a dataset
        /// </summary>
        /// <param name="datasetId">Id of dataset</param>
        /// <param name="initialSetPoints">List of initial set points</param>
        /// <returns></returns>
        [HttpPost]
        [Route("ManualSetPointMode")]
        public IActionResult ManualSetPointMode(int datasetId, [FromBody] List<CtrlSetPoint> initialSetPoints) {
            BoundCalc.BoundCalc.ManualSetPointMode(datasetId,this.GetUserId(),initialSetPoints);
            return Ok();
        }

        /// <summary>
        /// Adjusts branch capcities to remove overloads
        /// </summary>
        [HttpPost]
        [Route("AdjustBranchCapacities")]
        public BoundCalcResults AdjustBranchCapacities(int datasetId, int transportModelId){
            return BoundCalc.BoundCalc.AdjustBranchCapacities(datasetId, transportModelId, this.GetUserId());
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
        public BoundCalcNetworkData NetworkData(int datasetId, int transportModelId=0) {
            using( var bc = new BoundCalc.BoundCalc(datasetId,transportModelId) ) {
                return new BoundCalcNetworkData(bc);
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
        public IActionResult UpdateLocations(string datasetName) {
            using( var da = new DataAccess()) {
                var dataset = da.Datasets.GetDataset(DatasetType.BoundCalc, datasetName);
                if ( dataset==null ) {
                    throw new Exception($"Cannot find loadflow dataset with name [{datasetName}]");
                }
                var locUpdater = new BoundCalcLocationUpdater();
                var msg = locUpdater.Update(dataset.Id);
                return this.Ok(msg);
            }
        }

    }
}
