using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Manager
{
    public interface ICompanyAgencyService
    {
        Task<(bool Success, string Message)> EmpanelAgenciesAsync(string userEmail, List<long> vendorIds);

        Task<(bool Success, string Message)> DepanelAgenciesAsync(string userEmail, List<long> vendorIds);
    }

    internal class CompanyAgencyService : ICompanyAgencyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyAgencyService> _logger;

        public CompanyAgencyService(ApplicationDbContext context, ILogger<CompanyAgencyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> EmpanelAgenciesAsync(string userEmail, List<long> vendorIds)
        {
            try
            {
                if (vendorIds == null || !vendorIds.Any())
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

                // Prevent adding duplicates if they already exist in the list
                var newVendors = vendorsToEmpanel
                    .Where(v => !company.EmpanelledVendors.Any(existing => existing.VendorId == v.VendorId))
                    .ToList();

                if (newVendors.Any())
                {
                    company.EmpanelledVendors.AddRange(newVendors);
                    company.Updated = DateTime.UtcNow;
                    company.UpdatedBy = userEmail;

                    _context.ClientCompany.Update(company);
                    await _context.SaveChangesAsync();
                }

                return (true, "Agency(s) empanelled successfully");
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
                if (vendorIds == null || !vendorIds.Any())
                    return (false, "No agency selected !!!");

                var companyUser = await _context.ApplicationUser.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                if (companyUser == null)
                    return (false, "User Not Found.");

                var company = await _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                    return (false, "Company Not Found.");

                // Identify the agencies currently empanelled that match the provided IDs
                var agenciesToDepanel = company.EmpanelledVendors
                    .Where(v => vendorIds.Contains(v.VendorId))
                    .ToList();

                if (!agenciesToDepanel.Any())
                    return (false, "None of the selected agencies are currently empanelled.");

                foreach (var agency in agenciesToDepanel)
                {
                    company.EmpanelledVendors.Remove(agency);
                }

                company.Updated = DateTime.UtcNow;
                company.UpdatedBy = userEmail;

                _context.ClientCompany.Update(company);
                await _context.SaveChangesAsync();

                return (true, "Agency(s) De-panelled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred depanelling Agencies for {UserEmail}", userEmail);
                return (false, "Error occurred while removing agencies. Try again.");
            }
        }
    }
}