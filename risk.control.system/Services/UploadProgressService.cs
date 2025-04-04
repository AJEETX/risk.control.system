using Hangfire;
using System.Collections.Concurrent;

namespace risk.control.system.Services
{
    public interface IUploadProgressService
    {
        void UpdateProgress(int jobId, int progress);
        int GetProgress(int jobId);
    }
    public class UploadProgressService : IUploadProgressService
    {
        private static ConcurrentDictionary<int, int> jobProgress = new();

        public void UpdateProgress(int jobId, int progress)
        {
            jobProgress[jobId] = progress;
        }

        public int GetProgress(int jobId)
        {
            return jobProgress.TryGetValue(jobId, out int progress) ? progress : 0;
        }
    }
}
