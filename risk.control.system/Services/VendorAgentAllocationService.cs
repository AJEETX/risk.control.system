using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IVendorAgentAllocationService
    {
        Task<AllocateVendorAgentResult> AllocateAsync(
            string selectedCase,
            long claimId,
            string? allocatedByEmail);
    }

    internal class VendorAgentAllocationService : IVendorAgentAllocationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVendorInvestigationDetailService _vendorInvestigationDetailService;
        private readonly ILogger<VendorAgentAllocationService> _logger;

        public VendorAgentAllocationService(
            ApplicationDbContext context,
            IVendorInvestigationDetailService vendorInvestigationDetailService,
            ILogger<VendorAgentAllocationService> logger)
        {
            _context = context;
            _vendorInvestigationDetailService = vendorInvestigationDetailService;
            _logger = logger;
        }

        public async Task<AllocateVendorAgentResult> AllocateAsync(
            string selectedCase,
            long claimId,
            string? allocatedByEmail)
        {
            try
            {
                var vendorAgent = await _context.ApplicationUser
                    .Include(a => a.Vendor)
                    .FirstOrDefaultAsync(u => u.Id.ToString() == selectedCase);

                if (vendorAgent == null)
                {
                    return new AllocateVendorAgentResult
                    {
                        Success = false,
                        ErrorMessage = "User not found"
                    };
                }

                if (!vendorAgent.VendorId.HasValue)
                {
                    return new AllocateVendorAgentResult
                    {
                        Success = false,
                        ErrorMessage = "Vendor not mapped to agent"
                    };
                }

                var claim = await _vendorInvestigationDetailService.AssignToVendorAgent(
                    vendorAgent.Email,
                    allocatedByEmail,
                    vendorAgent.VendorId.Value,
                    claimId);

                if (claim == null)
                {
                    return new AllocateVendorAgentResult
                    {
                        Success = false,
                        ErrorMessage = $"Error occurred while assigning case {claimId}"
                    };
                }

                return new AllocateVendorAgentResult
                {
                    Success = true,
                    VendorAgentEmail = vendorAgent.Email,
                    VendorId = vendorAgent.VendorId.Value,
                    ContractNumber = claim.PolicyDetail.ContractNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Allocation failed for Case {ClaimId} by {User}",
                    claimId,
                    allocatedByEmail ?? "Anonymous");

                return new AllocateVendorAgentResult
                {
                    Success = false,
                    ErrorMessage = "Unexpected system error"
                };
            }
        }
    }
}