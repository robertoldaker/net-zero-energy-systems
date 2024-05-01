using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;
using SmartEnergyLabDataApi.Elsi;
using static SmartEnergyLabDataApi.Models.ElsiReference;
using System.Text.Json;

namespace SmartEnergyLabDataApi.Controllers
{
    [Route("Elsi")]
    [ApiController]
    public class ElsiController : ControllerBase
    {
        IHubContext<NotificationHub> _hubContext;
        public ElsiController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }


        /// <summary>
        /// Loads data from Elsi spreadsheet
        /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("LoadFromXlsm")]
        public string LoadFromXlsm(IFormFile file)
        {
            using( var da = new DataAccess()) {
                return da.Elsi.LoadFromXlsm(file);
            }
        }

        /// <summary>
        /// Runs Elsi on the day specified
        /// </summary>
        /// <param name="day">Day of year (1-365)</param>
        /// <param name="scenario">Scenario to use</param>
        /// <param name="datasetId">Id of dataset to use</param>
        /// <param name="printFile">printFile</param>
        /// <param name="parallelProcessing">parallelProcessing</param>
        /// <param name="connectionId">connectionId</param>
        [HttpGet]
        [Route("RunSingleDay")]
        public ElsiDayResult RunSingleDay(
            int day, 
            ElsiScenario scenario,
            int datasetId,
            bool printFile=false, 
            bool parallelProcessing=true, 
            string? connectionId=null) {
            ElsiLog? log = connectionId!=null ? new ElsiLog(_hubContext, connectionId) : null;
            using ( var da = new DataAccess() ) {
                var datasetInfo = new DatasetInfo(da,datasetId);
                var data = new ElsiData(da, datasetInfo, scenario);
                var mm = new ModelManager(data,log); 
                #if DEBUG 
                    if ( printFile  ) {
                        PrintFile.Init();
                    }
                #endif
                var results = mm.RunDay(day);
                return results;
            }
        }

        /// <summary>
        /// Runs Elsi over the days specified by startDay and endDay
        /// </summary>
        /// <param name="startDay">Day of year (1-365) to start run</param>
        /// <param name="endDay">Day of year (1-365) to end run</param>
        /// <param name="scenario">Scenario to use</param>
        /// <param name="datasetId">Dataset id</param>
        /// <param name="connectionId">Connectionid for results</param>
        [HttpGet]
        [Route("RunDays")]
        public IActionResult RunDays(
            int startDay, 
            int endDay, 
            ElsiScenario scenario, 
            int datasetId, 
            string? connectionId=null) {
            ElsiLog? log = connectionId!=null ? new ElsiLog(_hubContext, connectionId) : null;
            var dayRunner = new DayRunner(startDay,endDay,scenario,datasetId,log);
            if ( connectionId!=null) {
                dayRunner.ElsiProgress+=(sender,e)=>{
                    _hubContext.Clients.Client(connectionId).SendAsync("Elsi_Progress",e);
                };
            }
            dayRunner.Run();

            return Ok();
        }

        /// <summary>
        /// Prints input data for a day and scenario
        /// </summary>
        /// <param name="day">Day of year (1-365)</param>
        /// <param name="scenario">Scenario to use</param>
        /// <param name="datasetName">Name of dataset</param>
        [HttpGet]
        [Route("PrintInput")]
        public string PrintInput(int day, ElsiScenario scenario, string datasetName="Default") {
            using ( var da = new DataAccess() ) {
                var dataVersion = da.Elsi.GetDataVersion(this.GetUserId(),datasetName);
                if ( dataVersion!=null ) {
                    var dataset = new DatasetInfo(da,dataVersion.Id);
                    var data = new ElsiData(da, dataset, scenario);
                    data.SetDay(day);
                    //
                    var lines = data.Print();
                    return lines;
                } else {
                    throw new Exception($"Cannot find dataset with name [{datasetName}]");
                }
            }
        }

        /// <summary>
        /// Get list of ElsiDataVersions for the current user
        /// </summary>
        [HttpGet]
        [Route("DataVersions")]
        public IList<ElsiDataVersion> DataVersions() {
            using ( var da = new DataAccess() ) {
                return da.Elsi.GetDataVersions(this.GetUserId());
            }
        }

        /// <summary>
        /// Creates a new ElsiDataVersion for the current user
        /// </summary>
        [HttpPost]
        [Route("NewDataVersion")]
        public IActionResult NewDataVersion([FromBody] NewElsiDataVersion dv) {
            using( var m = new NewElsiDataVersionModel(this, dv)) {
                if ( !m.Save() ) {
                    return this.ModelErrors(m.Errors);
                }
                // return the id of the new object created
                return Ok(m.Id.ToString());
            }
        }

        /// <summary>
        /// Saves changes to an existing ElsiDataVersion for the current user
        /// </summary>
        [HttpPost]
        [Route("SaveDataVersion")]
        public IActionResult SaveDataVersion([FromBody] ElsiDataVersion dv) {
            using( var m = new EditElsiDataVersionModel(this, dv)) {
                if ( !m.Save() ) {
                    return this.ModelErrors(m.Errors);
                }
                return Ok();
            }
        }

        /// <summary>
        /// Deletes an existing ElsiDataVersion for the current user
        /// </summary>
        [HttpPost]
        [Route("DeleteDataVersion")]
        public IActionResult DeleteDataVersion([FromBody] int id) {
            using( var m = new EditElsiDataVersionModel(this, id)) {
                m.Delete();
                return Ok();
            }
        }

        [HttpGet]
        [Route("DatasetInfo")]
        public DatasetInfo DatasetInfo(int versionId) {
            using( var da = new DataAccess() ) {
                return new DatasetInfo(da,versionId);
            }
        }
    
        [HttpGet]
        [Route("Results")]
        public IList<ElsiResult> Results(int datasetId, ElsiScenario scenario) {
            using( var da = new DataAccess() ) {
                var ers = da.Elsi.GetResults(datasetId, scenario);
                return ers;
            }
        }
    
        [HttpGet]
        [Route("DownloadResultsAsJson")]
        public IActionResult DownloadResultsAsJson(int datasetId, ElsiScenario scenario) {
            using( var da = new DataAccess() ) {
                var dataset = da.Elsi.GetDataVersion(datasetId);
                if (dataset!=null) {
                    var ers = da.Elsi.GetResults(datasetId, scenario);
                    var edrs = new List<ElsiDayResult>();
                    foreach( var er in ers) {
                        var erStr = System.Text.Encoding.UTF8.GetString(er.Data);
                        var edr = JsonSerializer.Deserialize<ElsiDayResult>(erStr,new JsonSerializerOptions() {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        if ( edr!=null ){
                            edrs.Add(edr);
                        }
                    }
                    var json=JsonSerializer.Serialize(edrs,new JsonSerializerOptions() {
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });                    
                    var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                    var fsr = new FileStreamResult(ms, "application/json");
                    fsr.FileDownloadName = $"Elsi results [{dataset.Name}].json";
                    return fsr;
                } else {
                    throw new Exception($"Cannot find dataset with id=[{datasetId}]");
                }
            }
        }

        [HttpGet]
        [Route("DownloadResultsAsCsv")]
        public IActionResult DownloadResultsAsCsv(int datasetId, ElsiScenario scenario) {
            using( var da = new DataAccess() ) {
                var dataset = da.Elsi.GetDataVersion(datasetId);
                if (dataset!=null) {
                    var ers = da.Elsi.GetResults(datasetId, scenario);
                    var edrs = new List<ElsiDayResult>();
                    foreach( var er in ers) {
                        var erStr = System.Text.Encoding.UTF8.GetString(er.Data);
                        var edr = JsonSerializer.Deserialize<ElsiDayResult>(erStr,new JsonSerializerOptions() {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        if ( edr!=null ){
                            edrs.Add(edr);
                        }
                    }
                    var json=JsonSerializer.Serialize(edrs,new JsonSerializerOptions() {
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });                    
                    var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                    var fsr = new FileStreamResult(ms, "application/json");
                    fsr.FileDownloadName = $"Elsi results [{dataset.Name}].json";
                    return fsr;
                } else {
                    throw new Exception($"Cannot find dataset with id=[{datasetId}]");
                }
            }
        }

        [HttpGet]
        [Route("ResultCount")]
        public int ResultCount(int datasetId) {
            using( var da = new DataAccess() ) {
                var count = da.Elsi.GetResultCount(datasetId);
                return count;
            }
        }

        [HttpGet]
        [Route("DayResult")]
        public string DayResult(int elsiResultId) {
            using( var da = new DataAccess() ) {
                var ers = da.Elsi.GetResult(elsiResultId);
                string json = "";
                if ( ers!=null && ers.Data!=null) {
                    json = System.Text.Encoding.UTF8.GetString(ers.Data);
                }
                return json;
            }
        }

        [HttpPost]
        [Route("SaveUserEdit")]
        public IActionResult SaveUserEdit([FromBody] ElsiUserEdit userEdit) {
            using( var da = new DataAccess() ) {
                da.Elsi.SaveUserEdit(userEdit);
                da.CommitChanges();
            }
            return Ok();
        }

        [HttpPost]
        [Route("DeleteUserEdit")]
        public IActionResult DeleteUserEdit([FromBody] int id) {
            using( var da = new DataAccess() ) {
                da.Elsi.DeleteUserEdit(id);
                da.CommitChanges();
            }
            return Ok();
        }

        /// <summary>
        /// Stores Elsi reference spreadsheet
        /// </summary>
        /// <param name="file"></param>
        [HttpPost]
        [Route("Reference/Upload")]
        public void UploadReference(IFormFile file) {
            var m=new ElsiReference();
            m.Load(file);
        }

        /// <summary>
        /// Runs Elsi against reference and returns variables with abs error greater than supplied tolerance
        /// </summary>
        /// <param name="day">Day number to use (1-365)</param>
        /// <param name="tol">Tolerance to use for absolute error</param>
        /// <param name="phase">Only consider this phase of the analysis (phases are "Availabilities", "Market phase", "Balance phase" and "Balance mechanism")</param>
        [HttpGet]
        [Route("Reference/Run")]
        public ElsiErrors Run(int day=1, double tol=1e-6, string phase="All") {
            var m=new ElsiReference();
            return m.Run(day,tol,phase);
        }
        
    

    }
}
