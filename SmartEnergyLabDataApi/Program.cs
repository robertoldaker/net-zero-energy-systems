using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using Npgsql;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

public static class Program
{
    // Start the data access - this will check schema and run any startup scripts as needed
    private const int SCHEMA_VERSION = 33;
    private const int SCRIPT_VERSION = 6;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        //
        AppEnvironment.Initialise(builder.Environment);
        Logger.Initialise(builder.Environment.ContentRootPath);
        AppFolders.Initialise(builder.Environment.ContentRootPath, builder.Environment.WebRootPath);        

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
                                      builder.WithOrigins("http://localhost:44463","http://odin.local:5021","https://lv-app.net-zero-energy-systems.org")
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

        var app = builder.Build();


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

        var backgroundTasks = app.Services.GetService<IBackgroundTasks>();
        if ( backgroundTasks!=null ) {
            ClassificationToolBackgroundTask.Register(backgroundTasks);
            DatabaseBackupBackgroundTask.Register(backgroundTasks);
            LoadNetworkDataBackgroundTask.Register(backgroundTasks);
        }
        

#if DEBUG
        string host = "localhost";
#else
        string host = "localhost";
#endif
        DataAccessBase.Initialise(new DbConnection(SCHEMA_VERSION, SCRIPT_VERSION)
        {
            Server = host,
            DatabaseName = "smart_energy_lab",
            Username = "smart_energy_lab",
            Password = "1234567890",
            DbProvider = DbProvider.PostgreSQL
        }, DataAccess.SchemaUpdated, StartupScript.Run);

        // Needed to read spreadsheets
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        Logger.Instance.LogInfoEvent($"Starting AngelBooks...");
        app.Run();

    }
}

