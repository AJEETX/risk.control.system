using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IAgencyApiService
    {
        Task<object[]> AllAgencies();

        Task<object[]> GetAllEmpanelledAgenciesAsync(string userEmail);

        Task<object[]> GetEmpanelledAgency(string userEmail, long caseId);

        Task<object[]> GetAvailableAgencies(string userEmail);

        Task<List<AgencyServiceResponse>> GetAgencyService(long id);

        Task<List<AgencyServiceResponse>> AllServices(string userEmail);

        Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long id);
    }

    internal class AgencyApiService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IAgencyInvestigationServiceService agencyInvestigationServiceService,
        ICompanyAgencyApiService empanelledAvailableAgencyService,
        IAgencyAgentService agencyAgentService,
        IBase64FileService base64FileService) : IAgencyApiService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
        private readonly IAgencyInvestigationServiceService _agencyInvestigationServiceService = agencyInvestigationServiceService;
        private readonly ICompanyAgencyApiService _empanelledAvailableAgencyService = empanelledAvailableAgencyService;
        private readonly IAgencyAgentService _agencyAgentService = agencyAgentService;
        private readonly IBase64FileService _base64FileService = base64FileService;

        public async Task<object[]> AllAgencies()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var agencyData = await _context.Vendor.AsNoTracking().Include(v => v.Country).Include(v => v.District).Include(v => v.State).Include(v => v.PinCode).Where(v => !v.Deleted).OrderBy(a => a.Name)
             .Select(u => new
             {
                 u.VendorId,
                 u.DocumentUrl,
                 u.Email,
                 u.Name,
                 ISDCode = u.Country!.ISDCode,
                 u.Addressline,
                 DistrictName = u.District!.Name,
                 StateCode = u.State!.Code,
                 CountryCode = u.Country.Code,
                 PinCodeValue = $"{u.PinCode!.Name} - {u.PinCode.Code}",
                 u.Status,
                 u.Updated,
                 u.Created,
                 u.UpdatedBy,
                 u.IsUpdated
             }).ToListAsync();

            var result = agencyData.Select(async u =>
            {
                var documentImage = await _base64FileService.GetBase64FileAsync(u.DocumentUrl!, Applicationsettings.NO_IMAGE);
                return new
                {
                    Id = u.VendorId,
                    Document = documentImage,
                    Domain = $"<a>{u.Email}</a>",
                    u.Name,
                    Address = $"{u.Addressline}, {u.DistrictName}, {u.StateCode}",
                    Country = u.CountryCode,
                    Flag = $"/flags/{u.CountryCode.ToLower()}.png",
                    Pincode = u.PinCodeValue,
                    Status = $"<span class='badge badge-light'>{u.Status!.GetEnumDisplayName()}</span>",
                    Updated = (u.Updated ?? u.Created),
                    u.UpdatedBy,
                    VendorName = u.Email,
                    RawStatus = u.Status!.GetEnumDisplayName(),
                    u.IsUpdated,
                    LastModified = u.Updated
                };
            });
            var awaitedResults = await Task.WhenAll(result);
            await _context.Vendor.AsNoTracking().Where(v => !v.Deleted).ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsUpdated, false));
            return awaitedResults;
        }

        public async Task<object[]> GetAllEmpanelledAgenciesAsync(string userEmail)
        {
            var emapanelledAgencies = await _empanelledAvailableAgencyService.GetAllEmpanelledAgenciesAsync(userEmail);
            return emapanelledAgencies;
        }

        public async Task<object[]> GetEmpanelledAgency(string userEmail, long caseId)
        {
            var empanelledAgenciesForCase = await _empanelledAvailableAgencyService.GetEmpanelledAgency(userEmail, caseId);
            return empanelledAgenciesForCase;
        }

        public async Task<object[]> GetAvailableAgencies(string userEmail)
        {
            var availableAgencies = await _empanelledAvailableAgencyService.GetAvailableAgencies(userEmail);
            return availableAgencies;
        }

        public async Task<List<AgencyServiceResponse>> GetAgencyService(long vendorId)
        {
            var agencyServices = await _agencyInvestigationServiceService.GetAgencyService(vendorId);
            return agencyServices;
        }

        public async Task<List<AgencyServiceResponse>> AllServices(string userEmail)
        {
            var agencyAllServices = await _agencyInvestigationServiceService.AllServices(userEmail);
            return agencyAllServices;
        }

        public async Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long caseId)
        {
            var agentsWithCases = await _agencyAgentService.GetAgentWithCases(userEmail, caseId);
            return agentsWithCases;
        }
    }
}