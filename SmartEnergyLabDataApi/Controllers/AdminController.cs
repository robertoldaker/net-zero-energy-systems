using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.FileProviders;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.SGT;
using SmartEnergyLabDataApi.Models;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EnergySystemLabDataApi.Controllers
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
        private LoadDistributionDataBackgroundTask _loadDistributionDataTask;

        public AdminController(IBackgroundTasks backgroundTasks)
        {
            _backgroundTasks = backgroundTasks;
            _backupDbTask = backgroundTasks.GetTask<DatabaseBackupBackgroundTask>(DatabaseBackupBackgroundTask.Id);
            _loadDistributionDataTask = backgroundTasks.GetTask<LoadDistributionDataBackgroundTask>(LoadDistributionDataBackgroundTask.Id);
        }

        /// <summary>
        /// Obtains current system log
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Logs")]
        public LogData Get()
        {
            return AdminModel.Instance.LoadLogFile();
        }

        /// <summary>
        /// Deletes current system log
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Logs/Delete")]
        public void Delete()
        {
            AdminModel.Instance.DeleteLogFile();
        }

        /// <summary>
        /// Returns system info for the server
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("SystemInfo")]
        public SystemInfo SystemInfo() {
            return new SystemInfo();
        }

        /// <summary>
        /// Sets maintenance mode on/off
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("MaintenanceMode")]
        public void MaintenanceMode(bool state) {
            AdminModel.Instance.MaintenanceMode = state;
        }

        /// <summary>
        /// Backsup database to backup server
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("BackupDb")]
        public IActionResult BackupDb() {
            _backupDbTask.Run();
            return this.Ok();
        }

        /// <summary>
        /// Backsup database locally
        /// </summary>
        /// <param name="appGroup">The application group to backup</param>
        /// <returns></returns>
        [HttpGet]
        [Route("BackupDbLocally")]
        public IActionResult BackupDbLocally(ApplicationGroup appGroup = ApplicationGroup.All) {
            var m = new DatabaseBackup(null);
            var sr = m.BackupToStream(out string filename, appGroup);
            var fsr = new FileStreamResult(sr.BaseStream, "application/sql");
			fsr.FileDownloadName = filename;
			return fsr;
        }

        /// <summary>
        /// Restores database backup
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("RestoreDb")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public IActionResult RestoreDb(IFormFile file) {
            var m = new DatabaseBackup(null);
            m.Restore(file);
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
        /// Loads substations and supply points from external websites
        /// </summary>
        /// <param name="source">Either 0 for all network providers, 1 for NGED or 2 for UKPower for UK Power networks</param>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadDistributionData")]
        public IActionResult LoadDistributionData(LoadNetworkDataSource source=LoadNetworkDataSource.All) {
            try {
                //
                _loadDistributionDataTask.Run(source);
                return this.Ok();
            } catch( Exception e) {
                return this.StatusCode(500,e.Message);
            }
        }

        /// <summary>
        /// Returns data about distribution data available
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DistributionData")]
        public DistributionData GetDistributionData() {
            var m = new DistributionData();
            return m;
        }

        /// <summary>
        /// Returns data about transmission data available
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("TransmissionData")]
        public TransmissionData GetTransmissionData() {
            var m = new TransmissionData();
            return m;
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

        /// <summary>
        /// Returns data about the current database usage
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DataModel")]
        public DataModel GetDataModel() {
            var m = new DataModel();
            m.Load();
            return m;
        }

        /// <summary>
        /// Performs a database cleanup operation to release diskspace (does a VACUUM FULL ANALYZE)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("PerformCleanup")]
        public void PerformCleanup() {
            Logger.Instance.LogInfoEvent("Started database cleanup...");
            DataAccessBase.PerformCleanup();
            Logger.Instance.LogInfoEvent("Finished database cleanup");
        }

        [HttpGet]
        [Route("GenerateError")]
        public void GenerateError() {
            throw new Exception("This is an error generated for test purposes using \"Admin/GenerateError\"");
        }

        /// <summary>
        /// Deletes all GSPs, primrary and distribution substations for the given distribution area
        /// </summary>
        /// <param name="gaId"></param>
        [HttpPost]
        [Route("DeleteAllSubstations")]
        public void DeleteAllSubstations(int gaId) {
            using( var da = new DataAccess() ) {
                da.Substations.DeleteAllDistributionInGeographicalArea(gaId);
                da.Substations.DeleteAllPrimaryInGeographicalArea(gaId);
                da.SupplyPoints.DeleteAllGridSupplyPointsInGeographicalArea(gaId);
                da.CommitChanges();
            }
        }

    }

}
