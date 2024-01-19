using EvDemandService.Models;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Create app environment and logger
AppEnvironment.Initialise(builder.Environment);
Logger.Initialise(builder.Environment.ContentRootPath);
Logger.Instance.LogInfoEvent($"Starting EV demand service...");

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    c => {
            c.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "EV Demand API",
                    Description = "Api to provide EV demand support Smart Energy Lab data",
                    Version = "v1.0"
                }
             );
            //var exeName = System.AppDomain.CurrentDomain.FriendlyName;
            //var filePath = Path.Combine(System.AppContext.BaseDirectory, $"{exeName}.xml");
            //c.IncludeXmlComments(filePath);
    }
);

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
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseCors(corsPolicyName);

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationHub>("/NotificationHub");

// get signalR context and initiialise singletons needing it
var  hubContext = (IHubContext<NotificationHub>) app.Services.GetService(typeof(IHubContext<NotificationHub>));
if ( hubContext==null ) {
    throw new Exception("IHubContext<NotificationHub> is null");
}
EVDemandRunner.Initialise(builder.Environment.ContentRootPath,hubContext);
AdminModel.Initialise(hubContext);


app.Run();
