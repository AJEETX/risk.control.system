using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;

using AspNetCoreHero.ToastNotification;

using Hangfire;
using Hangfire.MemoryStorage;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Models;
using risk.control.system.Permission;
using risk.control.system.Services;

using SmartBreadcrumbs.Extensions;
namespace risk.control.system.StartupExtensions;

public static class BusinessServiceExtension
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024; // Arbitrary units
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

        services.AddCors(opt =>
        {
            opt.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                .AllowAnyHeader()
                    .AllowAnyMethod();
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

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        services.AddHttpClient();
        services.AddScoped<IInvestigationDetailService, InvestigationDetailService>();
        services.AddScoped<IPolicyProcessor, PolicyProcessor>();
        services.AddScoped<IVerifierProcessor, VerifierProcessor>();
        services.AddScoped<ICustomerValidator, CustomerValidator>();
        services.AddScoped<IUploadFileDataProcessor, UploadFileDataProcessor>();
        services.AddScoped<IExtractorService, ExtractorService>();
        services.AddScoped<IBeneficiaryValidator, BeneficiaryValidator>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IUploadFileInitiator, UploadFileInitiator>();
        services.AddScoped<IFileUploadProcessor, FileUploadProcessor>();
        services.AddScoped<IUploadFileStatusService, UploadFileStatusService>();
        services.AddScoped<ICsvFileReaderService, CsvFileReaderService>();
        services.AddScoped<IUploadFileService, UploadFileService>();
        services.AddScoped<IImageAnalysisService, ImageAnalysisService>();
        services.AddScoped<ISpeech2TextService, Speech2TextService>();
        services.AddScoped<IText2SpeechService, Text2SpeechService>();
        services.AddScoped<IInvestigationReportPdfService, InvestigationReportPdfService>();
        services.AddScoped<IAzureAdService, AzureAdService>();
        services.AddScoped<IAnswerService, AnswerService>();
        services.AddScoped<IMediaIdfyService, MediaIdfyService>();
        services.AddScoped<IDocumentIdfyService, DocumentIdfyService>();
        services.AddScoped<IAgentFaceIdfyService, AgentFaceIdfyService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IVendorUserService, VendorUserService>();
        services.AddScoped<ICompanyUserService, CompanyUserService>();
        services.AddScoped<IVendorServiceTypeManager, VendorServiceTypeManager>();
        services.AddScoped<IAgencyUserCreateEditService, AgencyUserCreateEditService>();
        services.AddScoped<IAgencyCreateEditService, AgencyCreateEditService>();
        services.AddScoped<IValidateImageService, ValidateImageService>();
        services.AddScoped<IBeneficiaryCreateEditService, BeneficiaryCreateEditService>();
        services.AddScoped<ICustomerCreateEditService, CustomerCreateEditService>();
        services.AddScoped<ICaseCreateEditService, CaseCreateEditService>();
        services.AddScoped<IWeatherInfoService, WeatherInfoService>();
        services.AddScoped<IManagerService, ManagerService>();
        services.AddScoped<IAddInvestigationService, AddInvestigationService>();
        services.AddScoped<IAssessorService, AssessorService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<ISanitizerService, SanitizerService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddFeatureManagement().AddFeatureFilter<TimeWindowFilter>();
        services.AddScoped<IPhoneService, PhoneService>();
        services.AddScoped<IMediaDataService, MediaDataService>();
        services.AddScoped<ITinyUrlService, TinyUrlService>();
        services.AddScoped<IPdfGenerateQuestionLocationService, PdfGenerateQuestionLocationService>();
        services.AddScoped<IPdfGenerateDocumentLocationService, PdfGenerateDocumentLocationService>();
        services.AddScoped<IPdfGenerateFaceLocationService, PdfGenerateFaceLocationService>();
        services.AddScoped<IPdfGenerateAgentLocationService, PdfGenerateAgentLocationService>();
        services.AddScoped<IPdfGenerateDetailReportService, PdfGenerateDetailReportService>();
        services.AddScoped<IPdfGenerateCaseDetailService, PdfGenerateCaseDetailService>();
        services.AddScoped<IPdfGenerateDetailService, PdfGenerateDetailService>();
        services.AddScoped<IPdfGenerativeService, PdfGenerativeService>();
        services.AddScoped<IViewRenderService, ViewRenderService>();
        services.AddScoped<IPanCardService, PanCardService>();
        services.AddScoped<ICloneReportService, CloneReportService>();
        services.AddScoped<IAgentIdfyService, AgentIdfyService>();
        services.AddScoped<ICaseVendorService, CaseVendorService>();
        services.AddScoped<IVendorInvestigationService, VendorInvestigationService>();
        services.AddScoped<IDashboardCountService, DashboardCountService>();
        services.AddScoped<ITimelineService, TimelineService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IProcessCaseService, ProcessCaseService>();
        services.AddScoped<IInvestigationService, InvestigationService>();
        services.AddScoped<IHangfireJobService, HangfireJobService>();
        services.AddScoped<ICaseCreationService, CaseCreationService>();
        services.AddScoped<ICaseDetailCreationService, CaseDetailCreationService>();
        services.AddScoped<ICustomerCreationService, CustomerCreationService>();
        services.AddScoped<IBeneficiaryCreationService, BeneficiaryCreationService>();
        services.AddScoped<ICaseImageCreationService, CaseImageCreationService>();
        services.AddScoped<IUploadService, UploadService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomApiClient, CustomApiClient>();
        services.AddScoped<IAgencyService, AgencyService>();
        services.AddScoped<ICaseAgentService, CaseAgentService>();
        services.AddScoped<IAmazonApiService, AmazonApiService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<INumberSequenceService, NumberSequenceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<ICaseInvestigationService, CaseInvestigationService>();
        services.AddScoped<IEmpanelledAgencyService, EmpanelledAgencyService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IFaceMatchService, FaceMatchService>();
        services.AddScoped<IGoogleService, GoogleService>();
        services.AddScoped<IGoogleMaskHelper, GoogleMaskHelper>();
        services.AddScoped<ITextAnalyticsService, TextAnalyticsService>();

        services.AddScoped<IHttpClientService, HttpClientService>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        //services.AddTransient<CustomCookieAuthenticationEvents>();

        var connectionString = "Data Source=" + Environment.GetEnvironmentVariable("COUNTRY") + "_" + configuration.GetConnectionString("Database");
        services.AddDbContext<ApplicationDbContext>(options =>
                                options.UseSqlite(connectionString,
                sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        services.AddHangfire(config => config.UseMemoryStorage());
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5;
            options.Queues = new[] { "default", "emails", "critical" };
        });

        return services;
    }
}
