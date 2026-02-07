using risk.control.system.StartupExtensions;
using Serilog;

AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromMilliseconds(100)); // process-wide setting
//QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;
// Set up logging

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "iCheckify")
    .WriteTo.File(
        path: "Logs/log-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        formatter: new Serilog.Formatting.Json.JsonFormatter()
    )
    .CreateLogger();

builder.Host.UseSerilog();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

builder.Services.AddBusinessServices(builder.Configuration);

builder.Services.AddDatastoreServices(builder.Configuration, env);

builder.Services.AddAwsServices(builder.Configuration);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestHeadersTotalSize = 32768;
});

builder.Services.AddAuthAndSecurity(builder.Configuration);

try
{
    var app = builder.Build();

    await app.UseServices(builder.Configuration);
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

    await app.RunAsync();
}
catch (Exception ex)
{
    await File.WriteAllTextAsync("start.txt", ex.ToString());
    throw;
}