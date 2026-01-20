using Microsoft.EntityFrameworkCore;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Models
{
    public class ApplicationDbContext : AuditableIdentityContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor context, IServiceProvider services) : base(options, context, services)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
        public virtual DbSet<BsbInfo> BsbInfo { get; set; }
        public virtual DbSet<EducationType> EducationType { get; set; }
        public virtual DbSet<OccupationType> OccupationType { get; set; }
        public virtual DbSet<AnnualIncome> AnnualIncome { get; set; }
        public virtual DbSet<PdfDownloadTracker> PdfDownloadTracker { get; set; }
        public virtual DbSet<InvestigationTask> Investigations { get; set; }
        public DbSet<ReportTemplate> ReportTemplates { get; set; }
        public DbSet<LocationReport> LocationReport { get; set; }
        public virtual DbSet<Question> Questions { get; set; }
        public virtual DbSet<StatusNotification> Notifications { get; set; }
        public virtual DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
        public virtual DbSet<UserSessionAlive> UserSessionAlive { get; set; }
        public virtual DbSet<GlobalSettings> GlobalSettings { get; set; }
        public virtual DbSet<NumberSequence> NumberSequence { get; set; }
        public virtual DbSet<AgencyRating> Ratings { get; set; }
        public virtual DbSet<VendorInvoice> VendorInvoice { get; set; }
        public virtual DbSet<PermissionModule> PermissionModule { get; set; }
        public virtual DbSet<ApplicationUser> ApplicationUser { get; set; }

        public virtual DbSet<CostCentre> CostCentre { get; set; }
        public virtual DbSet<CaseEnabler> CaseEnabler { get; set; }
        public virtual DbSet<BeneficiaryRelation> BeneficiaryRelation { get; set; }
        public virtual DbSet<Country> Country { get; set; }
        public virtual DbSet<State> State { get; set; }
        public virtual DbSet<District> District { get; set; }
        public virtual DbSet<PinCode> PinCode { get; set; }
        public virtual DbSet<ClientCompany> ClientCompany { get; set; }
        public virtual DbSet<InvestigationServiceType> InvestigationServiceType { get; set; }
        public virtual DbSet<Vendor> Vendor { get; set; } = default!;
        public virtual DbSet<VendorInvestigationServiceType> VendorInvestigationServiceType { get; set; } = default!;
        public virtual DbSet<EnquiryRequest> QueryRequest { get; set; } = default!;
        public virtual DbSet<FileOnFileSystemModel> FilesOnFileSystem { get; set; }
        public DbSet<PolicyDetail> PolicyDetail { get; set; } = default!;
        public DbSet<CustomerDetail> CustomerDetail { get; set; } = default!;
        public DbSet<BeneficiaryDetail> BeneficiaryDetail { get; set; } = default!;
        public DbSet<AgentIdReport> AgentIdReport { get; set; } = default!;
        public DbSet<FaceIdReport> DigitalIdReport { get; set; } = default!;
        public DbSet<DocumentIdReport> DocumentIdReport { get; set; } = default!;
        public DbSet<MediaReport> MediaReport { get; set; } = default!;
        public DbSet<InvestigationReport> InvestigationReport { get; set; } = default!;
    }
}