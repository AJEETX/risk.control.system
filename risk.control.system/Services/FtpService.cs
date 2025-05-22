using CsvHelper;
using CsvHelper.Configuration;

using Hangfire;

using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Net;

namespace risk.control.system.Services
{
    public interface IFtpService
    {
        Task<int> UploadFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, bool uploadAndAssign = false);
        Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false);

    }

    public class FtpService : IFtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ITimelineService timelineService;
        private readonly IInvestigationService investigationService;
        private readonly IMailService mailService;
        private readonly IProcessCaseService processCaseService;
        private readonly IUploadService uploadService;
        private static WebClient client = new WebClient
        {
            Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA),
        };
        public FtpService(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ITimelineService timelineService,
            IInvestigationService investigationService,
            IMailService mailService,
            IProcessCaseService processCaseService,
            IUploadService uploadService)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.timelineService = timelineService;
            this.investigationService = investigationService;
            this.mailService = mailService;
            this.processCaseService = processCaseService;
            this.uploadService = uploadService;
        }

        public async Task<int> UploadFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, bool uploadAndAssign = false)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-file");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filePath = Path.Combine(path, Path.GetFileName(postedFile.FileName));

            byte[] byteData;
            using (MemoryStream ms = new MemoryStream())
            {
                postedFile.CopyTo(ms);
                byteData = ms.ToArray();
            }

            var uploadId = await SaveUpload(postedFile, filePath, "File upload", userEmail, byteData, autoOrManual, ORIGIN.FILE, uploadAndAssign);
            return uploadId;
        }

        private async Task<int> SaveUpload(IFormFile file, string filePath, string description, string uploadedBy, byte[] byteData, CREATEDBY autoOrManual, ORIGIN fileOrFtp, bool uploadAndAssign = false)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var company = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == uploadedBy);
            int lastCompanySequence = await _context.FilesOnFileSystem.Where(f => f.CompanyId == company.ClientCompanyId).MaxAsync(f => (int?)f.CompanySequenceNumber) ?? 0;

            // Get the last User-Level sequence (within the company)
            int lastUserSequence = await _context.FilesOnFileSystem.Where(f => f.CompanyId == company.ClientCompanyId && f.UploadedBy == uploadedBy).MaxAsync(f => (int?)f.UserSequenceNumber) ?? 0;
            var fileModel = new FileOnFileSystemModel
            {
                CompanySequenceNumber = lastCompanySequence + 1,
                UserSequenceNumber = lastUserSequence + 1,
                CreatedOn = DateTime.Now,
                FileType = file.ContentType,
                Extension = extension,
                Name = fileName,
                Description = description,
                FilePath = filePath,
                UploadedBy = uploadedBy,
                CompanyId = company.ClientCompanyId,
                ByteData = byteData,
                AutoOrManual = autoOrManual,
                Message = uploadAndAssign ? "Assign In progress" : "Upload In progress",
                FileOrFtp = fileOrFtp,
                DirectAssign = uploadAndAssign
            };
            var uploadData = _context.FilesOnFileSystem.Add(fileModel);
            await _context.SaveChangesAsync();
            return uploadData.Entity.Id;
        }

        void SetUploadFailure(FileOnFileSystemModel fileData, string message, bool uploadAndAssign, List<string> claimsIds = null)
        {
            fileData.Completed = false;
            fileData.Icon = "fas fa-times-circle i-orangered";
            fileData.Status = "Error";
            fileData.Message = message;
            fileData.DirectAssign = uploadAndAssign;
            fileData.CompletedOn = DateTime.Now;
            fileData.ClaimsId = claimsIds;
        }

        private static List<UploadCase>? ReadFirstCsvFromZipToObject(byte[] zipData)
        {
            using (MemoryStream zipStream = new MemoryStream(zipData))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // Find the first CSV file in the ZIP archive
                ZipArchiveEntry? csvEntry = archive.Entries.FirstOrDefault(e =>
                    e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

                if (csvEntry != null)
                {
                    using (StreamReader reader = new StreamReader(csvEntry.Open()))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        TrimOptions = TrimOptions.Trim,
                        HeaderValidated = null,  // Disables header validation errors
                        MissingFieldFound = null // Prevents missing field errors
                    }))
                    {
                        return csv.GetRecords<UploadCase>().ToList(); // Convert CSV rows to objects
                    }
                }
            }
            return null; // Return null if no CSV is found
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false)
        {
            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);
            var uploadFileData = await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            var customData = ReadFirstCsvFromZipToObject(uploadFileData.ByteData); // Read the first CSV file from the ZIP archive
            var totalClaimsCreated = await _context.Investigations.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);

            try
            {
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    if (totalClaimsCreated > companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        SetFileUploadFailure(uploadFileData, $"Case limit reached of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s).", uploadAndAssign);
                        await _context.SaveChangesAsync();
                        await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                        return;
                    }
                }

                var uploadedClaims = await uploadService.FileUpload(companyUser, customData, uploadFileData);
                if (uploadedClaims == null || uploadedClaims.Count == 0)
                {
                    SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }
                var totalAddedAndExistingCount = uploadedClaims.Count + totalClaimsCreated;
                // License Check (if Trial)
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    if (totalAddedAndExistingCount > companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        SetFileUploadFailure(uploadFileData, $"Case limit exceeded of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s).", uploadAndAssign);
                        await _context.SaveChangesAsync();
                        await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                        return;
                    }
                }

                var totalReadyToAssign = await investigationService.GetAutoCount(userEmail);
                if (uploadedClaims.Count + totalReadyToAssign > companyUser.ClientCompany.TotalToAssignMaxAllowed)
                {
                    SetFileUploadFailure(uploadFileData, $"Max count of {companyUser.ClientCompany.TotalToAssignMaxAllowed} Assign-Ready Case(s) limit reached.", uploadAndAssign, uploadedClaims.Select(c => c.Id).ToList());
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }

                _context.Investigations.AddRange(uploadedClaims);

                var ros = await _context.SaveChangesAsync();

                try
                {

                    if (uploadAndAssign && uploadedClaims.Any())
                    {
                        // Auto-Assign Claims if Enabled
                        var claimsIds = uploadedClaims.Select(c => c.Id).ToList();
                        var autoAllocated = await processCaseService.BackgroundUploadAutoAllocation(claimsIds, userEmail, url);
                        SetUploadAssignSuccess(uploadFileData, uploadedClaims, autoAllocated);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Upload Success
                        SetUploadSuccess(uploadFileData, uploadedClaims);
                        await _context.SaveChangesAsync();

                        var updateTasks = uploadedClaims.Select(u => timelineService.UpdateTaskStatus(u.Id, userEmail));
                        await Task.WhenAll(updateTasks);

                        // Notify User
                        await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    SetFileUploadFailure(uploadFileData, "Error Assigning cases", uploadAndAssign, uploadedClaims.Select(u => u.Id).ToList());
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                return;
            }


        }

        private static void SetUploadAssignSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims, List<long> autoAllocated)
        {
            var uploadedClaimCount = claims.Count(c => c.PolicyDetail.InsuranceType == InsuranceType.CLAIM);

            var assignedClaimCount = claims.Count(c => autoAllocated.Contains(c.Id) && c.PolicyDetail.InsuranceType == InsuranceType.CLAIM);

            var uploadedUnderWritingCount = claims.Count(c => c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING);
            var assignedUnderWritingCount = claims.Count(c => autoAllocated.Contains(c.Id) && c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING);

            string message = $"Claims (Uploaded/Assigned) = ({uploadedClaimCount}/{assignedClaimCount}): Underwritings (Uploaded/Assigned) = ({uploadedUnderWritingCount}/{assignedUnderWritingCount})";
            fileData.Completed = true;
            fileData.Icon = "fas fa-check-circle i-green";
            fileData.Status = "Completed";
            fileData.Message = message;
            fileData.DirectAssign = true;
            fileData.RecordCount = claims.Count;
            fileData.CaseIds = claims.Select(c => c.Id).ToList();
            fileData.CompletedOn = DateTime.Now;
        }

        private static void SetUploadSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims)
        {
            var claimCount = claims.Count(c => c.PolicyDetail.InsuranceType == InsuranceType.CLAIM);
            var underWritingCount = claims.Count(c => c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING);

            string message = $"Total Uploaded Claims: {claimCount} & Underwritings: {underWritingCount}";
            fileData.Completed = true;
            fileData.Icon = "fas fa-check-circle i-green";
            fileData.Status = "Completed";
            fileData.Message = message;
            fileData.RecordCount = claims.Count;
            fileData.CaseIds = claims.Select(c => c.Id).ToList();
            fileData.CompletedOn = DateTime.Now;
        }

        private static void SetFileUploadFailure(FileOnFileSystemModel fileData, string message, bool uploadAndAssign, List<long> claimsIds = null)
        {
            fileData.Completed = false;
            fileData.Icon = "fas fa-times-circle i-orangered";
            fileData.Status = "Error";
            fileData.Message = message;
            fileData.DirectAssign = uploadAndAssign;
            fileData.CompletedOn = DateTime.Now;
            fileData.CaseIds = claimsIds;
        }
    }
}