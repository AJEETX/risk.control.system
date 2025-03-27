using System.Collections.Concurrent;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public class BackgroundTaskService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BackgroundTaskService> _logger;
        private readonly ConcurrentQueue<(ClaimsInvestigation claim, string id)> _taskQueue = new();

        public BackgroundTaskService(IServiceScopeFactory serviceScopeFactory, ILogger<BackgroundTaskService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public void EnqueueTask(ClaimsInvestigation claim, string claimsInvestigationId)
        {
            _taskQueue.Enqueue((claim, claimsInvestigationId));
            _logger.LogInformation($"Task queued for claimsInvestigationId: {claimsInvestigationId}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_taskQueue.TryDequeue(out var task))
                {
                    try
                    {
                        await ProcessTask(task.claim, task.id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing task for {id}", task.id);
                    }
                }
                await Task.Delay(1000, stoppingToken); // Prevents excessive CPU usage
            }
        }

        private async Task ProcessTask(ClaimsInvestigation claim, string claimsInvestigationId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");
            Directory.CreateDirectory(folder);

            var filename = $"report{claimsInvestigationId}.pdf";
            var filePath = Path.Combine(folder, filename);

            (await PdfReportRunner.Run(webHostEnvironment.WebRootPath, claim)).Build(filePath);

            claim.AgencyReport.PdfReportFilePath = Path.Combine("report", filename);
            context.Update(claim);
            await context.SaveChangesAsync();

            _logger.LogInformation($"Task completed for claimsInvestigationId: {claimsInvestigationId}");
        }
    }

}
