using System.Configuration;
using System.Reflection;

using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
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
    x.MultipartBodyLengthLimit = 1000000; // In case of multipart
    x.ValueLengthLimit = 1000000; //not recommended value
    x.MemoryBufferThreshold = 1000000;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClaimsInvestigationService, ClaimsInvestigationService>();
builder.Services.AddScoped<IICheckifyService, ICheckifyService>();
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
builder.Services.AddTransient<IMailService, MailService>();
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

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//         options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite("Data Source=mobile.db"));

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
    });
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
//                {
//                    options.Events.OnRedirectToLogin = (context) =>
//                    {
//                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                        return Task.CompletedTask;
//                    };
//                    options.Cookie.Name = "UserLoginCookie";
//                    options.LoginPath = "/Account/Login";
//                    options.LogoutPath = "/Account/Logout"; // If the LogoutPath is not set here, ASP.NET Core will default to /Account/Logout
//                    options.AccessDeniedPath = "/Account/AccessDenied"; // If the AccessDeniedPath is not set here, ASP.NET Core will default to /Account/AccessDenied
//                    options.Cookie.HttpOnly = true;
//                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
//                    options.Cookie.SameSite = SameSiteMode.None;
//                    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
//                    options.SlidingExpiration = true;
//                });

//builder.Services.AddAuthorization(options =>
//{
//    options.FallbackPolicy = new AuthorizationPolicyBuilder()
//        .RequireAuthenticatedUser()
//        .Build();
//});
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

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}
//app.UseStatusCodePagesWithRedirects("/Home/Error?code={0}");
app.UseHttpsRedirection();

await DatabaseSeed.SeedDatabase(app);

app.UseHttpLogging();

app.UseStaticFiles();

app.UseRouting();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});
//app.UseCookiePolicy(
//    new CookiePolicyOptions
//    {
//        Secure = CookieSecurePolicy.Always
//    });
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseNToastNotify();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();