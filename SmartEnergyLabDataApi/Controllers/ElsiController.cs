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
                        string fn = Path.Combine(AppFolders.Instance.Temp,"ElsiDebug.txt");
                        PrintFile.Init(fn);
                    }
                #endif
                var results = mm.RunDay(day);
                #if DEBUG 
                    if ( printFile  ) {
                        PrintFile.Close();
                    }
                #endif
            
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
        public string PrintInput(int day, ElsiScenario scenario, string datasetName="GB network") {
            using ( var da = new DataAccess() ) {
                var dataVersion = da.Datasets.GetDataset(DatasetType.Elsi,this.GetUserId(),datasetName);
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
                var dataset = da.Datasets.GetDataset(datasetId);
                if (dataset!=null) {
                    var edrs = da.Elsi.GetElsiDayResults(datasetId, scenario);
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
                var dataset = da.Datasets.GetDataset(datasetId);
                if (dataset!=null) {
                    //
                    var m = new ElsiCsvWriter(da);
                    var ms = m.WriteToMemoryStream(datasetId,ElsiScenario.SteadyProgression);
                    //                    
                    var fsr = new FileStreamResult(ms, "application/json");
                    fsr.FileDownloadName = $"Elsi results [{dataset.Name}].csv";
                    return fsr;
                } else {
                    throw new Exception($"Cannot find dataset with id=[{datasetId}]");
                }
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
        /// <param name="phase">Only consider this phase of the analysis (phases are "(A)vailabilities", "(M)arket phase", "(B)alance phase" and "(B)alance (M)echanism")</param>
        [HttpGet]
        [Route("Reference/Run")]
        public ElsiErrors Run(int day=1, double tol=1e-6, string phase="All") {
            var m=new ElsiReference();
            return m.Run(day,tol,phase);
        }
        
    

    }
}
