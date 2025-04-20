using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface ITimelineService
    {
        Task UpdateTaskStatus(long taskId, string updatedBy, string subStatus = "");
    }
    public class TimelineService : ITimelineService
    {
        private readonly ApplicationDbContext context;

        public TimelineService(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task UpdateTaskStatus(long taskId, string updatedBy, string subStatus = "")
        {
            var task = await context.Investigations
                .Include(t => t.InvestigationTimeline)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return;

            // Get last status history
            var lastHistory = task.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            // Calculate duration
            TimeSpan? duration = null;
            if (lastHistory != null)
            {
                duration = DateTime.Now - lastHistory.StatusChangedAt;
            }

            if(!string.IsNullOrWhiteSpace(subStatus))
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
                StatusChangedAt = DateTime.Now,
                Duration = duration
            };

            task.InvestigationTimeline.Add(history);
            context.Investigations.Update(task);
            await context.SaveChangesAsync();
        }
    }
}
