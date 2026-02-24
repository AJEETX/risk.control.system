using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Common
{
    public interface ITimelineService
    {
        Task UpdateTaskStatus(long taskId, string updatedBy, string subStatus = "");
    }

    internal class TimelineService : ITimelineService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public TimelineService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task UpdateTaskStatus(long taskId, string updatedBy, string subStatus = "")
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var task = await context.Investigations.AsNoTracking()
                .Include(t => t.InvestigationTimeline)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return;

            // Get last status history
            var lastHistory = task.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            // Calculate duration
            TimeSpan? duration = null;
            if (lastHistory != null)
            {
                duration = DateTime.UtcNow - lastHistory.StatusChangedAt;
            }

            if (!string.IsNullOrWhiteSpace(subStatus))
            {
                task.SubStatus = subStatus;
            }
            // Add new status history
            var history = new InvestigationTimeline
            {
                InvestigationTaskId = task.Id,
                Status = task.Status,
                SubStatus = task.SubStatus,
                UpdatedBy = updatedBy,
                AssigedTo = task.CaseOwner,
                StatusChangedAt = DateTime.UtcNow,
                Duration = duration
            };

            task.InvestigationTimeline.Add(history);
            context.Investigations.Update(task);
            await context.SaveChangesAsync(null, false);
        }
    }
}