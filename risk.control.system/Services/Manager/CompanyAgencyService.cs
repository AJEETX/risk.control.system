using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Manager
{
    public interface ICompanyAgencyService
    {
        Task<(bool Success, string Message)> EmpanelAgenciesAsync(string userEmail, List<long> vendorIds);

        Task<(bool Success, string Message)> DepanelAgenciesAsync(string userEmail, List<long> vendorIds);
    }

    internal class CompanyAgencyService(ApplicationDbContext context, ILogger<CompanyAgencyService> logger) : ICompanyAgencyService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<CompanyAgencyService> _logger = logger;

        public async Task<(bool Success, string Message)> EmpanelAgenciesAsync(string userEmail, List<long> vendorIds)
        {
            try
            {
                if (vendorIds?.Any() != true)
                    return (false, "No agency selected !!!");

                var companyUser = await _context.ApplicationUser.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                if (companyUser == null)
                    return (false, "User Not Found");

                var company = await _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                    return (false, "Company Not Found");

                var vendorsToEmpanel = await _context.Vendor
                    .Where(v => vendorIds.Contains(v.VendorId))
                    .ToListAsync();

                var newVendors = vendorsToEmpanel
                    .Where(v => !company.EmpanelledVendors.Any(existing => existing.VendorId == v.VendorId))
                    .ToList();

                if (newVendors.Any())
                {
                    foreach (var vendor in newVendors)
                    {
                        vendor.IsUpdated = true;
                        vendor.Updated = DateTime.UtcNow;
                    }
                    company.EmpanelledVendors.AddRange(newVendors);
                    company.Updated = DateTime.UtcNow;
                    company.UpdatedBy = userEmail;

                    _context.ClientCompany.Update(company);
                    await _context.SaveChangesAsync();
                }

                return (true, $"<b>{newVendors.Count}</b> Agency(s) empanelled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error empanelling agencies for user {UserEmail}", userEmail);
                return (false, "An internal error occurred while empanelling agencies.");
            }
        }

        public async Task<(bool Success, string Message)> DepanelAgenciesAsync(string userEmail, List<long> vendorIds)
        {
            try
            {
                if (vendorIds?.Any() != true)
                    return (false, "No agency selected !!!");
                var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser == null)
                    return (false, "User Not Found.");
                var company = await _context.ClientCompany.Include(c => c.EmpanelledVendors).FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                    return (false, "Company Not Found.");
                var agenciesToDepanel = company.EmpanelledVendors.Where(v => vendorIds.Contains(v.VendorId)).ToList();
                if (agenciesToDepanel.Count == 0)
                    return (false, "None of the selected agencies are currently empanelled.");
                foreach (var agency in agenciesToDepanel)
                {
                    company.EmpanelledVendors.Remove(agency);
                    agency.IsUpdated = true;
                    agency.Updated = DateTime.UtcNow;
                }
                company.Updated = DateTime.UtcNow;
                company.UpdatedBy = userEmail;
                _context.ClientCompany.Update(company);
                await _context.SaveChangesAsync();

                return (true, $"<b>{agenciesToDepanel.Count}</b> Agency(s) De-panelled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred depanelling Agencies for {UserEmail}", userEmail);
                return (false, "Error occurred while removing agencies. Try again.");
            }
        }
    }
}