using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

using Amazon;
using Amazon.Rekognition;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.Textract;
using Amazon.TranscribeService;

using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;

using Hangfire;
using Hangfire.MemoryStorage;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Middleware;
using risk.control.system.Models;
using risk.control.system.Permission;
using risk.control.system.Services;

using SmartBreadcrumbs.Extensions;

using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromMilliseconds(100)); // process-wide setting

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);       // 1 year
    options.IncludeSubDomains = true;              // apply to all subdomains
    options.Preload = true;                        // optional, for browser preload lists
});
var env = builder.Environment;

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

builder.Services.AddBreadcrumbs(Assembly.GetExecutingAssembly(), options =>
{
    options.TagName = "nav";
    options.TagClasses = "";
    options.OlClasses = "breadcrumb";
    options.LiClasses = "breadcrumb-item";
    options.ActiveLiClasses = "breadcrumb-item active";
});
// Set up logging
var logDirectory = LogSetup.CreateLogging(builder.Environment.ContentRootPath);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Error); // Optional global filter
builder.Logging.AddProvider(new CsvLoggerProvider(logDirectory, LogLevel.Error));

//builder.Services.AddWorkflow();
//builder.Services.AddTransient<InvestigationTaskWorkflow>();
//builder.Services.AddTransient<CaseCreateStep>();
//builder.Services.AddTransient<CaseAssignToAgencyStep>();
//builder.Services.AddTransient<CaseWithdrawStep>();
//builder.Services.AddTransient<CaseDeclineStep>();
//builder.Services.AddTransient<CaseAssignToAgentStep>();
//builder.Services.AddTransient<CaseAgentReportSubmitted>();
//builder.Services.AddTransient<CaseReAssignedToAgentStep>();
//builder.Services.AddTransient<CaseInvestigationReportSubmitted>();
//builder.Services.AddTransient<CaseApproved>();
//builder.Services.AddTransient<CaseRejected>();

//builder.Services.AddCors(opt =>
//{
//    opt.AddDefaultPolicy(builder =>
//    {
//        builder.AllowAnyOrigin()
//        .AllowAnyHeader()
//            .AllowAnyMethod();
//    });
//});
// For FileUpload
builder.Services.Configure<FormOptions>(x =>
{
    x.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsync(
            """
            {
                "error": "Too many requests. Please try again later."
            }
            """,
            token);
    };

    options.AddPolicy("PerUserOrIP", context =>
    {
        // 1️⃣ Try authenticated user
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        // 2️⃣ Fallback to IP
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var partitionKey = userId ?? ip;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 50,               // ⬅ max requests
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAnswerService, AnswerService>();
builder.Services.AddScoped<IMediaIdfyService, MediaIdfyService>();
builder.Services.AddScoped<IDocumentIdfyService, DocumentIdfyService>();
builder.Services.AddScoped<IAgentFaceIdfyService, AgentFaceIdfyService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IVendorUserService, VendorUserService>();
builder.Services.AddScoped<ICompanyUserService, CompanyUserService>();
builder.Services.AddScoped<IVendorServiceTypeManager, VendorServiceTypeManager>();
builder.Services.AddScoped<IAgencyUserCreateEditService, AgencyUserCreateEditService>();
builder.Services.AddScoped<IAgencyCreateEditService, AgencyCreateEditService>();
builder.Services.AddScoped<IValidateImageService, ValidateImageService>();
builder.Services.AddScoped<IBeneficiaryCreateEditService, BeneficiaryCreateEditService>();
builder.Services.AddScoped<ICustomerCreateEditService, CustomerCreateEditService>();
builder.Services.AddScoped<ICaseCreateEditService, CaseCreateEditService>();
builder.Services.AddScoped<IWeatherInfoService, WeatherInfoService>();
builder.Services.AddScoped<IManagerService, ManagerService>();
builder.Services.AddScoped<IAddInvestigationService, AddInvestigationService>();
builder.Services.AddScoped<IAssessorService, AssessorService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<ISanitizerService, SanitizerService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddFeatureManagement().AddFeatureFilter<TimeWindowFilter>();
builder.Services.AddScoped<IPhoneService, PhoneService>();
builder.Services.AddScoped<IMediaDataService, MediaDataService>();
builder.Services.AddScoped<ITinyUrlService, TinyUrlService>();
builder.Services.AddScoped<IPdfGenerateQuestionLocationService, PdfGenerateQuestionLocationService>();
builder.Services.AddScoped<IPdfGenerateDocumentLocationService, PdfGenerateDocumentLocationService>();
builder.Services.AddScoped<IPdfGenerateFaceLocationService, PdfGenerateFaceLocationService>();
builder.Services.AddScoped<IPdfGenerateAgentLocationService, PdfGenerateAgentLocationService>();
builder.Services.AddScoped<IPdfGenerateDetailReportService, PdfGenerateDetailReportService>();
builder.Services.AddScoped<IPdfGenerateCaseDetailService, PdfGenerateCaseDetailService>();
builder.Services.AddScoped<IPdfGenerateDetailService, PdfGenerateDetailService>();
builder.Services.AddScoped<IPdfGenerativeService, PdfGenerativeService>();
builder.Services.AddScoped<IViewRenderService, ViewRenderService>();
builder.Services.AddScoped<IPanCardService, PanCardService>();
builder.Services.AddScoped<ICloneReportService, CloneReportService>();
builder.Services.AddScoped<IAgentIdfyService, AgentIdfyService>();
builder.Services.AddScoped<ICaseVendorService, CaseVendorService>();
builder.Services.AddScoped<IVendorInvestigationService, VendorInvestigationService>();
builder.Services.AddScoped<IDashboardCountService, DashboardCountService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IProcessCaseService, ProcessCaseService>();
builder.Services.AddScoped<IInvestigationService, InvestigationService>();
builder.Services.AddScoped<IHangfireJobService, HangfireJobService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<ICaseCreationService, CaseCreationService>();
builder.Services.AddScoped<ICaseDetailCreationService, CaseDetailCreationService>();
builder.Services.AddScoped<ICustomerCreationService, CustomerCreationService>();
builder.Services.AddScoped<IBeneficiaryCreationService, BeneficiaryCreationService>();
builder.Services.AddScoped<ICaseImageCreationService, CaseImageCreationService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICustomApiClient, CustomApiClient>();
builder.Services.AddScoped<IAgencyService, AgencyService>();
builder.Services.AddScoped<IClaimsAgentService, ClaimsAgentService>();
builder.Services.AddScoped<ICompareFaces, CompareFaces>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<INumberSequenceService, NumberSequenceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IClaimsInvestigationService, ClaimsInvestigationService>();
builder.Services.AddScoped<IEmpanelledAgencyService, EmpanelledAgencyService>();
builder.Services.AddScoped<ICaseService, CaseService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IFtpService, FtpService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFaceMatchService, FaceMatchService>();
builder.Services.AddScoped<IGoogleApi, GoogleApi>();
builder.Services.AddScoped<IGoogleMaskHelper, GoogleMaskHelper>();

builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
//builder.Services.AddTransient<CustomCookieAuthenticationEvents>();

var awsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
{
    Credentials = new BasicAWSCredentials(Environment.GetEnvironmentVariable("aws_id"), Environment.GetEnvironmentVariable("aws_secret")),
    Region = RegionEndpoint.APSoutheast2 // Specify the region as needed,
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


// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI()
    .AddRazorRuntimeCompilation()
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

var connectionString = "Data Source=" + Environment.GetEnvironmentVariable("COUNTRY") + "_" + builder.Configuration.GetConnectionString("Database");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite(connectionString,
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.Queues = new[] { "default", "emails", "critical" };
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
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
builder.Services.AddAuthentication(options =>
{
    // Use Cookies as the default for the browser/UI
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
// 2. Add Microsoft Entra ID (Azure AD)
// This handles its own cookie internally but links to the OIDC scheme
.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"),
    OpenIdConnectDefaults.AuthenticationScheme,
    CookieAuthenticationDefaults.AuthenticationScheme)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// 3. Add JWT Bearer for API calls (if needed)
builder.Services.AddAuthentication()
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.Zero
        };
    });
// 4. Configure the Identity Cookie (Do this AFTER AddAuthentication)
builder.Services.ConfigureApplicationCookie(options =>
{
    // Your custom expiration logic
    options.Events.OnValidatePrincipal = async context => { /* ... your logic ... */ };

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = AppCookie.AUTH_COOKIE_NAME;
    options.LoginPath = AppCookie.LOGIN_PATH;
    options.LogoutPath = AppCookie.LOGOUT_PATH;
    options.AccessDeniedPath = AppCookie.LOGOUT_PATH;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromSeconds(double.Parse(builder.Configuration["SESSION_TIMEOUT_SEC"]));
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = AppCookie.ANTI_FORGERY_COOKIE_NAME; // Set a custom cookie name
    options.Cookie.HttpOnly = true; // Make the cookie HttpOnly
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require secure cookies (only over HTTPS)
    options.Cookie.SameSite = SameSiteMode.Strict; // Apply a strict SameSite policy
    options.HeaderName = "X-CSRF-TOKEN"; // Set a custom header name
    options.FormFieldName = "__RequestVerificationToken"; // Set a custom form field name
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
        Description = @"JWT Authorization header. \r\n\r\n Enter the token in the text input below.",
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
try
{
    var app = builder.Build();


    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new BasicAuthAuthorizationFilter() }
    });
    app.UseMiddleware<RequirePasswordChangeMiddleware>();
    app.UseSwagger();

    if (app.Environment.IsDevelopment())
    {
        // Show detailed error page for devs
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // Redirect to custom error page in production
        app.UseExceptionHandler("/Home/Error");
        //app.UseStatusCodePagesWithRedirects("/Home/HTTP?statusCode={0}");
        app.UseHsts();
    }

    app.UseMiddleware<SecurityMiddleware>(builder.Configuration["HttpStatusErrorCodes"]);

    app.UseHttpsRedirection();

    await risk.control.system.Seeds.DatabaseSeed.SeedDatabase(app);

    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            if (!ctx.Context.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=2592000"; // 30 days
            }
        }
    });

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

    app.UseNotyf();
    app.UseFileServer();

    app.UseRateLimiter();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}")
        .RequireRateLimiting("PerUserOrIP");

    //RecurringJob.AddOrUpdate<IHangfireJobService>(
    //    "clean-failed-jobs",
    //    job => job.CleanFailedJobs(),
    //    Cron.Hourly // Runs every hour
    //);

    int sessionTimeoutMinutes = int.Parse(builder.Configuration["SESSION_TIMEOUT_SEC"]) / 60;
    //RecurringJob.AddOrUpdate<IdleUserService>(
    //    "check-idle-users",
    //    service => service.CheckIdleUsers(),
    //    $"*/{sessionTimeoutMinutes} * * * *"); // Check every 5 minutes

    app.Run();
}
catch (Exception ex)
{
    File.WriteAllText("start.txt", ex.ToString());
    throw;
}