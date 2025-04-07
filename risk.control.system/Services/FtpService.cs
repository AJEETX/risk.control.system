using CsvHelper;
using CsvHelper.Configuration;

using Hangfire;

using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using System.ComponentModel.Design;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace risk.control.system.Services
{
    public interface IFtpService
    {
        Task<int> UploadFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual);
        Task StartUpload(string userEmail, int uploadId, string url);

        Task<bool> UploadFtpFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, long lineOfBusinessId);
    }

    public class FtpService : IFtpService
    {
        private const string CLAIMS = "claims";
        private static string NO_DATA = " NO - DATA ";
        private static Regex regex = new Regex("\\\"(.*?)\\\"");
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ICustomApiCLient customApiCLient;
        private readonly IMailboxService mailboxService;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IProgressService progressService;
        private readonly IUploadService uploadService;
        private static WebClient client = new WebClient
        {
            Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA),
        };
        public FtpService(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment, 
            ICustomApiCLient customApiCLient,
            IMailboxService mailboxService,
            IBackgroundJobClient backgroundJobClient,
            IProgressService progressService,
            IUploadService uploadService)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.customApiCLient = customApiCLient;
            this.mailboxService = mailboxService;
            this.backgroundJobClient = backgroundJobClient;
            this.progressService = progressService;
            this.uploadService = uploadService;
        }

        public async Task<bool> UploadFtpFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, long lineOfBusinessId)
        {
            try
            {
                string folder = Path.Combine(webHostEnvironment.WebRootPath, "upload-ftp");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = Path.GetFileName(postedFile.FileName);
                string ftpUrl = Applicationsettings.FTP_SITE + fileName;  // Replace with your folder and filename

                string localZipFilePath = Path.Combine(folder, fileName);
                using (FileStream stream = new FileStream(localZipFilePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }
                byte[] fileBytes = await File.ReadAllBytesAsync(localZipFilePath);
                var byesUploaded = await client.UploadDataTaskAsync(new Uri(ftpUrl), fileBytes);

                Console.WriteLine("ZIP file uploaded successfully.");

                var processed = await DownloadFtp(userEmail, autoOrManual, lineOfBusinessId);
                if (!processed)
                {
                    return false;
                }
                byte[] byteData;
                using (MemoryStream ms = new MemoryStream())
                {
                    postedFile.CopyTo(ms);
                    byteData = ms.ToArray();
                }
                var uploadId = await SaveUpload(postedFile, localZipFilePath, "File upload", userEmail, byteData, autoOrManual, ORIGIN.FILE);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private async Task<bool> DownloadFtp(string userEmail, CREATEDBY autoOrManual, long lineOfBusinessId)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "download-ftp");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var files = GetFtpData();
            var zipFiles = files.Where(f => Path.GetExtension(f).Equals(".zip"));

            foreach (var zipFile in zipFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipFile);
                string filePath = Path.Combine(path, zipFile);
                string ftpPath = $"{Applicationsettings.FTP_SITE}/{zipFile}";
                client.DownloadFile(ftpPath, filePath);
                using (var archive = ZipFile.OpenRead(filePath))
                {
                    var csvFileCount = archive.Entries.Count(e => Path.GetExtension(e.FullName).Equals(".csv"));
                    var innerFile = archive.Entries.FirstOrDefault(e => Path.GetExtension(e.FullName).Equals(".csv"));
                    if (innerFile == null || csvFileCount != 1)
                    {
                        return false;
                    }
                    var processed = await ProcessFile(userEmail, archive, autoOrManual, ORIGIN.FTP,lineOfBusinessId);
                    if (!processed)
                    {
                        return false;
                    }
                }
            }
            var rows = _context.SaveChanges();
            foreach (var zipFile in zipFiles)
            {
                string fileName = zipFile;

                FtpWebRequest requestFileDelete = (FtpWebRequest)WebRequest.Create($"{Applicationsettings.FTP_SITE}" + fileName);
                requestFileDelete.Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA);
                requestFileDelete.Method = WebRequestMethods.Ftp.DeleteFile;

                FtpWebResponse responseFileDelete = (FtpWebResponse)requestFileDelete.GetResponse();
            }
            return true;
        }

        public async Task<int> UploadFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-file");

            // Ensure the directory exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Use the actual file name instead of a temp file name
            string filePath = Path.Combine(path, Path.GetFileName(postedFile.FileName));

            //using (FileStream stream = new FileStream(filePath, FileMode.Create))
            //{
            //    postedFile.CopyTo(stream);
            //}

            // If you need the file as bytes:
            byte[] byteData;
            using (MemoryStream ms = new MemoryStream())
            {
                postedFile.CopyTo(ms);
                byteData = ms.ToArray();
            }

            // filePath now contains the saved file location

            //using (var stream = postedFile.OpenReadStream())
            //{
            //    using (var archive = new ZipArchive(stream))
            //    {
            //        var csvFileCount = archive.Entries.Count(e => Path.GetExtension(e.FullName).Equals(".csv"));
            //        var innerFile = archive.Entries.FirstOrDefault(e => Path.GetExtension(e.FullName).Equals(".csv"));
            //        if (innerFile == null || csvFileCount != 1)
            //        {
            //            return false;
            //        }
            //        var processed = await ProcessFile(userEmail, archive, autoOrManual, ORIGIN.FILE, lineOfBusinessId);
            //        if (!processed)
            //        {
            //            return false;
            //        }

            //    }
            //}
            var uploadId = await SaveUpload(postedFile, filePath, "File upload", userEmail, byteData, autoOrManual, ORIGIN.FILE);
            return uploadId;
        }

        private async Task<int> SaveUpload(IFormFile file, string filePath, string description, string uploadedBy, byte[] byteData, CREATEDBY autoOrManual, ORIGIN fileOrFtp)
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
                FileOrFtp = fileOrFtp
            };
            var uploadData = _context.FilesOnFileSystem.Add(fileModel);
            await _context.SaveChangesAsync();
            return uploadData.Entity.Id;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task StartUpload(string userEmail, int uploadId, string url)
        {
            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);
            var uploadFileData = await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            var customData = ReadFirstCsvFromZipToObject(uploadFileData.ByteData); // Read the first CSV file from the ZIP archive
            var totalClaimsCreated = await _context.ClaimsInvestigation.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
            var totalIncludingUploaded = totalClaimsCreated + customData.Count - 1;
            var userCanCreate = true;
            if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
            {
                if (totalClaimsCreated >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                }
            }
            else {
                userCanCreate = companyUser.ClientCompany.TotalCreatedClaimAllowed >= totalIncludingUploaded;
            }
            if (userCanCreate)
            {
                var uploadedCount = await uploadService.PerformCustomUpload(companyUser, customData, uploadFileData);
                if(uploadedCount > 0)
                {
                    uploadFileData.Completed = true;
                    uploadFileData.Icon = "fas fa-check-circle i-green";
                    uploadFileData.Status = "Completed";
                    uploadFileData.Message = $"Total cases uploaded : {uploadedCount}";
                    uploadFileData.RecordCount = uploadedCount;
                }
                else
                {
                    uploadFileData.Completed = false;
                    uploadFileData.Icon = "fas fa-times-circle i-orangered";
                    uploadFileData.Status = "Error";
                    uploadFileData.Message = "Error uploading the file";
                }
                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyFileUpload(userEmail, uploadFileData, url));

                await _context.SaveChangesAsync();
            }
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
        public static List<string>? ReadFirstCsvFromZip(byte[] zipData)
        {
            using (MemoryStream zipStream = new MemoryStream(zipData))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // Find the first file that has a .csv extension
                ZipArchiveEntry? csvEntry = archive.Entries.FirstOrDefault(e =>
                    e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

                if (csvEntry != null)
                {
                    using (StreamReader reader = new StreamReader(csvEntry.Open()))
                    {
                        List<string> lines = new List<string>();
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                        return lines; // Return the list of lines from the first CSV found
                    }
                }
            }
            return null; // Return null if no CSV file is found
        }

        private async Task<bool> ProcessFile(string userEmail, ZipArchive archive, CREATEDBY autoOrManual, ORIGIN fileOrFtp, long lineOfBusinessId)
        {

            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);
            bool userCanCreate = true;
            var totalClaimsCreated = await _context.ClaimsInvestigation.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
            if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
            {
                if (totalClaimsCreated >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                }
            }

            if (!userCanCreate)
            {
                return userCanCreate;
            }

            var innerFile = archive.Entries.FirstOrDefault(e => Path.GetExtension(e.FullName).Equals(".csv"));

            string csvData = string.Empty;
            using (var ss = innerFile.Open())
            {
                using (var memoryStream = new MemoryStream())
                {
                    ss.CopyTo(memoryStream);
                    var bytes = memoryStream.ToArray();
                    csvData = Encoding.UTF8.GetString(bytes);
                }
            }
            var dataRows = csvData.Split('\n');

            var totalIncludingUploaded = totalClaimsCreated + dataRows.Length - 1;
            var userCanUpload = true;
            if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
            {
                userCanUpload = companyUser.ClientCompany.TotalCreatedClaimAllowed >= totalIncludingUploaded;
            }
            if (userCanCreate && userCanUpload)
            {

                return await uploadService.DoUpload(companyUser, dataRows, autoOrManual, archive, fileOrFtp, lineOfBusinessId);
            }
            return false;

        }
        private static List<string> GetFtpData()
        {
            var request = WebRequest.Create(Applicationsettings.FTP_SITE);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA);

            var files = new List<string>();

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            var file = reader.ReadLine();
                            //Make sure you only get the filename and not the whole path.
                            file = file.Substring(file.LastIndexOf('/') + 1);
                            //The root folder will also be added, this can of course be ignored.
                            if (!file.StartsWith("."))
                                files.Add(file);
                        }
                    }
                }
            }

            return files;
        }
    }
}