using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using risk.control.system.Models;

namespace risk.control.system.Data
{
    public abstract class AuditableIdentityContext : IdentityDbContext<ApplicationUser, ApplicationRole, long>
    {
        public IHttpContextAccessor httpContext;
        private readonly IServiceProvider services;

        public AuditableIdentityContext(DbContextOptions options, IHttpContextAccessor context, IServiceProvider services) : base(options)
        {
            this.httpContext = context;
            this.services = services;
        }
        protected ApplicationDbContext _context => services.GetRequiredService<ApplicationDbContext>();
        public DbSet<Audit> AuditLogs { get; set; }

        public virtual async Task<int> SaveChangesAsync(string userId = null, bool notseed = true)
        {
            if (notseed)
            {
                await OnBeforeSaveChanges(userId);
            }
            var result = await base.SaveChangesAsync();
            return result;
        }

        private async Task OnBeforeSaveChanges(string userId)
        {
            var userEmail = userId ?? httpContext?.HttpContext?.User?.Identity.Name;
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;
                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name;
                auditEntry.UserId = userEmail;
                auditEntry.CompanyId = companyUser?.ClientCompanyId;
                auditEntries.Add(auditEntry);
                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.AuditType = AuditType.Create;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.AuditType = AuditType.Delete;
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.AuditType = AuditType.Update;
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            foreach (var auditEntry in auditEntries)
            {
                AuditLogs.Add(auditEntry.ToAudit());
            }
        }
    }
}
