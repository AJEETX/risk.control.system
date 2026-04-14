using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services
{
    public interface IAgencyDetailService
    {
        Task<Vendor> GetVendorDetailAsync(long vendorId, long selectedCaseId);
    }

    internal class AgencyDetailService(
        ApplicationDbContext context,
        IAgencyCaseLoadService agencyCaseLoadService) : IAgencyDetailService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IAgencyCaseLoadService _agencyCaseLoadService = agencyCaseLoadService;

        public async Task<Vendor> GetVendorDetailAsync(long vendorId, long selectedCaseId)
        {
            var vendor = await _context.Vendor.Include(v => v.Ratings).Include(v => v.Country).Include(v => v.PinCode).Include(v => v.State).Include(v => v.District).Include(v => v.VendorInvestigationServiceTypes).FirstOrDefaultAsync(v => v.VendorId == vendorId);
            if (vendor == null) return null!;
            var totalCases = await _context.Investigations.CountAsync(c =>
                c.VendorId == vendor.VendorId && !c.Deleted &&
                (c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR || c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR));
            var agentCount = await _context.ApplicationUser.CountAsync(u => u.VendorId == vendor.VendorId && !u.Deleted && u.Role == AppRoles.AGENT);
            var currentCases = (await _agencyCaseLoadService.GetAgencyIdsLoad(new List<long> { vendor.VendorId })).FirstOrDefault();
            vendor.UserCount = agentCount;
            vendor.CurrentCasesCount = currentCases?.CaseCount ?? 0;
            vendor.CompletedCasesCount = totalCases;
            vendor.SelectedPincodeId = selectedCaseId;
            return vendor;
        }
    }
}