using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Creator
{
    public interface IDeleteCaseService
    {
        Task<(bool Success, string Message)> SoftDeleteCaseAsync(long id, string currentUserEmail);

        Task<(bool Success, string Message)> SoftDeleteBulkCasesAsync(List<long> ids, string currentUserEmail);
    }

    internal class DeleteCaseService : IDeleteCaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteCaseService> _logger;

        public DeleteCaseService(ApplicationDbContext context, ILogger<DeleteCaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> SoftDeleteCaseAsync(long id, string currentUserEmail)
        {
            if (id <= 0) return (false, "Invalid ID provided.");

            var investigation = await _context.Investigations.FindAsync(id);
            if (investigation == null) return (false, "Case not found.");

            ApplySoftDelete(investigation, currentUserEmail);

            await _context.SaveChangesAsync();
            return (true, "Case deleted successfully!");
        }

        public async Task<(bool Success, string Message)> SoftDeleteBulkCasesAsync(List<long> ids, string currentUserEmail)
        {
            if (ids == null || !ids.Any()) return (false, "No cases selected.");

            // Fetch all cases in one query for better performance
            var investigations = await _context.Investigations
                .Where(i => ids.Contains(i.Id) && !i.Deleted)
                .ToListAsync();

            if (investigations.Count == 0) return (false, "No valid cases found for deletion.");

            foreach (var investigation in investigations)
            {
                ApplySoftDelete(investigation, currentUserEmail);
            }

            await _context.SaveChangesAsync();
            return (true, $"{investigations.Count} cases deleted successfully.");
        }

        private void ApplySoftDelete(InvestigationTask investigation, string userEmail)
        {
            investigation.Updated = DateTime.UtcNow;
            investigation.UpdatedBy = userEmail;
            investigation.Deleted = true;
            _context.Investigations.Update(investigation);
        }
    }
}