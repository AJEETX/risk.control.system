using Microsoft.AspNetCore.DataProtection;
using risk.control.system.StartupExtensions;
using Serilog;

AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromMilliseconds(100)); // process-wide setting
//QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;
// Use a path that exists on Azure Windows or Linux App Service
var keysPath = env.IsDevelopment()
    ? "/app/DataProtection-Keys"
    : Path.Combine(env.ContentRootPath, "DataProtection-Keys");

if (!Directory.Exists(keysPath)) Directory.CreateDirectory(keysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("iCheckify");
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

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

builder.Services.AddConfigureServices(builder.Configuration);

builder.Services.AddBusinessServices(builder.Configuration);

builder.Services.AddDatastoreServices(builder.Configuration, env);

builder.Services.AddAwsServices(builder.Configuration);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestHeadersTotalSize = 32768;

    if (!env.IsDevelopment())
    {
        // Azure App Service usually injects a "PORT" environment variable
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        serverOptions.ListenAnyIP(int.Parse(port));
    }
});

builder.Services.AddAuthAndSecurity(builder.Configuration);

try
{
    var app = builder.Build();

    await app.UseServices(builder.Configuration);
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    await app.RunAsync();
}
catch (Exception ex)
{
    await File.WriteAllTextAsync("start.txt", ex.ToString());
    throw;
}