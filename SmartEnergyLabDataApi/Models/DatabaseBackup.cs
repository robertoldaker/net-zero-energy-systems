using HaloSoft.EventLogger;

namespace SmartEnergyLabDataApi.Models
{
    public class DatabaseBackup
    {
        private TaskRunner? _taskRunner;
        public DatabaseBackup(TaskRunner? taskRunner) {
            _taskRunner = taskRunner;
        }

        public void Run() {
            create();
            if ( _taskRunner?.CancellationToken.IsCancellationRequested==true ) {
                return;
            }
            upload();
        }

        private void create() {
            Logger.Instance.LogInfoEvent("Creating database backup");
            _taskRunner?.Notify(TaskRunner.TaskState.RunningState.Running,"Creating backup");
            Task.Delay(5000,_taskRunner.CancellationToken).Wait();
        }

        private void upload() {
            Logger.Instance.LogInfoEvent("Uploading database backup");
            _taskRunner?.Notify(TaskRunner.TaskState.RunningState.Running,"Uploading to SFTP site");
            Task.Delay(5000,_taskRunner.CancellationToken).Wait();
        }


    }
}