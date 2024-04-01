using System.Reflection;
using System.Threading.RateLimiting;

using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using NToastNotify;

using risk.control.system.Data;
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
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromSeconds(12);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IClaimsInvestigationService, ClaimsInvestigationService>();
builder.Services.AddScoped<IInvestigationReportService, InvestigationReportService>();
builder.Services.AddScoped<IClaimsVendorService, ClaimsVendorService>();
builder.Services.AddScoped<IEmpanelledAgencyService, EmpanelledAgencyService>();
builder.Services.AddScoped<IClaimPolicyService, ClaimPolicyService>();
builder.Services.AddScoped<IICheckifyService, ICheckifyService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IFtpService, FtpService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMailboxService, MailboxService>();

builder.Services.AddScoped<IInboxMailService, InboxMailService>();
builder.Services.AddScoped<ISentMailService, SentMailService>();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddScoped<ITrashMailService, TrashMailService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
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
                        options.UseSqlite("Data Source=x-edlweiss_1_0_0_2.db"));
}

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

builder.Services.AddIdentityCore<VendorApplicationUser>(o => o.User.RequireUniqueEmail = true)
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddIdentityCore<ClientCompanyApplicationUser>(o => o.User.RequireUniqueEmail = true)
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


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events.OnRedirectToLogin = (context) =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        //options.Cookie.Name = Guid.NewGuid().ToString() + "authCookie";
        //options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
        options.Cookie.HttpOnly = true;
        // Only use this when the sites are on different domains
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Domain = "check.azurewebsites.com";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddSwaggerGen(c =>
{
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

var app = builder.Build();
app.UseSwagger();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
//app.UseStatusCodePagesWithRedirects("/Home/Error?code={0}");
app.UseHttpsRedirection();

await DatabaseSeed.SeedDatabase(app);

//app.UseHttpLogging();
//app.UseResponseCaching();
//app.UseResponseCompression();
app.UseStaticFiles();

app.UseCookiePolicy(
    new CookiePolicyOptions
    {
        Secure = CookieSecurePolicy.Always,
        HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
        MinimumSameSitePolicy = SameSiteMode.Strict
    });

app.UseRouting();
app.UseRateLimiter();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
    context.Response.Headers.Add("X-Xss-Protection", "1; mode=block");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(self)");

    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self';" +
        "connect-src 'self' wss: https://maps.googleapis.com; " +
        "script-src 'unsafe-inline' 'self' https://maps.googleapis.com https://polyfill.io https://highcharts.com https://export.highcharts.com https://cdnjs.cloudflare.com; " +
        "style-src 'unsafe-inline' 'self' https://cdnjs.cloudflare.com/ https://fonts.googleapis.com https://stackpath.bootstrapcdn.com; " +
        "font-src  'self'  https://fonts.gstatic.com https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com; " +
        "img-src 'self'  data: blob: https://maps.gstatic.com https://maps.googleapis.com https://hostedscan.com https://highcharts.com https://export.highcharts.com; " +
        "frame-src 'none';" +
        "media-src 'self' blob: https:;" +
        "object-src 'none';" +
        "form-action 'self';" +
        "frame-ancestors 'self' https://maps.googleapis.com;" +
        "upgrade-insecure-requests;");

    await next();
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseNToastNotify();
app.UseNotyf();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();