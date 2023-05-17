using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.SGT;
using SmartEnergyLabDataApi.Models;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EnergySystemLabDataApi.SubStations
{
    /// <summary>
    /// Api to support admin operations
    /// </summary>
    [ApiController]
    [Route("Admin")]
    public class AdminController : ControllerBase
    {
        private IBackgroundTasks _backgroundTasks;
        private DatabaseBackupBackgroundTask _backupDbTask;

        public AdminController(IBackgroundTasks backgroundTasks)
        {
            _backgroundTasks = backgroundTasks;
            _backupDbTask = backgroundTasks.GetTask<DatabaseBackupBackgroundTask>(DatabaseBackupBackgroundTask.Id);
        }

        /// <summary>
        /// Obtains current system log
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Logs")]
        public LogData Get()
        {
            return (new AdminModel()).LoadLogFile();
        }

        /// <summary>
        /// Returns system info for the server
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("SystemInfo")]
        public object SystemInfo() {
            return new { ProcessorCount=Environment.ProcessorCount };
        }

        /// <summary>
        /// Backsup database
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("BackupDb")]
        public IActionResult BackupDb() {
            _backupDbTask.Run();
            return this.Ok();
        }

        /// <summary>
        /// Cancels a running task
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Cancel")]
        public IActionResult Cancel(int taskId) {
            try {
                _backgroundTasks.GetTask<BackgroundTaskBase>(taskId)?.Cancel();
                return this.Ok();
            } catch( Exception e) {
                return this.StatusCode(500,e.Message);
            }
        }

        /// <summary>
        /// Load geo spatial data
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadGeoSpatialData")]
        public IActionResult LoadGeoSpatialData() {
            try {
                var loader = new GeoSpatialDataLoader();
                var message = loader.Load();
                return this.Ok(message);
            } catch( Exception e) {
                return this.StatusCode(500,e.Message);
            }
        }

        /// <summary>
        /// Load geo spatial data
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadDistributionData")]
        public IActionResult LoadDistributionData() {
            try {
                var loader = new DistributionDataLoader();
                var message = loader.Load();
                return this.Ok(message);
            } catch( Exception e) {
                return this.StatusCode(500,e.Message);
            }
        }

        /// <summary>
        /// Test Log file
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("TestLog")]
        public IActionResult TestLog() {
            try {
                for( int i=1; i<=100; i++) {
                    Logger.Instance.LogInfoEvent($"Test log message [{i}]");
                }
                return this.Ok();
            } catch( Exception e) {
                return this.StatusCode(500,e.Message);
            }
        }
    }

}
