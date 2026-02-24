using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IFileUploadProcessor
    {
        Task ProcessloadFile(string userEmail, List<InvestigationTask> uploadedCases, FileOnFileSystemModel uploadFileData, string url, bool uploadAndAssign = false);
    }

    internal class FileUploadProcessor : IFileUploadProcessor
    {
        private readonly IUploadFileStatusService uploadFileStatusService;
        private readonly ILogger<FileUploadProcessor> logger;
        private readonly ITimelineService timelineService;
        private readonly IFileUploadCaseAllocationService fileUploadCaseAllocationService;
        private readonly IMailService mailService;

        public FileUploadProcessor(
            IUploadFileStatusService uploadFileStatusService,
            ILogger<FileUploadProcessor> logger,
            ITimelineService timelineService,
            IFileUploadCaseAllocationService fileUploadCaseAllocationService,
            IMailService mailService)
        {
            this.uploadFileStatusService = uploadFileStatusService;
            this.logger = logger;
            this.timelineService = timelineService;
            this.fileUploadCaseAllocationService = fileUploadCaseAllocationService;
            this.mailService = mailService;
        }

        public async Task ProcessloadFile(string userEmail, List<InvestigationTask> uploadedCases, FileOnFileSystemModel uploadFileData, string url, bool uploadAndAssign = false)
        {
            try
            {
                if (uploadAndAssign && uploadedCases.Any())
                {
                    // Auto-Assign Claims if Enabled
                    var autoAllocated = await fileUploadCaseAllocationService.UploadAutoAllocation(uploadedCases, userEmail, url);
                    await uploadFileStatusService.SetUploadAssignSuccess(uploadFileData, uploadedCases, autoAllocated);
                }
                else
                {
                    // Upload Success
                    await uploadFileStatusService.SetUploadSuccess(uploadFileData, uploadedCases);

                    // Add Timeline entry for all uploaded cases
                    var updateTasks = uploadedCases.Select(u => timelineService.UpdateTaskStatus(u.Id, userEmail));
                    await Task.WhenAll(updateTasks);

                    // Notify User
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Process Upload File. {UserEmail}", userEmail);
                var errorString = "Error processing upload file. Please try again.";
                uploadFileData.ErrorByteData = System.Text.Encoding.UTF8.GetBytes(errorString);
                await uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error processing the uploaded file", uploadAndAssign, uploadedCases.Select(u => u.Id).ToList());
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
            }
        }
    }
}