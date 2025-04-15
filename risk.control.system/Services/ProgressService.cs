using Hangfire;
using System.Collections.Concurrent;

namespace risk.control.system.Services
{
    public interface IProgressService
    {
        void UpdateProgress(int jobId, int progress);
        void UpdateAssignmentProgress(string jobId, int progress);
        int GetProgress(int jobId);
        int GetAssignmentProgress(string jobId);

        void AddUploadJob(string userEmail, string jobId);
        void AddAssignmentJob(string userEmail, string jobId);
        List<string> GetUploadJobIds(string userEmail);
        List<string> GetAssignmentJobIds(string userEmail);
    }
    public class ProgressService : IProgressService
    {
        private static ConcurrentDictionary<int, int> jobProgress = new();
        private static ConcurrentDictionary<string, int> jobAssignmentProgress = new();
        private static ConcurrentDictionary<string, List<string>> uploadJobIds = new();
        private static ConcurrentDictionary<string, List<string>> assignmentJobIds = new();

        public List<string> GetUploadJobIds(string userEmail)
        {
            if (uploadJobIds.TryGetValue(userEmail, out var jobs))
            {
                return jobs;
            }
            return new List<string>(); // Return an empty list if no jobs exist
        }

        public void AddUploadJob(string jobId, string userEmail)
        {
            uploadJobIds.AddOrUpdate(userEmail,
                new List<string> { jobId },  // If user does not exist, create new list
                (key, existingJobs) =>
                {
                    existingJobs.Add(jobId);  // If user exists, add job to their list
                    return existingJobs;
                });
        }
        public void AddAssignmentJob(string jobId, string userEmail)
        {
            assignmentJobIds.AddOrUpdate(userEmail,
                new List<string> { jobId },  // If user does not exist, create new list
                (key, existingJobs) =>
                {
                    existingJobs.Add(jobId);  // If user exists, add job to their list
                    return existingJobs;
                });
        }

        public List<string> GetAssignmentJobIds(string userEmail)
        {
            if (assignmentJobIds.TryGetValue(userEmail, out var jobs))
            {
                return jobs;
            }
            return new List<string>(); // Return an empty list if no jobs exist
        }

        public void UpdateAssignmentProgress(string jobId, int progress)
        {
            jobAssignmentProgress[jobId] = progress;
        }
        public void UpdateProgress(int jobId, int progress)
        {
            jobProgress[jobId] = progress;
        }

        public int GetAssignmentProgress(string jobId)
        {
            return jobAssignmentProgress.TryGetValue(jobId, out int progress) ? progress : 0;
        }
        public int GetProgress(int jobId)
        {
            return jobProgress.TryGetValue(jobId, out int progress) ? progress : 0;
        }
    }
}
