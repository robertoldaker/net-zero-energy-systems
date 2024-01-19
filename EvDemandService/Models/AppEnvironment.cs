using System.Reflection;

namespace EvDemandService.Models
{
    public enum Context { Development, Staging, Production }
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
        
        public AppEnvironment(IWebHostEnvironment hostingEnvironment)
        {
            // Workout whether we are in maintenance mode and set the filename for the file
            // that controls this
            string rootFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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

    }
}