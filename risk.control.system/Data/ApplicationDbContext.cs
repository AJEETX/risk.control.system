using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public virtual DbSet<ApplicationUser> ApplicationUser { get; set; }
        //public virtual DbSet<ClientCompanyUser> ClientApplicationUser { get; set; }
        public virtual DbSet<ApplicationRole> ApplicationRole { get; set; }
        public virtual DbSet<InvestigationCase> InvestigationCase { get; set; }
        //public virtual DbSet<ClaimsInvestigation> ClaimsInvestigation { get; set; }
        public virtual DbSet<LineOfBusiness> LineOfBusiness { get; set; }
        public virtual DbSet<InvestigationCaseStatus> InvestigationCaseStatus { get; set; }
        public virtual DbSet<Country> Country { get; set; }
        public virtual DbSet<State> State { get; set; }
        public virtual DbSet<District> District { get; set; }
        public virtual DbSet<PinCode> PinCode { get; set; }
        public virtual DbSet<ClientCompany> ClientCompany { get; set; }
        public virtual DbSet<InvestigationServiceType> InvestigationServiceType { get; set; }
        public virtual DbSet<ServicedPinCode> ServicedPinCode { get; set; }
        public virtual DbSet<Vendor> Vendor { get; set; } = default!;
        public DbSet<VendorInvestigationServiceType> VendorInvestigationServiceType { get; set; } = default!;
        //public virtual DbSet<VendorUser> VendorUser { get; set; } = default!;
    }
}
