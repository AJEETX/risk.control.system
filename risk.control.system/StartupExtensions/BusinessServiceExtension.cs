using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;

using AspNetCoreHero.ToastNotification;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using risk.control.system.Controllers.Api.PortalAdmin;
using risk.control.system.Permission;
using risk.control.system.Services;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Api;
using risk.control.system.Services.Assessor;
using risk.control.system.Services.Common;
using risk.control.system.Services.Company;
using risk.control.system.Services.Creator;
using risk.control.system.Services.Report;
using risk.control.system.Services.Tool;
using SmartBreadcrumbs.Extensions;

namespace risk.control.system.StartupExtensions;

public static class BusinessServiceExtension
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
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

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        services.AddHttpClient();
        services.AddScoped<IAgencyAgentService, AgencyAgentService>();
        services.AddScoped<IAgencyInvestigationServiceService, AgencyInvestigationServiceService>();
        services.AddScoped<IEmpanelledAvailableAgencyService, EmpanelledAvailableAgencyService>();
        services.AddScoped<IFileUploadCaseAllocationService, FileUploadCaseAllocationService>();
        services.AddScoped<IManagerDashboardService, ManagerDashboardService>();
        services.AddScoped<IAssessorDashboardService, AssessorDashboardService>();
        services.AddScoped<ICreatorDashboardService, CreatorDashboardService>();
        services.AddScoped<IAdminDashBoardService, AdminDashBoardService>();
        services.AddScoped<ICompanyDashboardService, CompanyDashboardService>();
        services.AddScoped<IAssessorQueryService, AssessorQueryService>();
        services.AddScoped<IAgencyUserApiService, AgencyUserApiService>();
        services.AddScoped<ICompanyUserApiService, CompanyUserApiService>();
        services.AddScoped<IAgencyQueryReplyService, AgencyQueryReplyService>();
        services.AddScoped<IProcessSubmittedReportService, ProcessSubmittedReportService>();
        services.AddScoped<IDeclineCaseService, DeclineCaseService>();
        services.AddScoped<IWithdrawCaseService, WithdrawCaseService>();
        services.AddScoped<ICaseAllocationService, CaseAllocationService>();
        services.AddScoped<IProcessImageService, ProcessImageService>();
        services.AddScoped<IAgencyAgentAllocationService, AgencyAgentAllocationService>();
        services.AddScoped<IAgencyDetailService, AgencyDetailService>();
        services.AddScoped<IDateParserService, DateParserService>();
        services.AddScoped<IBase64FileService, Base64FileService>();
        services.AddScoped<IAgencyInvestigationDetailService, AgencyInvestigationDetailService>();
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
        services.AddScoped<IZipFileService, ZipFileService>();
        services.AddScoped<IImageAnalysisService, ImageAnalysisService>();
        services.AddScoped<ISpeech2TextService, Speech2TextService>();
        services.AddScoped<IText2SpeechService, Text2SpeechService>();
        services.AddScoped<IInvestigationReportPdfService, InvestigationReportPdfService>();
        services.AddScoped<IAzureAdService, AzureAdService>();
        services.AddScoped<IAgentAnswerService, AgentAnswerService>();
        services.AddScoped<IMediaIdfyService, MediaIdfyService>();
        services.AddScoped<IDocumentIdfyService, DocumentIdfyService>();
        services.AddScoped<IAgentFaceIdfyService, AgentFaceIdfyService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IAgencyUserService, AgencyUserService>();
        services.AddScoped<ICompanyUserService, CompanyUserService>();
        services.AddScoped<IAgencyServiceTypeManager, AgencyServiceTypeManager>();
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
        services.AddScoped<Services.Api.IAgencyService, Services.Api.AgencyService>();
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
        services.AddScoped<IFaceIdfyService, FaceIdfyService>();
        services.AddScoped<ICaseReportService, CaseReportService>();
        services.AddScoped<IAgencyInvestigationService, AgencyInvestigationService>();
        services.AddScoped<IAgencyDashboardService, AgencyDashboardService>();
        services.AddScoped<ITimelineService, TimelineService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IProcessCaseService, ProcessCaseService>();
        services.AddScoped<IInvestigationService, InvestigationService>();
        services.AddScoped<IHangfireJobService, HangfireJobService>();
        services.AddScoped<ICaseDetailCreationService, CaseDetailCreationService>();
        services.AddScoped<ICustomerCreationService, CustomerCreationService>();
        services.AddScoped<IBeneficiaryCreationService, BeneficiaryCreationService>();
        services.AddScoped<ICaseImageCreationService, CaseImageCreationService>();
        services.AddScoped<IUploadService, UploadService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomApiClient, CustomApiClient>();
        services.AddScoped<Services.Agency.IAgencyService, Services.Agency.AgencyService>();
        services.AddScoped<IAgentSubmitCaseService, AgentSubmitCaseService>();
        services.AddScoped<IAmazonApiService, AmazonApiService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<INumberSequenceService, NumberSequenceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<ICaseNotesService, CaseNotesService>();
        services.AddScoped<IEmpanelledAgencyService, EmpanelledAgencyService>();
        services.AddScoped<IAgentCaseDetailService, AgentCaseDetailService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUploadZipFileService, UploadZipFileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IFaceMatchService, FaceMatchService>();
        services.AddScoped<IGoogleService, GoogleService>();
        services.AddScoped<IGoogleMaskHelper, GoogleMaskHelper>();
        services.AddScoped<ITextAnalyticsService, TextAnalyticsService>();

        services.AddScoped<IHttpClientService, HttpClientService>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}