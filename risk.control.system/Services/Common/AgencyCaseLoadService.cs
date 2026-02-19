using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface IAgencyCaseLoadService
    {
        Task<List<VendorIdWithCases>> GetAgencyIdsLoad(List<long> existingVendors);
    }

    internal class AgencyCaseLoadService : IAgencyCaseLoadService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public AgencyCaseLoadService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<VendorIdWithCases>> GetAgencyIdsLoad(List<long> existingVendors)
        {
            // Get relevant status IDs in one query
            var relevantStatuses = new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
                }; // Improves lookup performance
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Fetch cases that match the criteria
            var vendorCaseCount = context.Investigations.AsNoTracking()
                .Where(c => !c.Deleted &&
                            c.VendorId.HasValue &&
                            c.AssignedToAgency &&
                            relevantStatuses.Contains(c.SubStatus))
                .GroupBy(c => c.VendorId.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // Create the list of VendorIdWithCases
            return existingVendors
                .Select(vendorId => new VendorIdWithCases
                {
                    VendorId = vendorId,
                    CaseCount = vendorCaseCount.GetValueOrDefault(vendorId, 0)
                })
                .ToList();
        }
    }
}