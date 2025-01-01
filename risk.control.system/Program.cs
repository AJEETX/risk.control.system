using System;
using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;

using Amazon;
using Amazon.Rekognition;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Textract;
using Amazon.TranscribeService;

using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;

using Google.Api;

using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.OpenApi.Models;

using NToastNotify;

using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Middleware;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Permission;
using risk.control.system.Seeds;
using risk.control.system.Services;

using SmartBreadcrumbs.Extensions;

using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBreadcrumbs(Assembly.GetExecutingAssembly(), options =>
{
    options.TagName = "nav";
    options.TagClasses = "";
    options.OlClasses = "breadcrumb";
    options.LiClasses = "breadcrumb-item";
    options.ActiveLiClasses = "breadcrumb-item active";
    //options.SeparatorElement = "<li class=\"separator\">/</li>";
});

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
        .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// For FileUpload
builder.Services.Configure<FormOptions>(x =>
{
    x.MultipartBodyLengthLimit = 5000000; // In case of multipart
    x.ValueLengthLimit = 5000000; //not recommended value
    x.MemoryBufferThreshold = 5000000;
});
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromSeconds(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    }));
// forward headers configuration for reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddFeatureManagement().AddFeatureFilter<TimeWindowFilter>();
builder.Services.AddScoped<IClaimCreationService,ClaimCreationService>();
builder.Services.AddScoped<IGoogleService, GoogleService>();
builder.Services.AddScoped<ICustomApiCLient, CustomApiClient>();
builder.Services.AddScoped<IAgencyService, AgencyService>();
builder.Services.AddScoped<IClaimsAgentService, ClaimsAgentService>();
builder.Services.AddScoped<ICompareFaces, CompareFaces>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<ICreatorService, CreatorService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<INumberSequenceService, NumberSequenceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IClaimsInvestigationService, ClaimsInvestigationService>();
builder.Services.AddScoped<IInvestigationReportService, InvestigationReportService>();
builder.Services.AddScoped<IClaimsVendorService, ClaimsVendorService>();
builder.Services.AddScoped<IEmpanelledAgencyService, EmpanelledAgencyService>();
builder.Services.AddScoped<IClaimPolicyService, ClaimPolicyService>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<IICheckifyService, ICheckifyService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IFtpService, FtpService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMailboxService, MailboxService>();
builder.Services.AddScoped<IFaceMatchService, FaceMatchService>();
builder.Services.AddScoped<IGoogleApi, GoogleApi>();
builder.Services.AddScoped<IGoogleMaskHelper, GoogleMaskHelper>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IChatSummarizer, OpenAISummarizer>();

builder.Services.AddScoped<IInboxMailService, InboxMailService>();
builder.Services.AddScoped<ISentMailService, SentMailService>();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddScoped<ITrashMailService, TrashMailService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddTransient<CustomCookieAuthenticationEvents>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

var awsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
{
    Credentials = new BasicAWSCredentials(Environment.GetEnvironmentVariable("aws_id"), Environment.GetEnvironmentVariable("aws_secret")),
    Region = Amazon.RegionEndpoint.APSoutheast2 // Specify the region as needed,
};
builder.Services.AddDefaultAWSOptions(awsOptions);
// Register AWS Transcribe Service with the configured options
builder.Services.AddAWSService<IAmazonTranscribeService>();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonRekognition>();
builder.Services.AddAWSService<IAmazonTextract>();

AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
AWSConfigs.LoggingConfig.LogMetrics = true;
AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.Always;


//builder.Services.AddTransient<IMailService, MailService>();
// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation()
    .AddNToastNotifyNoty(new NotyOptions
    {
        ProgressBar = true,
        Timeout = 1000,
        Modal = true,
        Type = Enums.NotificationTypesNoty.Info
    })
    .AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
);
//builder.Services.AddSignalR();
builder.Services.AddNotyf(config =>
{
    config.DurationInSeconds = 2;
    config.IsDismissable = true;
    config.Position = NotyfPosition.TopCenter;
});

var isProd = builder.Configuration.GetSection("IsProd").Value;
var prod = bool.Parse(isProd);
if (prod)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
         options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite(builder.Configuration.GetConnectionString("Database")));
}

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

builder.Services.AddIdentityCore<VendorApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddIdentityCore<ClientCompanyApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    //options.EventsType = typeof(CustomCookieAuthenticationEvents);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Logout";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromSeconds(double.Parse(builder.Configuration["SESSION_TIMEOUT_SEC"]));
}).AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AddRequiredHeaderParameter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =@"JWT Authorization header. \r\n\r\n Enter the token in the text input below.",
    });
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "iCheckify Utility",
        Description = "iCheckify API ",
        TermsOfService = new Uri("https://icheckify.co.in"),
        Contact = new OpenApiContact
        {
            Name = "iCheckify Team",
            Email = "hi@icheckify.co.in",
            Url = new Uri("https://icheckify.co.in"),
        },
        License = new OpenApiLicense
        {
            Name = "Use under OpenApiLicense",
            Url = new Uri("https://icheckify.co.in"),
        }
    });
});
builder.Services.AddMvcCore(config =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    config.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddHttpContextAccessor();

//builder.Services.AddWebSockets(options =>
//{
//    options.KeepAliveInterval = TimeSpan.FromSeconds(120);
//});
var app = builder.Build();
//app.UseMiddleware<UpdateUserLastActivityMiddleware>();
//app.UseWebSockets();
app.UseSwagger();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseMiddleware<SecurityMiddleware>(builder.Configuration["HttpStatusErrorCodes"]);

//app.UseStatusCodePagesWithRedirects("/Home/Error?code={0}");
app.UseHttpsRedirection();

await DatabaseSeed.SeedDatabase(app);

app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});


app.UseMiddleware<WhitelistListMiddleware>();

app.UseCors();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LicensingMiddleware>();

app.UseNToastNotify();
app.UseNotyf();
app.UseFileServer();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

//app.MapHub<ChatHub>("/chatHub");

app.Run();

public partial class Program
{

}