using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Common
{
    public interface ITimelineService
    {
        Task UpdateTaskStatus(long taskId, string updatedBy, string subStatus = "");
        Task UpdateCaseStatus(long taskId, string updatedBy, string subStatus = "");
    }

    internal class TimelineService(IDbContextFactory<ApplicationDbContext> contextFactory) : ITimelineService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

        public async Task UpdateCaseStatus(long taskId, string updatedBy, string subStatus = "")
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var task = await context.SubmittedForms.AsNoTracking()
                .Include(t => t.CaseTimelines)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return;

            // Get last status history
            var lastHistory = task.CaseTimelines.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            // Calculate duration
            TimeSpan? duration = null;
            if (lastHistory != null)
            {
                duration = DateTime.UtcNow - lastHistory.StatusChangedAt;
            }

            if (!string.IsNullOrWhiteSpace(subStatus))
            {
                task.Status = subStatus;
            }
            // Add new status history
            var history = new CaseTimeline
            {
                InvestigationTaskId = task.Id,
                Status = task.Status,
                UpdatedBy = updatedBy,
                AssigedTo = task.CaseOwner!,
                StatusChangedAt = DateTime.UtcNow,
                Duration = duration
            };

            task.CaseTimelines.Add(history);
            context.SubmittedForms.Update(task);
            await context.SaveChangesAsync(null, false);
        }

        public async Task UpdateTaskStatus(long taskId, string updatedBy, string subStatus = "")
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
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
                AssigedTo = task.CaseOwner!,
                StatusChangedAt = DateTime.UtcNow,
                Duration = duration
            };

            task.InvestigationTimeline.Add(history);
            context.Investigations.Update(task);
            await context.SaveChangesAsync(null, false);
        }
    }
}