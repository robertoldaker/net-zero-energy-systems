using System.Reflection;
using CommonInterfaces.Models;

namespace SmartEnergyLabDataApi.Models
{
    public class AppEnvironment
    {
        private static AppEnvironment _instance;

        public static void Initialise(IWebHostEnvironment hostingEnvironment)
        {
            _instance = new AppEnvironment(hostingEnvironment);
        }

        public static AppEnvironment Instance
        {
            get
            {
                if (_instance == null) {
                    throw new Exception("Please call Initialise to create the singleton instance");
                } else {
                    return _instance;
                }
            }
        }

        private Context _appContext;
        // maintenance mode stuff
        private const string MAINTENANCE_MODE_FILENAME = "maintenance_mode.txt";
        private bool _maintenanceMode;
        private string _activeFilename;
        private string _inactiveFilename;

        public AppEnvironment(IWebHostEnvironment hostingEnvironment)
        {
            // Workout whether we are in maintenance mode and set the filename for the file
            // that controls this
            string rootFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _activeFilename = Path.Combine(rootFolder, MAINTENANCE_MODE_FILENAME);
            _inactiveFilename = Path.Combine(rootFolder, "_" + MAINTENANCE_MODE_FILENAME);
            _maintenanceMode = File.Exists(_activeFilename);
            // This means we have had an install when in maintenance mode so remove the newly copied file
            if ( _maintenanceMode && File.Exists(_inactiveFilename))
            {
                File.Delete(_inactiveFilename);
            }

            if ( hostingEnvironment.IsProduction()) {
                _appContext = Context.Production;
            } else if ( hostingEnvironment.IsStaging()) {
                _appContext = Context.Staging;
            } else if ( hostingEnvironment.IsDevelopment()) {
                _appContext = Context.Development;
            } else {
                throw new Exception($"Unknown hosting environment: {hostingEnvironment.EnvironmentName}");
            }
        }

        public Context Context
        {
            get
            {
                return _appContext;
            }
        }

        public bool IsInMaintenanceMode
        {
            get
            {
                return _maintenanceMode;
            }
        }

        public void SetMaintenanceMode(bool value)
        {
            if ( value && File.Exists(_inactiveFilename) )
            {
                File.Move(_inactiveFilename, _activeFilename);
            } 
            else if ( !value && File.Exists(_activeFilename))
            {
                File.Move(_activeFilename, _inactiveFilename);
            }
            _maintenanceMode = File.Exists(_activeFilename);
            //
            //??NotificationHubSender.Instance.SendMaintenanceModeUpdateEvent(_maintenanceMode);
        }
    }
}