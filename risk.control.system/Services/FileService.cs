using Hangfire;

using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IFileService
    {
        Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false);
    }

    internal class FileService : IFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUploadFileDataProcessor uploadFileDataProcessor;
        private readonly IUploadFileInitiator uploadFileInitiator;
        private readonly ICsvFileReaderService csvFileReaderService;
        private readonly ILogger<FileService> logger;

        public FileService(ApplicationDbContext context,
            IUploadFileDataProcessor uploadFileDataProcessor,
            IUploadFileInitiator uploadFileInitiator,
            ICsvFileReaderService csvFileReaderService,
            ILogger<FileService> logger)
        {
            _context = context;
            this.uploadFileDataProcessor = uploadFileDataProcessor;
            this.uploadFileInitiator = uploadFileInitiator;
            this.csvFileReaderService = csvFileReaderService;
            this.logger = logger;
        }


        [AutomaticRetry(Attempts = 0)]
        public async Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false)
        {
            try
            {
                var companyUser = await _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(c => c.Email == userEmail);
                var uploadFileData = await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);

                var (zipFileByteData, validRecords, errors) = await csvFileReaderService.ReadPipeDelimitedCsvFromZip(uploadFileData); // Read the first CSV file from the ZIP archive
                var totalClaimsCreated = await _context.Investigations.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);

                await uploadFileDataProcessor.Process(companyUser, validRecords, totalClaimsCreated, uploadFileData, url, errors, uploadAndAssign);

                await uploadFileInitiator.StartProcess(companyUser, validRecords, zipFileByteData, uploadFileData, totalClaimsCreated, url, uploadAndAssign);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {UploadId} for {UserEmail}", uploadId, userEmail);
            }
        }
    }
}