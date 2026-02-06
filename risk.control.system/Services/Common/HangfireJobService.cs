using Hangfire;
using Hangfire.Storage;

namespace risk.control.system.Services.Common
{
    public interface IHangfireJobService
    {
        void CleanFailedJobs();
    }

    internal class HangfireJobService : IHangfireJobService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfireJobService(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public void CleanFailedJobs()
        {
            using (var connection = JobStorage.Current.GetConnection())
            {
                var failedJobs = GetFailedJobIds(connection);
                foreach (var jobId in failedJobs)
                {
                    _backgroundJobClient.Delete(jobId);
                }
            }
        }

        private List<string> GetFailedJobIds(IStorageConnection connection)
        {
            var failedJobs = new List<string>();

            // Query the Hangfire database for failed job IDs
            using (var transaction = connection.CreateWriteTransaction())
            {
                var failedJobsSet = connection.GetAllItemsFromSet("failed");
                failedJobs.AddRange(failedJobsSet);
            }

            return failedJobs;
        }
    }
}