using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IFileUploadProcessor
    {
        Task ProcessloadFile(string userEmail, List<InvestigationTask> uploadedCases, FileOnFileSystemModel uploadFileData, string url, bool uploadAndAssign = false);
    }

    internal class FileUploadProcessor(
        IUploadFileStatusService uploadFileStatusService,
        ILogger<FileUploadProcessor> logger,
        ITimelineService timelineService,
        IFileUploadCaseAllocationService fileUploadCaseAllocationService,
        IAwsFaceImageCheckService awsFaceImageCheckService,
        ICaseNotificationService caseNotificationService) : IFileUploadProcessor
    {
        private readonly IUploadFileStatusService _uploadFileStatusService = uploadFileStatusService;
        private readonly ILogger<FileUploadProcessor> _logger = logger;
        private readonly ITimelineService _timelineService = timelineService;
        private readonly IFileUploadCaseAllocationService _fileUploadCaseAllocationService = fileUploadCaseAllocationService;
        private readonly ICaseNotificationService _caseNotificationService = caseNotificationService;
        private readonly IAwsFaceImageCheckService _awsFaceImageCheckService = awsFaceImageCheckService;

        public async Task ProcessloadFile(string userEmail, List<InvestigationTask> uploadedCases, FileOnFileSystemModel uploadFileData, string url, bool uploadAndAssign = false)
        {
            try
            {
                if (uploadAndAssign && uploadedCases.Any())
                {
                    // Auto-Assign Claims if Enabled
                    var autoAllocated = await _fileUploadCaseAllocationService.UploadAutoAllocation(uploadedCases, userEmail, url);
                    await _uploadFileStatusService.SetUploadAssignSuccess(uploadFileData, uploadedCases, autoAllocated);
                }
                else
                {
                    // Upload Success
                    await _uploadFileStatusService.SetUploadSuccess(uploadFileData, uploadedCases);

                    // Add Timeline entry for all uploaded cases
                    var updateTasks = uploadedCases.Select(u => _timelineService.UpdateTaskStatus(u.Id, userEmail));
                    await Task.WhenAll(updateTasks);

                    // Notify User
                    await _caseNotificationService.NotifyFileUpload(userEmail, uploadFileData, url);
                }

                //Upload face images to AWS for all uploaded cases
                var awsFaceTasks = uploadedCases.Select(c => _awsFaceImageCheckService.SetCaseImagesToAws(c.Id));
                await Task.WhenAll(awsFaceTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred Process Upload File. {UserEmail}", userEmail);
                var errorString = "Error processing upload file. Please try again.";
                uploadFileData.ErrorByteData = System.Text.Encoding.UTF8.GetBytes(errorString);
                await _uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error processing the uploaded file", uploadAndAssign, uploadedCases.Select(u => u.Id).ToList());
                await _caseNotificationService.NotifyFileUpload(userEmail, uploadFileData, url);
            }
        }
    }
}