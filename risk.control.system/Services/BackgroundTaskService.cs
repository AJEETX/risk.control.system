using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public class BackgroundTaskService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BackgroundTaskService(IServiceScopeFactory serviceScopeFactory, IWebHostEnvironment webHostEnvironment)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _webHostEnvironment = webHostEnvironment;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Here you can implement a queue to process tasks
                await Task.Delay(5000, stoppingToken); // Example delay
            }
        }

        public async Task ProcessTask(ClaimsInvestigation claim, string claimsInvestigationId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "report");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filename = $"report{claimsInvestigationId}.pdf";
            var filePath = Path.Combine(folder, filename);

            (await PdfReportRunner.Run(_webHostEnvironment.WebRootPath, claim)).Build(filePath);

            claim.AgencyReport.PdfReportFilePath = Path.Combine("report", filename);
            context.Update(claim);
            await context.SaveChangesAsync();
        }
    }

}
