using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.OpenApi.Models;
using Npgsql;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using SmartEnergyLabDataApi.Models;

public static class Program
{
    // Start the data access - this will check schema and run any startup scripts as needed
    private const int SCHEMA_VERSION = 68;
    private const int SCRIPT_VERSION = 9;

    public const string DB_NAME = "smart_energy_lab";
    public const string DB_USER = "smart_energy_lab";
    public const string DB_PASSWORD = "1234567890";
    public static int DB_PORT = 5432;
    public static string DB_HOST = "";


    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        //
        AppEnvironment.Initialise(builder.Environment);
        Logger.Initialise(builder.Environment.ContentRootPath);
        AppFolders.Initialise(builder.Environment.ContentRootPath, builder.Environment.WebRootPath);

        // Add initial entry in log file
        Logger.Instance.LogInfoEvent($"Starting Smart Energy Lab...");

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "Smart Energy Lab Data API",
                    Description = "Api to support Smart Energy Lab data",
                    Version = "v1.0"
                }
             );
            var exeName = System.AppDomain.CurrentDomain.FriendlyName;
            var filePath = Path.Combine(System.AppContext.BaseDirectory, $"{exeName}.xml");
            c.IncludeXmlComments(filePath);
        });

        var corsPolicyName = "allowAll";
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsPolicyName,
                                  builder => {
                                      builder.WithOrigins("http://localhost:44463",
                                                "http://odin.local:5021",
                                                "https://lv-app.net-zero-energy-systems.org",
                                                "http://lv-app-test.net-zero-energy-systems.org",
                                                "https://lv-app-test.net-zero-energy-systems.org",
                                                "http://lv-app-tmp.net-zero-energy-systems.org")
                                            .AllowCredentials()
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                                  });
        });

        // SignalR
        builder.Services.AddSignalR();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options => {
                            //options.LoginPath = "/App/ShowLogOn";
                            options.SlidingExpiration = true;
                            options.ExpireTimeSpan = new TimeSpan(14, 0, 0, 0);
                            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                        });

        // Add our own injectible classes
        builder.Services.AddSingleton<IBackgroundTasks,BackgroundTasks>();
        builder.Services.AddSingleton<ICarbonIntensityFetcher,CarbonIntensityFetcher>();
        builder.Services.AddSingleton<IElectricityCostFetcher,ElectricityCostFetcher>();

        builder.Services.AddControllers(options => options.Filters.Add<ExceptionLoggerFilter>())
                        .AddJsonOptions(options => options.JsonSerializerOptions.NumberHandling =
                                System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals);

        var app = builder.Build();

        // server files from wwwroot
        // Set up custom content types - associating file extension to MIME type
        var provider = new FileExtensionContentTypeProvider();
        // Add new mappings
        provider.Mappings[".geojson"] = "application/geo+json";

        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = provider
        });

        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI();

        // Seems to screw up reponse when using Http?
        //app.UseHttpsRedirection();
        app.UseCors(corsPolicyName);

        app.UseAuthorization();
        app.UseAuthentication();

        app.MapControllers();
        app.MapHub<NotificationHub>("/NotificationHub");

        var  hubContext = (IHubContext<NotificationHub>) app.Services.GetService(typeof(IHubContext<NotificationHub>));
        if ( hubContext==null ) {
            throw new Exception("IHubContext<NotificationHub> is null");
        }
        var backgroundTasks = app.Services.GetService<IBackgroundTasks>();
        if ( backgroundTasks!=null ) {
            ClassificationToolBackgroundTask.Register(backgroundTasks);
            DatabaseBackupBackgroundTask.Register(backgroundTasks);
            LoadDistributionDataBackgroundTask.Register(backgroundTasks);
        }

        var hostName = getDbHostName();
        var cpnts = hostName.Split(':');
        if ( cpnts.Length>1) {
            DB_HOST = cpnts[0];
            DB_PORT = int.Parse(cpnts[1]);
        } else {
            DB_HOST = hostName;
        }
        DataAccessBase.Initialise(new DbConnection(SCHEMA_VERSION, SCRIPT_VERSION)
        {
            Server = hostName,
            DatabaseName = DB_NAME,
            Username = DB_USER,
            Password = DB_PASSWORD,
            DbProvider = DbProvider.PostgreSQL
        }, DataAccess.SchemaUpdated, StartupScript.RunNewVersion);

        AdminModel.Initialise(hubContext);

        StartupScript.RunStartup();

        // Needed to read spreadsheets
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        // BoundCalc
        EditItemModel.AddHandler<Node>(new NodeItemHandler());
        EditItemModel.AddHandler<Zone>(new ZoneItemHandler());
        EditItemModel.AddHandler<Boundary>(new BoundaryItemHandler());
        EditItemModel.AddHandler<Branch>(new BranchItemHandler());
        EditItemModel.AddHandler<Ctrl>(new CtrlItemHandler());
        EditItemModel.AddHandler<GridSubstationLocation>(new GridSubstationLocationItemHandler());
        EditItemModel.AddHandler<Generator>(new GeneratorItemHandler());
        EditItemModel.AddHandler<TransportModel>(new TransportModelItemHandler());
        EditItemModel.AddHandler<TransportModelEntry>(new TransportModelEntryItemHandler());
        // Elsi
        EditItemModel.AddHandler<GenCapacity>(new GenCapacityItemHandler());
        EditItemModel.AddHandler<GenParameter>(new GenParameterItemHandler());
        EditItemModel.AddHandler<Link>(new LinkItemHandler());
        EditItemModel.AddHandler<MiscParams>(new MiscParamsItemHandler());
        EditItemModel.AddHandler<PeakDemand>(new PeakDemandItemHandler());

        app.Run();
    }

    private static string getDbHostName() {
        string host = "localhost";
        #if DEBUG
            // This allows us to have different db locations when debugging
            var dbHostFilename = "debugDbHost.txt";
            if ( File.Exists(dbHostFilename)) {
                using (var sr = new StreamReader(dbHostFilename))
                {
                    // Read the stream as a string, and write the string to the console.
                    string line;
                    while( (line = sr.ReadLine())!=null) {
                        if ( !line.StartsWith("#")) {
                            host=line;
                            break;
                        }
                    }
                }
            }
        #endif
        return host;
    }
}

public class ExceptionLoggerFilter : IActionFilter, IOrderedFilter
{
    public int Order => int.MaxValue - 10;

    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception != null) {
            Logger.Instance.LogException(context.Exception, $"Exception handling [{context.HttpContext.Request.Path}]");
            if (context.Controller is ControllerBase) {
                context.Result = ((ControllerBase)context.Controller).StatusCode(500, context.Exception.Message);
                context.Exception = null;
            }
        }
    }
}

