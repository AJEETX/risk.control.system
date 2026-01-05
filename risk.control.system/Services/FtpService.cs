using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using Hangfire;

using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IFtpService
    {
        Task<int> UploadFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, bool uploadAndAssign = false);
        Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false);

    }

    internal class FtpService : IFtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FtpService> logger;
        private readonly IFileStorageService fileStorageService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ITimelineService timelineService;
        private readonly IInvestigationService investigationService;
        private readonly IMailService mailService;
        private readonly IProcessCaseService processCaseService;
        private readonly IUploadService uploadService;

        public FtpService(ApplicationDbContext context,
            ILogger<FtpService> logger,
            IFileStorageService fileStorageService,
            IWebHostEnvironment webHostEnvironment,
            ITimelineService timelineService,
            IInvestigationService investigationService,
            IMailService mailService,
            IProcessCaseService processCaseService,
            IUploadService uploadService)
        {
            _context = context;
            this.logger = logger;
            this.fileStorageService = fileStorageService;
            this.webHostEnvironment = webHostEnvironment;
            this.timelineService = timelineService;
            this.investigationService = investigationService;
            this.mailService = mailService;
            this.processCaseService = processCaseService;
            this.uploadService = uploadService;
        }

        public async Task<int> UploadFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, bool uploadAndAssign = false)
        {
            var (fileName, relativePath) = await fileStorageService.SaveAsync(postedFile, "UploadFile");

            using (var dataStream = new MemoryStream())
            {
                await postedFile.CopyToAsync(dataStream);
                await File.WriteAllBytesAsync(relativePath, dataStream.ToArray());
            }

            var uploadId = await SaveUpload(postedFile, relativePath, fileName, userEmail, autoOrManual, ORIGIN.FILE, uploadAndAssign);
            return uploadId;
        }

        private async Task<int> SaveUpload(IFormFile file, string filePath, string description, string uploadedBy, CREATEDBY autoOrManual, ORIGIN fileOrFtp, bool uploadAndAssign = false)
        {
            var fileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var company = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == uploadedBy);
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
                AutoOrManual = autoOrManual,
                Message = uploadAndAssign ? "Assign In progress" : "Upload In progress",
                FileOrFtp = fileOrFtp,
                DirectAssign = uploadAndAssign
            };
            var uploadData = _context.FilesOnFileSystem.Add(fileModel);
            await _context.SaveChangesAsync();
            return uploadData.Entity.Id;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false)
        {
            var companyUser = await _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(c => c.Email == userEmail);
            var uploadFileData = await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            var filePath = Path.Combine(webHostEnvironment.ContentRootPath, uploadFileData.FilePath);

            var zipFileByteData = await File.ReadAllBytesAsync(filePath);
            var (validRecords, errors) = ReadPipeDelimitedCsvFromZip(zipFileByteData); // Read the first CSV file from the ZIP archive

            if (errors.Any())
            {
                string errorString = string.Join(Environment.NewLine, errors); // or use "," or "\n"
                uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                SetFileUploadFailure(uploadFileData, $"{errorString}", uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                return;
            }

            if (validRecords.Count == 0)
            {
                string errorString = "No data";
                uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                SetFileUploadFailure(uploadFileData, $"{errorString}", uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                return;

            }
            var totalClaimsCreated = await _context.Investigations.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);

            try
            {
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial)
                {
                    if (totalClaimsCreated > companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        SetFileUploadFailure(uploadFileData, $"Case limit reached of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s).", uploadAndAssign);
                        await _context.SaveChangesAsync();
                        await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                        return;
                    }
                }

                var uploadedCaseResult = await uploadService.FileUpload(companyUser, validRecords, zipFileByteData, uploadFileData.FileOrFtp);
                var sb = new StringBuilder();

                if (uploadedCaseResult.Count > 0)
                {
                    var rowNum = 0;
                    sb.AppendLine("Row #, [FieldName = Error detail]"); // CSV header
                    foreach (var result in uploadedCaseResult)
                    {
                        var errorList = result.ErrorDetail;
                        var caseErrors = result.Errors;
                        if (caseErrors.Count > 0)
                        {
                            ++rowNum;
                            sb.AppendLine($"\"{rowNum}\", {string.Join(",", caseErrors)}");
                        }
                    }
                    if (rowNum > 0)
                    {
                        SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                        byte[] errorBytes = Encoding.UTF8.GetBytes(sb.ToString());
                        uploadFileData.ErrorByteData = errorBytes;
                        await _context.SaveChangesAsync();
                        await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                        return;
                    }
                }
                var uploadedCases = uploadedCaseResult.Select(c => c.InvestigationTask)?.ToList();

                if (uploadedCases == null || uploadedCases.Count == 0)
                {
                    SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);

                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }
                var totalAddedAndExistingCount = uploadedCases.Count + totalClaimsCreated;
                // License Check (if Trial)
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial)
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
                if (uploadedCases.Count + totalReadyToAssign > companyUser.ClientCompany.TotalToAssignMaxAllowed)
                {
                    SetFileUploadFailure(uploadFileData, $"Max count of {companyUser.ClientCompany.TotalToAssignMaxAllowed} Assign-Ready Case(s) limit reached.", uploadAndAssign, uploadedCases.Select(c => c.Id).ToList());
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }

                _context.Investigations.AddRange(uploadedCases);

                var ros = await _context.SaveChangesAsync();

                try
                {

                    if (uploadAndAssign && uploadedCases.Any())
                    {
                        // Auto-Assign Claims if Enabled
                        var claimsIds = uploadedCases.Select(c => c.Id).ToList();
                        var autoAllocated = await processCaseService.BackgroundUploadAutoAllocation(claimsIds, userEmail, url);
                        SetUploadAssignSuccess(uploadFileData, uploadedCases, autoAllocated);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Upload Success
                        SetUploadSuccess(uploadFileData, uploadedCases);
                        await _context.SaveChangesAsync();

                        var updateTasks = uploadedCases.Select(u => timelineService.UpdateTaskStatus(u.Id, userEmail));
                        await Task.WhenAll(updateTasks);

                        // Notify User
                        await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred");
                    SetFileUploadFailure(uploadFileData, "Error Assigning cases", uploadAndAssign, uploadedCases.Select(u => u.Id).ToList());
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }
            }

            catch (Exception ex)
            {
                    logger.LogError(ex, "Error occurred");
                SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                return;
            }


        }

        private static (List<UploadCase> ValidRecords, List<string> Errors) ReadPipeDelimitedCsvFromZip(byte[] zipData)
        {
            var validRecords = new List<UploadCase>();
            var errors = new List<string>();

            using var zipStream = new MemoryStream(zipData);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var csvEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            if (csvEntry == null)
            {
                errors.Add("No CSV file found in ZIP.");
                return (validRecords, errors);
            }
            string? firstLine;
            using (var detectReader = new StreamReader(csvEntry.Open()))
            {
                firstLine = detectReader.ReadLine();
            }
            if (string.IsNullOrWhiteSpace(firstLine))
            {
                errors.Add("CSV file is empty or has no header.");
                return (validRecords, errors);
            }
            int pipeCount = firstLine.Count(c => c == '|');

            if (pipeCount < 26)
            {
                errors.Add("The file is has less than expected columns.");
                return (validRecords, errors);
            }
            using var reader = new StreamReader(csvEntry.Open());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "|", // 👈 Pipe-delimited
                TrimOptions = TrimOptions.Trim,
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = context =>
                {
                    errors.Add($"Row {context.Field}: Bad data - {context.RawRecord}");
                }
            });

            int rowNumber = 1;
            try
            {
                csv.Read();
                csv.ReadHeader();
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading header: {ex.Message}");
                return (validRecords, errors);
            }

            while (csv.Read())
            {
                rowNumber++;
                try
                {
                    var record = csv.GetRecord<UploadCase>();
                    validRecords.Add(record);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNumber}: {ex.Message}");
                }
            }

            return (validRecords, errors);
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

            string message = $"Uploaded Claims: {claimCount} & Underwritings: {underWritingCount}";
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