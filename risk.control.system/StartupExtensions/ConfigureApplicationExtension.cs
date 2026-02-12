using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;
using AspNetCoreHero.ToastNotification;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using SmartBreadcrumbs.Extensions;

namespace risk.control.system.StartupExtensions
{
    public static class ConfigureApplicationExtension
    {
        public static IServiceCollection AddConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 2048; // Arbitrary units
            });
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);       // 1 year
                options.IncludeSubDomains = true;              // apply to all subdomains
                options.Preload = true;                        // optional, for browser preload lists
            });

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            services.AddBreadcrumbs(Assembly.GetExecutingAssembly(), options =>
            {
                options.TagName = "nav";
                options.TagClasses = "";
                options.OlClasses = "breadcrumb";
                options.LiClasses = "breadcrumb-item";
                options.ActiveLiClasses = "breadcrumb-item active";
            });
            //services.AddWorkflow();
            //services.AddTransient<InvestigationTaskWorkflow>();
            //services.AddTransient<CaseCreateStep>();
            //services.AddTransient<CaseAssignToAgencyStep>();
            //services.AddTransient<CaseWithdrawStep>();
            //services.AddTransient<CaseDeclineStep>();
            //services.AddTransient<CaseAssignToAgentStep>();
            //services.AddTransient<CaseAgentReportSubmitted>();
            //services.AddTransient<CaseReAssignedToAgentStep>();
            //services.AddTransient<CaseInvestigationReportSubmitted>();
            //services.AddTransient<CaseApproved>();
            //services.AddTransient<CaseRejected>();

            services.AddNotyf(config =>
            {
                config.DurationInSeconds = 2;
                config.IsDismissable = true;
                config.Position = NotyfPosition.TopCenter;
            });

            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .WithOrigins(allowedOrigins!)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // For FileUpload
            services.Configure<FormOptions>(x =>
            {
                x.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB
            });

            services.AddRateLimiter(options =>
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
                            PermitLimit = 500,               // ⬅ max requests
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });
            });

            return services;
        }
    }
}