using System.Net;
using System.Reflection;
using System.Text;

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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
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

var builder = WebApplication.CreateBuilder(args);

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
    x.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB
    //x.ValueLengthLimit = 20000000; //not recommended value
    //x.MemoryBufferThreshold = 20000000;
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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddFeatureManagement().AddFeatureFilter<TimeWindowFilter>();
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
builder.Services.AddScoped<IAgentIdService, AgentIdService>();
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
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGoogleService, GoogleService>();
builder.Services.AddScoped<ICustomApiCLient, CustomApiClient>();
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
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IFtpService, FtpService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFaceMatchService, FaceMatchService>();
builder.Services.AddScoped<IGoogleApi, GoogleApi>();
builder.Services.AddScoped<IGoogleMaskHelper, GoogleMaskHelper>();
builder.Services.AddScoped<IChatSummarizer, OpenAISummarizer>();

builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
//builder.Services.AddTransient<CustomCookieAuthenticationEvents>();

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


// Add services to the container.
builder.Services.AddControllersWithViews()
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

//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//                        options.UseSqlServer(connectionString));

var connectionString = builder.Configuration.GetConnectionString("Database");
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

    app.UseNotyf();
    app.UseFileServer();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}");

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