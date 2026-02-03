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

    internal class AgencyDetailService : IAgencyDetailService
    {
        private readonly ApplicationDbContext context;
        private readonly IInvestigationDetailService investigationDetailService;

        public AgencyDetailService(
            ApplicationDbContext context,
            IInvestigationDetailService investigationDetailService)
        {
            this.context = context;
            this.investigationDetailService = investigationDetailService;
        }

        public async Task<Vendor> GetVendorDetailAsync(long vendorId, long selectedCaseId)
        {
            var vendor = await context.Vendor
                .Include(v => v.ratings)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);

            if (vendor == null)
                return null;

            var approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
            var rejected = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

            var totalCases = await context.Investigations.CountAsync(c =>
                c.VendorId == vendor.VendorId &&
                !c.Deleted &&
                (c.SubStatus == approved || c.SubStatus == rejected));

            var agentCount = await context.ApplicationUser.CountAsync(u =>
                u.VendorId == vendor.VendorId &&
                !u.Deleted &&
                u.Role == AppRoles.AGENT);

            var currentCases = investigationDetailService
                .GetAgencyIdsLoad(new List<long> { vendor.VendorId })
                .FirstOrDefault();

            // ⚠️ Legacy hack preserved
            vendor.SelectedCountryId = agentCount;
            vendor.SelectedStateId = currentCases?.CaseCount ?? 0;
            vendor.SelectedDistrictId = totalCases;
            vendor.SelectedPincodeId = selectedCaseId;

            return vendor;
        }
    }
}