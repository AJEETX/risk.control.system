using System;
using System.Net;
using System.Reflection;
using System.Text;
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

using Hangfire;
using Hangfire.MemoryStorage;

using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NToastNotify;

using risk.control.system.AppConstant;
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
using Hangfire.SQLite;
using Hangfire.Dashboard;
using risk.control.system.WorkFlow;

var builder = WebApplication.CreateBuilder(args);
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;


builder.Services.AddBreadcrumbs(Assembly.GetExecutingAssembly(), options =>
{
    options.TagName = "nav";
    options.TagClasses = "";
    options.OlClasses = "breadcrumb";
    options.LiClasses = "breadcrumb-item";
    options.ActiveLiClasses = "breadcrumb-item active";
    //options.SeparatorElement = "<li class=\"separator\">/</li>";
});
builder.Services.AddWorkflow();
builder.Services.AddTransient<InvestigationTaskWorkflow>();
builder.Services.AddTransient<CaseCreateStep>();
builder.Services.AddTransient<CaseAssignToAgencyStep>();
builder.Services.AddTransient<CaseWithdrawStep>();
builder.Services.AddTransient<CaseDeclineStep>();
builder.Services.AddTransient<CaseAssignToAgentStep>();
builder.Services.AddTransient<CaseAgentReportSubmitted>();
builder.Services.AddTransient<CaseReAssignedToAgentStep>();
builder.Services.AddTransient<CaseAgencyReportSubmitted>();
builder.Services.AddTransient<CaseApproved>();
builder.Services.AddTransient<CaseRejected>();

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
//builder.Services.AddRateLimiter(_ => _
//    .AddFixedWindowLimiter(policyName: "fixed", options =>
//    {
//        options.PermitLimit = 100;
//        options.Window = TimeSpan.FromSeconds(1);
//        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
//        options.QueueLimit = 10;
//    }));
// forward headers configuration for reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddFeatureManagement().AddFeatureFilter<TimeWindowFilter>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IProcessCaseService, ProcessCaseService>();
builder.Services.AddScoped<IInvestigationService, InvestigationService>();
builder.Services.AddScoped<IHangfireJobService, HangfireJobService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<ICaseCreationService, CaseCreationService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddSingleton<IValidationService,ValidationService>();
builder.Services.AddScoped<ITokenService,TokenService>();
builder.Services.AddScoped<IUserService,UserService>();
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
builder.Services.AddScoped<IChatSummarizer, OpenAISummarizer>();

builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
//builder.Services.AddTransient<CustomCookieAuthenticationEvents>();
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
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
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

var connectionString = builder.Configuration.GetConnectionString("Database");
var HangfireConnectionString = builder.Configuration.GetConnectionString("HangfireDatabase");
var isProd = builder.Configuration.GetSection("IsProd").Value;
var prod = bool.Parse(isProd);
if (prod)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
         options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddHangfire(config => config.UseSQLiteStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite(connectionString));
    builder.Services.AddHangfire(config => config.UseMemoryStorage());
    //builder.Services.AddHangfire(config => config.UseSQLiteStorage(HangfireConnectionString));
}
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.Queues = new[] { "default", "emails", "critical" };
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
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
    options.Events.OnValidatePrincipal = async context =>
    {
        var userPrincipal = context.Principal;

        // Check if the cookie is close to expiring
        var now = DateTimeOffset.UtcNow;
        var issuedUtc = context.Properties.IssuedUtc;
        var expiresUtc = context.Properties.ExpiresUtc;

        var sessionTimeout = double.Parse(builder.Configuration["SESSION_TIMEOUT_SEC"]);
        var renewalThreshold = sessionTimeout * 0.9; // Renew when 90% of the session timeout has passed.

        if (expiresUtc.HasValue && now > expiresUtc.Value.AddSeconds(-renewalThreshold))
        {
            context.Properties.ExpiresUtc = now.AddSeconds(sessionTimeout);
            context.ShouldRenew = true;
        }

        await Task.CompletedTask;
    };
    //options.EventsType = typeof(CustomCookieAuthenticationEvents);
    // General cookie settings
    options.Cookie.HttpOnly = true; // Ensures the cookie cannot be accessed via JavaScript (enhances security).
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensures the cookie is sent only over HTTPS.
    options.Cookie.SameSite = SameSiteMode.Strict; // Prevents the cookie from being sent in cross-site requests (CSRF protection).
    options.Cookie.Name = AppCookie.AUTH_COOKIE_NAME; // Custom name for the authentication cookie.
    options.Cookie.Path = "/"; // Specifies the cookie path.
    options.Cookie.IsEssential = true; // Ensures the cookie is marked as essential, bypassing consent if required.
    // Authentication-specific settings
    options.LoginPath = AppCookie.LOGIN_PATH; // Redirect to this path if the user is not authenticated.
    options.LogoutPath = AppCookie.LOGOUT_PATH; // Redirect to this path after logout.
    options.AccessDeniedPath = AppCookie.LOGOUT_PATH; // Redirect here if the user lacks the required permissions.

    // Expiration settings
    options.SlidingExpiration = true; // Renews the cookie expiration time on every valid request.
    options.ExpireTimeSpan = TimeSpan.FromSeconds(double.Parse(builder.Configuration["SESSION_TIMEOUT_SEC"])); // Sets the lifetime of the cookie.
}).AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            ),
            ClockSkew = TimeSpan.Zero // Reduce delay tolerance for token expiration.
        };

        // Optional: Add token validation events for custom behavior.
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully.");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = AppCookie.ANTI_FORGERY_COOKIE_NAME; // Set a custom cookie name
    options.Cookie.HttpOnly = true; // Make the cookie HttpOnly
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require secure cookies (only over HTTPS)
    options.Cookie.SameSite = SameSiteMode.Strict; // Apply a strict SameSite policy
    options.HeaderName = "X-CSRF-TOKEN"; // Set a custom header name
    options.FormFieldName = "icheckifyAntiforgery"; // Set a custom form field name
    options.SuppressXFrameOptionsHeader = false; // Enable the X-Frame-Options header
});

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

var app = builder.Build();


app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new BasicAuthAuthorizationFilter() }
});
app.UseMiddleware<RequirePasswordChangeMiddleware>();
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
//app.UseRateLimiter();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});

app.UseMiddleware<CookieConsentMiddleware>();

app.UseMiddleware<WhitelistListMiddleware>();

app.UseCors();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LicensingMiddleware>();
app.UseMiddleware<UpdateUserLastActivityMiddleware>();

app.UseNToastNotify();
app.UseNotyf();
app.UseFileServer();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

RecurringJob.AddOrUpdate<IHangfireJobService>(
    "clean-failed-jobs",
    job => job.CleanFailedJobs(),
    Cron.Hourly // Runs every hour
);

int sessionTimeoutMinutes = int.Parse(builder.Configuration["SESSION_TIMEOUT_SEC"]) / 60;
//RecurringJob.AddOrUpdate<IdleUserService>(
//    "check-idle-users",
//    service => service.CheckIdleUsers(),
//    $"*/{sessionTimeoutMinutes} * * * *"); // Check every 5 minutes

app.Run();
public class BasicAuthAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;
        var authorization = request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            // Send 401 response with WWW-Authenticate to trigger login popup
            response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            response.StatusCode = StatusCodes.Status401Unauthorized;
            return false;
        }

        try
        {
            // Decode Authorization header (Base64 username:password)
            var encodedCredentials = authorization.Substring(6); // Remove "Basic "
            var decodedAuthHeader = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var credentials = decodedAuthHeader.Split(':');

            if (credentials.Length != 2)
                return false;

            string username = "admin", password = "admin";

#if !DEBUG
            username = Environment.GetEnvironmentVariable("SMS_User");
            password = Environment.GetEnvironmentVariable("SMS_Pwd");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Environment variables not set properly!");
                return false;
            }
#endif

            // Check if credentials match
            return credentials[0] == username && credentials[1] == password;
        }
        catch
        {
            return false; // Handle malformed authorization header
        }
    }
}