//using System.Collections.Concurrent;

//using Microsoft.EntityFrameworkCore;

//using risk.control.system.Data;
//using risk.control.system.Helpers;
//using risk.control.system.Models;

//namespace risk.control.system.Services
//{
//    public interface IBackgroundTaskService
//    {
//        void EnqueueTask(string claimsInvestigationId);
//    }
//    public class BackgroundTaskService : IBackgroundTaskService
//    {
//        private readonly IServiceScopeFactory _serviceScopeFactory;
//        private readonly ILogger<BackgroundTaskService> _logger;
//        private readonly ConcurrentQueue<string> _taskQueue = new();

//        public BackgroundTaskService(IServiceScopeFactory serviceScopeFactory, ILogger<BackgroundTaskService> logger)
//        {
//            _serviceScopeFactory = serviceScopeFactory;
//            _logger = logger;
//        }

//        public void EnqueueTask(string claimsInvestigationId)
//        {
//            _taskQueue.Enqueue(claimsInvestigationId);
//            _logger.LogInformation($"Task queued for claimsInvestigationId: {claimsInvestigationId}");
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                if (_taskQueue.TryDequeue(out var task))
//                {
//                    try
//                    {
//                        await ProcessTask(task.id);
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, "Error processing task for {id}", task.id);
//                    }
//                }
//                await Task.Delay(1000, stoppingToken); // Prevents excessive CPU usage
//            }
//        }

//        private async Task ProcessTask(string claimsInvestigationId)
//        {
//            using var scope = _serviceScopeFactory.CreateScope();
//            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//            var claim = context.ClaimsInvestigation
//                    .Include(c => c.CustomerDetail)
//                    .ThenInclude(c => c.District)
//                    .Include(c => c.CustomerDetail)
//                    .ThenInclude(c => c.State)
//                    .Include(c => c.CustomerDetail)
//                    .ThenInclude(c => c.Country)
//                    .Include(c => c.CustomerDetail)
//                    .ThenInclude(c => c.PinCode)
//                    .Include(c => c.BeneficiaryDetail)
//                    .ThenInclude(c => c.District)
//                    .Include(c => c.BeneficiaryDetail)
//                    .ThenInclude(c => c.State)
//                    .Include(c => c.BeneficiaryDetail)
//                    .ThenInclude(c => c.Country)
//                    .Include(c => c.BeneficiaryDetail)
//                    .ThenInclude(c => c.PinCode)
//                    .Include(c => c.ClientCompany)
//                    .Include(c => c.PolicyDetail)
//                .ThenInclude(c => c.LineOfBusiness)
//               .Include(c => c.PolicyDetail)
//               .ThenInclude(c => c.CaseEnabler)
//                .Include(r => r.AgencyReport)
//                .ThenInclude(r => r.DigitalIdReport)
//                .Include(r => r.AgencyReport)
//                .ThenInclude(r => r.PanIdReport)
//                .Include(r => r.Vendor)
//                .ThenInclude(v => v.Country)
//               .Include(c => c.PolicyDetail)
//               .ThenInclude(c => c.CaseEnabler)
//                .Include(r => r.AgencyReport)
//                .ThenInclude(r => r.AgentIdReport)
//                .Include(r => r.AgencyReport)
//                .ThenInclude(r => r.ReportQuestionaire)
//                .Include(r => r.Vendor)
//                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);
//            var webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

//            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");
//            Directory.CreateDirectory(folder);

//            var filename = $"report{claimsInvestigationId}.pdf";
//            var filePath = Path.Combine(folder, filename);

//            (await PdfReportRunner.Run(webHostEnvironment.WebRootPath, claim)).Build(filePath);

//            claim.AgencyReport.PdfReportFilePath = Path.Combine("report", filename);
//            context.Update(claim);
//            await context.SaveChangesAsync();

//            _logger.LogInformation($"Task completed for claimsInvestigationId: {claimsInvestigationId}");
//        }
//    }

//}
