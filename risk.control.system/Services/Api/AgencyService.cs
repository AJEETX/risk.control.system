using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IAgencyService
    {
        Task<object[]> AllAgencies();

        Task<object[]> GetAllEmpanelledAgenciesAsync(string userEmail);

        Task<object[]> GetEmpanelledAgency(string userEmail, long caseId);

        Task<object[]> GetAvailableAgencies(string userEmail);

        Task<List<AgencyServiceResponse>> GetAgencyService(long id);

        Task<List<AgencyServiceResponse>> AllServices(string userEmail);

        Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long id);
    }

    internal class AgencyService : IAgencyService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IAgencyInvestigationServiceService agencyInvestigationServiceService;
        private readonly ILogger<AgencyService> logger;
        private readonly IEmpanelledAvailableAgencyService empanelledAvailableAgencyService;
        private readonly IAgencyAgentService agencyAgentService;
        private readonly IBase64FileService base64FileService;

        public AgencyService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IAgencyInvestigationServiceService agencyInvestigationServiceService,
            ILogger<AgencyService> logger,
            IEmpanelledAvailableAgencyService empanelledAvailableAgencyService,
            IAgencyAgentService agencyAgentService,
            IBase64FileService base64FileService)
        {
            _contextFactory = contextFactory;
            this.agencyInvestigationServiceService = agencyInvestigationServiceService;
            this.logger = logger;
            this.empanelledAvailableAgencyService = empanelledAvailableAgencyService;
            this.agencyAgentService = agencyAgentService;
            this.base64FileService = base64FileService;
        }

        public async Task<object[]> AllAgencies()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var agencyData = await _context.Vendor.AsNoTracking()
             .Include(v => v.Country)
             .Include(v => v.District)
             .Include(v => v.State)
             .Include(v => v.PinCode)
             .Where(v => !v.Deleted)
             .OrderBy(a => a.Name)
             .Select(u => new
             {
                 u.VendorId,
                 u.DocumentUrl,
                 u.Email,
                 u.Name,
                 u.Code,
                 ISDCode = u.Country.ISDCode,
                 u.PhoneNumber,
                 u.Addressline,
                 DistrictName = u.District.Name,
                 StateCode = u.State.Code,
                 CountryCode = u.Country.Code,
                 PinCodeValue = u.PinCode.Code,
                 u.Status,
                 u.Updated,
                 u.Created,
                 u.UpdatedBy,
                 u.IsUpdated
             })
             .ToListAsync();

            var result = agencyData.Select(async u =>
            {
                var documentImage = await base64FileService.GetBase64FileAsync(u.DocumentUrl, Applicationsettings.NO_IMAGE);

                return new
                {
                    Id = u.VendorId,
                    Document = documentImage,
                    Domain = $"<a href='/Vendors/Details?id={u.VendorId}'>{u.Email}</a>",
                    u.Name,
                    u.Code,
                    Phone = $"(+{u.ISDCode}) {u.PhoneNumber}",
                    Address = $"{u.Addressline}, {u.DistrictName}, {u.StateCode}",
                    Country = u.CountryCode,
                    Flag = $"/flags/{u.CountryCode.ToLower()}.png",
                    Pincode = u.PinCodeValue,
                    Status = $"<span class='badge badge-light'>{u.Status.GetEnumDisplayName()}</span>",
                    Updated = (u.Updated ?? u.Created).ToString("dd-MM-yyyy"),
                    u.UpdatedBy,
                    VendorName = u.Email,
                    RawStatus = u.Status.GetEnumDisplayName(),
                    u.IsUpdated,
                    LastModified = u.Updated
                };
            });
            var awaitedResults = await Task.WhenAll(result);

            // 3. Batch Update (EF Core 7+) - This is much faster than loading all into memory
            await _context.Vendor.AsNoTracking()
                .Where(v => !v.Deleted)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsUpdated, false));

            return awaitedResults;
        }

        public async Task<object[]> GetAllEmpanelledAgenciesAsync(string userEmail)
        {
            try
            {
                var emapanelledAgencies = await empanelledAvailableAgencyService.GetAllEmpanelledAgenciesAsync(userEmail);
                return emapanelledAgencies;
            }
            catch (Exception)
            {
                logger.LogError("Error in GetAllEmpanelledAgenciesAsync for user {UserEmail}", userEmail);
                throw;
            }
        }

        public async Task<object[]> GetEmpanelledAgency(string userEmail, long caseId)
        {
            try
            {
                var empanelledAgenciesForCase = await empanelledAvailableAgencyService.GetEmpanelledAgency(userEmail, caseId);
                return empanelledAgenciesForCase;
            }
            catch (Exception)
            {
                logger.LogError("Error in GetEmpanelledAgency for user {UserEmail} and caseId {CaseId}", userEmail, caseId);
                throw;
            }
        }

        public async Task<object[]> GetAvailableAgencies(string userEmail)
        {
            try
            {
                var availableAgencies = await empanelledAvailableAgencyService.GetAvailableAgencies(userEmail);
                return availableAgencies;
            }
            catch (Exception)
            {
                logger.LogError("Error in GetAvailableAgencies for user {UserEmail}", userEmail);
                throw;
            }
        }

        public async Task<List<AgencyServiceResponse>> GetAgencyService(long vendorId)
        {
            try
            {
                var agencyServices = await agencyInvestigationServiceService.GetAgencyService(vendorId);
                return agencyServices;
            }
            catch (Exception)
            {
                logger.LogError("Error in GetAgencyService for vendorId {VendorId}", vendorId);
                throw;
            }
        }

        public async Task<List<AgencyServiceResponse>> AllServices(string userEmail)
        {
            try
            {
                var agencyAllServices = await agencyInvestigationServiceService.AllServices(userEmail);
                return agencyAllServices;
            }
            catch (Exception)
            {
                logger.LogError("Error in AllServices for user {UserEmail}", userEmail);
                throw;
            }
        }

        public async Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long caseId)
        {
            try
            {
                var agentsWithCases = await agencyAgentService.GetAgentWithCases(userEmail, caseId);
                return agentsWithCases;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GetAgentWithCases...{UserEmail}", userEmail);
                throw;
            }
        }
    }
}