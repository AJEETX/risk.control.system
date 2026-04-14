using Hangfire;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Creator
{
    public interface IUploadZipFileService
    {
        Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false);
    }

    internal class UploadZipFileService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IUploadFileDataProcessor uploadFileDataProcessor,
        IUploadFileInitiator uploadFileInitiator,
        ICsvFileReaderService csvFileReaderService,
        ILogger<UploadZipFileService> logger) : IUploadZipFileService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
        private readonly IUploadFileDataProcessor _uploadFileDataProcessor = uploadFileDataProcessor;
        private readonly IUploadFileInitiator _uploadFileInitiator = uploadFileInitiator;
        private readonly ICsvFileReaderService _csvFileReaderService = csvFileReaderService;
        private readonly ILogger<UploadZipFileService> _logger = logger;

        [AutomaticRetry(Attempts = 2)]
        public async Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var companyUser = await context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).FirstOrDefaultAsync(c => c.Email == userEmail);

                var uploadFileData = await context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser!.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);

                var (zipFileByteData, validRecords, errors) = await _csvFileReaderService.ReadPipeDelimitedCsvFromZip(uploadFileData!);

                var totalClaimsCreated = await context.Investigations.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser!.ClientCompanyId);

                await _uploadFileDataProcessor.Process(companyUser!, validRecords, totalClaimsCreated, uploadFileData!, url, errors, uploadAndAssign);

                await _uploadFileInitiator.StartProcess(companyUser!, validRecords, zipFileByteData, uploadFileData!, totalClaimsCreated, url, uploadAndAssign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for {UploadId} for {UserEmail}", uploadId, userEmail);
            }
        }
    }
}