using System.Diagnostics;
using CommonInterfaces.Models;
using HaloSoft.EventLogger;
using Renci.SshNet;

namespace SmartEnergyLabDataApi.Models
{
    public class DatabaseBackup
    {
        private TaskRunner? _taskRunner;
        private const string DB_NAME = "smart_energy_lab";
        private readonly string LOCAL_PATH = AppFolders.Instance.Temp;

        public DatabaseBackup(TaskRunner? taskRunner) {
            _taskRunner = taskRunner;
        }

        public void Run() {
            var fileName = create();
            if ( _taskRunner?.CancellationToken.IsCancellationRequested==true ) {
                return;
            }
            upload(fileName);
        }

        private string create() {
            _taskRunner?.Update(TaskRunner.TaskState.RunningState.Running,"Creating backup ...");

            var dbName = "smart_energy_lab";

            var now = DateTime.Now;
            var ts = now.ToString("yyyy-MMM-dd-HH-mm-ss");
            var filename = $"{dbName}-{ts}.sql";

            var backup = new Execute();
            var args = $"--clean --if-exists -f \"{filename}\" {dbName}";
            // explicitly using /usr/bin to ensure it picks up v14.
            // installing gdal brings in postgres12 which then means "pg_dump" is v12
            var exitCode = backup.Run("/usr/bin/pg_dump",args,LOCAL_PATH);
            if ( exitCode!=0) {
                throw new Exception(backup.StandardError);
            }
            return filename;
        }

        private void upload(string filename) {
            Logger.Instance.LogInfoEvent("Uploading database backup ...");
            _taskRunner?.Update(TaskRunner.TaskState.RunningState.Running,"Uploading to SFTP site ...");

            sFtpToServer(DB_NAME,filename,Path.Combine(LOCAL_PATH,filename));
        }

        private void sFtpToServer(string dbName, string backupName, string backupPath)
        {
            string rootFolder = getRootFolder();
            string subFolder = getBackupSubFolder();
            // create an FTP client
            try {
                using (var client = getSFtpClient()) {

                    // begin connecting to the server
                    client.Connect();

                    // check if a folder exists for db
                    //
                    createDirectory(client,$"{rootFolder}");
                    createDirectory(client,$"{rootFolder}/{subFolder}/");
                    createDirectory(client,$"{rootFolder}/{subFolder}/{dbName}/");

                    var remotePath = $"{rootFolder}/{subFolder}/{dbName}/{backupName}";
                    uploadFile(client, backupPath, remotePath);

                    // get a list of files and only keep the 7 most recent ones
                    var files = client.ListDirectory($"{rootFolder}/{subFolder}/{dbName}");
                    var filesList = files.Where(m => m.IsRegularFile).OrderByDescending(m => m.LastWriteTimeUtc).Skip(7).ToList();
                    foreach (var file in filesList) {
                        client.DeleteFile(file.FullName);
                    }
                    //
                    client.Disconnect();
                }
            } finally {
                if ( File.Exists(backupPath)) {
                    File.Delete(backupPath);
                }
            }
        }

        private void createDirectory(SftpClient client, string folder)
        {
            if ( !client.Exists(folder)) {
                client.CreateDirectory(folder);
            }
        }

        private  void uploadFile(SftpClient client, string backupPath, string remotePath)
        {
            var fi = new FileInfo(backupPath);
            var length = fi.Length;
            long progress=0;
            using( var stream = File.Open(backupPath, FileMode.Open)) {
                client.UploadFile(stream, remotePath, (prog)=>{
                    var newProgress = (100*(long) prog)/length;
                    if ( newProgress!=progress) {
                        progress = newProgress;
                        _taskRunner?.Update((int) progress);
                    }
                });
            }
        }

        private static string getBackupSubFolder()
        {
            if ( AppEnvironment.Instance.Context == Context.Production) {
                return "ProductionBackups";
            } else if ( AppEnvironment.Instance.Context == Context.Staging ) {
                return "StagingBackups";
            } else if ( AppEnvironment.Instance.Context == Context.Development) {
                return "TestBackups";
            } else {
                throw new Exception($"Unexpected AppEnvironment found [{AppEnvironment.Instance.Context}]");
            }
        }

        private static string getRootFolder()
        {
            return "SQL-backups";
        }

        private static SftpClient getSFtpClient()
        {
            string hostName = "ftpsqlbackup1.angelbooks.biz";
            var connectionInfo = new Renci.SshNet.ConnectionInfo(hostName,
                                                    "angelbooks",
                                                    new PasswordAuthenticationMethod("angelbooks", "pleasant12A"),
                                                    new PrivateKeyAuthenticationMethod("rsa.key"));
            var client = new SftpClient(connectionInfo);

            return client;
        }

        private static void removeDirectory(SftpClient client, string path)
        {
            var files = client.ListDirectory(path);
            foreach (var file in files) {
                if ( file.IsRegularFile ) {
                    client.DeleteFile(file.FullName);
                }
            }
            client.DeleteDirectory(path);
        }

        public StreamReader BackupToStream(out string filename) {

            var dbName = "smart_energy_lab";
            var args = $"--clean --if-exists {dbName}";
            var now = DateTime.Now;
            var ts = now.ToString("yyyy-MMM-dd-HH-mm-ss");
            filename = $"{dbName}-{ts}.sql";

            // explicitly using /usr/bin to ensure it picks up v14.
            // installing gdal brings in postgres12 which then means "pg_dump" is v12
            var exe = "/usr/bin/pg_dump";
            ProcessStartInfo oInfo = new ProcessStartInfo(exe, args);
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;

			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;

			StreamReader srOutput = null;
			StreamReader srError = null;

			Process proc = System.Diagnostics.Process.Start(oInfo);
			srOutput = proc.StandardOutput;
			return srOutput;            
        }


    }
}