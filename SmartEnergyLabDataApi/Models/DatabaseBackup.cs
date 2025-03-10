using System.Diagnostics;
using CommonInterfaces.Models;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using Renci.SshNet;
using SmartEnergyLabDataApi.Data;

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

            var dbName = Program.DB_NAME;

            var now = DateTime.Now;
            var ts = now.ToString("yyyy-MMM-dd-HH-mm-ss");
            var filename = $"{dbName}-{ts}.dump";

            var backup = new Execute();            
            var args = getPgDumpArgs(filename);
            var pgDump = getPgDump();
            var exitCode = backup.Run(pgDump,args,LOCAL_PATH,new Dictionary<string, string>() { {"PGPASSWORD",Program.DB_PASSWORD} });
            if ( exitCode!=0) {
                throw new Exception(backup.StandardError);
            }
            return filename;
        }

        private string getPgDumpArgs(string? fileName=null) {
            var fn = fileName!=null ? $"-f {fileName}" : "";
            var args = $"-h {Program.DB_HOST} -p {Program.DB_PORT} -U {Program.DB_USER} {fn} -Fc {Program.DB_NAME}";
            return args;
        }

        private string getPgDump() {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                var installDirectory = getWindowsPostgresBinFolder();
                var path = Path.Combine(installDirectory,"pg_dump.exe");
                if ( File.Exists(path)) {
                    return path;
                } else {
                    throw new Exception($"Could not find pg_dump at [{path}]");
                }
            } else {
                // explicitly using /usr/bin to ensure it picks up v14.
                // installing gdal brings in postgres12 which then means "pg_dump" is v12
                return "/usr/bin/pg_dump";
            }
        }

        private string getPgRestore() {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                var installDirectory = getWindowsPostgresBinFolder();
                var path = Path.Combine(installDirectory,"pg_restore.exe");
                if ( File.Exists(path)) {
                    return path;
                } else {
                    throw new Exception($"Could not find pg_restore at [{path}]");
                }
            } else {
                // explicitly using /usr/bin to ensure it picks up v14.
                // installing gdal brings in postgres12 which then means "pg_dump" is v12
                return "/usr/bin/pg_restore";
            }
        }

        private string getWindowsPostgresBinFolder() {
            var installDirectory = "C:\\Program Files\\PostgreSQL";
            var dirs = Directory.EnumerateDirectories(installDirectory);
            int version=0;
            foreach( var dir in dirs) {
                var cpnts = dir.Split(Path.DirectorySeparatorChar);
                if ( cpnts.Length>0 && int.TryParse(cpnts[cpnts.Length-1], out int testVersion) && testVersion>version) {
                    version = testVersion;
                }
            }
            if (version>0) {
                var path = Path.Combine(installDirectory,version.ToString(),"bin");
                if ( Directory.Exists(path)) {
                    return path;
                } else {
                    throw new Exception($"Could not find PostgreSQL bin directory");
                }
            } else {
                throw new Exception($"Could not find valid PostgreSQL install at {installDirectory}");
            }                
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
            int port = 2360;
            var connectionInfo = new Renci.SshNet.ConnectionInfo(hostName, port,
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

        public StreamReader BackupToStream(out string filename, ApplicationGroup appGroup=ApplicationGroup.All) {

            List<string> classNames = null;
            if ( appGroup != ApplicationGroup.All) {
                classNames = ApplicationGroupAttribute.GetTableNames(appGroup);
            }

            var dbName = Program.DB_NAME;
            var args = getPgDumpArgs();

            if ( classNames!=null) {
                args+=" --table=";
                args += string.Join(" --table=",classNames);
            }

            var now = DateTime.Now;
            var ts = now.ToString("yyyy-MMM-dd-HH-mm-ss");
            var append = appGroup==ApplicationGroup.All ? "": $" ({appGroup})";
            filename = $"{dbName}-{ts}{append}.dump";

            var exe = getPgDump();
            ProcessStartInfo oInfo = new ProcessStartInfo(exe, args);
            oInfo.EnvironmentVariables["PGPASSWORD"] = Program.DB_PASSWORD;
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;

			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;

			StreamReader srOutput = null;
			StreamReader srError = null;

			Process proc = System.Diagnostics.Process.Start(oInfo);

			srOutput = proc.StandardOutput;
            if ( proc.HasExited) {
                var stdErr = proc.StandardError.ReadToEnd();
                throw new Exception($"Failed to backup db: [{stdErr}]");
            }
			return srOutput;            
        }

        public void Restore(IFormFile file) {

            // Note this implementation deletes tables explicitly first as found that using --clean option with pg_restore
            // didn't do a cascade delete and had errors with repeated keys when restoring

            // Check its a .dump file we are restoring
            if ( !file.FileName.Contains(".dump")) {
                throw new Exception("Can only restore a .dump file");
            }

            // Figure out what tables are included in the dump based on filename
            ApplicationGroup appGroup;
            if ( file.FileName.Contains('(')) {
                appGroup = 0;
                if ( file.FileName.Contains("Elsi") ) {
                    appGroup = ApplicationGroup.Elsi;
                }
                if ( file.FileName.Contains("BoundCalc") ) {
                    appGroup |= ApplicationGroup.BoundCalc;
                }
            } else {
                appGroup = ApplicationGroup.All;
            }

            // Delete all tables based on this application group
            List<string> tableNames = null;
            tableNames = ApplicationGroupAttribute.GetTableNames(appGroup);
            foreach( var tableName in tableNames) {
                DataAccessBase.DeleteTable(tableName);
            }

            // Now restore the .dump file
            var restore = new Execute();
            var pgRestore = getPgRestore();  
            var args = getPgRestoreArgs();

            ProcessStartInfo oInfo = new ProcessStartInfo(pgRestore, args);
            oInfo.EnvironmentVariables["PGPASSWORD"] = Program.DB_PASSWORD;
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;

			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;
            oInfo.RedirectStandardInput = true;

            var inputStream = file.OpenReadStream();
            var readBuffer = new byte[8196];
			Process proc = System.Diagnostics.Process.Start(oInfo);
            int bRead;
            while( (bRead = inputStream.Read(readBuffer) )!=0 ) {                
                if ( proc.HasExited ) {
                    break;
                } else {
                    proc.StandardInput.BaseStream.Write(readBuffer,0,bRead);
                }
            }
            proc.StandardInput.BaseStream.Flush();
            proc.StandardInput.Close();
            proc.WaitForExit();
            if ( proc.ExitCode!=0 ) {
                var stdErr = proc.StandardError.ReadToEnd();
                throw new Exception($"Failed to restore db: [{stdErr}]");
            }
          
        }

        private string getPgRestoreArgs() {
            var args = $"--clean --if-exists -h {Program.DB_HOST} -p {Program.DB_PORT} -U {Program.DB_USER} -d {Program.DB_NAME}";
            return args;
        }

    }
}