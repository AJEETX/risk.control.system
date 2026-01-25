using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IFileUploadProcessor
    {
        Task ProcessloadFile(string userEmail, List<InvestigationTask> uploadedCases, FileOnFileSystemModel uploadFileData, ApplicationUser companyUser, string url,  bool uploadAndAssign = false);
    }
    internal class FileUploadProcessor: IFileUploadProcessor
    {
        private readonly ApplicationDbContext _context;
        private readonly IUploadFileStatusService uploadFileStatusService;
        private readonly ILogger<FileUploadProcessor> logger;
        private readonly ITimelineService timelineService;
        private readonly IMailService mailService;
        private readonly IProcessCaseService processCaseService;

        public FileUploadProcessor(ApplicationDbContext context,
            IUploadFileStatusService uploadFileStatusService,
            ILogger<FileUploadProcessor> logger,
            ITimelineService timelineService,
            IMailService mailService,
            IProcessCaseService processCaseService)
        {
            _context = context;
            this.uploadFileStatusService = uploadFileStatusService;
            this.logger = logger;
            this.timelineService = timelineService;
            this.mailService = mailService;
            this.processCaseService = processCaseService;
        }
        public async Task ProcessloadFile(string userEmail, List<InvestigationTask> uploadedCases, FileOnFileSystemModel uploadFileData, ApplicationUser companyUser, string url, bool uploadAndAssign = false)
        {
            try
            {
                if (uploadAndAssign && uploadedCases.Any())
                {
                    // Auto-Assign Claims if Enabled
                    var claimsIds = uploadedCases.Select(c => c.Id).ToList();
                    var autoAllocated = await processCaseService.BackgroundUploadAutoAllocation(claimsIds, userEmail, url);
                    uploadFileStatusService.SetUploadAssignSuccess(uploadFileData, uploadedCases, autoAllocated);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Upload Success
                    uploadFileStatusService.SetUploadSuccess(uploadFileData, uploadedCases);
                    await _context.SaveChangesAsync();

                    var updateTasks = uploadedCases.Select(u => timelineService.UpdateTaskStatus(u.Id, userEmail));
                    await Task.WhenAll(updateTasks);

                    // Notify User
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                }
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error Assigning cases", uploadAndAssign, uploadedCases.Select(u => u.Id).ToList());
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
            }
        }
    }
}
