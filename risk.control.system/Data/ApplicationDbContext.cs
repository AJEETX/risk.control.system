using Microsoft.EntityFrameworkCore;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Data
{
    public class ApplicationDbContext : AuditableIdentityContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor context) : base(options, context)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public virtual DbSet<ApplicationUser> ApplicationUser { get; set; }
        public virtual DbSet<ClientCompanyApplicationUser> ClientCompanyApplicationUser { get; set; }
        public virtual DbSet<ApplicationRole> ApplicationRole { get; set; }
        public virtual DbSet<InvestigationCase> InvestigationCase { get; set; }
        //public virtual DbSet<ClaimsInvestigation> ClaimsInvestigation { get; set; }
        public virtual DbSet<LineOfBusiness> LineOfBusiness { get; set; }
        public virtual DbSet<InvestigationCaseStatus> InvestigationCaseStatus { get; set; }
        public virtual DbSet<InvestigationCaseSubStatus> InvestigationCaseSubStatus { get; set; }
        public virtual DbSet<InvestigationCaseOutcome> InvestigationCaseOutcome { get; set; }
        public virtual DbSet<CostCentre> CostCentre { get; set; }
        public virtual DbSet<CaseEnabler> CaseEnabler { get; set; }
        public virtual DbSet<BeneficiaryRelation> BeneficiaryRelation { get; set; }
        public virtual DbSet<Country> Country { get; set; }
        public virtual DbSet<State> State { get; set; }
        public virtual DbSet<District> District { get; set; }
        public virtual DbSet<PinCode> PinCode { get; set; }
        public virtual DbSet<ClientCompany> ClientCompany { get; set; }
        public virtual DbSet<InvestigationServiceType> InvestigationServiceType { get; set; }
        public virtual DbSet<ServicedPinCode> ServicedPinCode { get; set; }
        public virtual DbSet<Vendor> Vendor { get; set; } = default!;
        public virtual DbSet<VendorInvestigationServiceType> VendorInvestigationServiceType { get; set; } = default!;
        public virtual DbSet<VendorApplicationUser> VendorApplicationUser { get; set; } = default!;



        public virtual DbSet<FileOnDatabaseModel> FilesOnDatabase { get; set; }
        public virtual DbSet<FileOnFileSystemModel> FilesOnFileSystem { get; set; }
    }
}
