﻿using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using System.Data;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace risk.control.system.Services
{
    public interface IFtpService
    {
        Task<bool> UploadFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual,long lineOfBusinessId);
        Task<bool> UploadCaseFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual);

        Task<bool> UploadFtpFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, long lineOfBusinessId);
    }

    public class FtpService : IFtpService
    {
        private static string NO_DATA = " NO - DATA ";
        private static Regex regex = new Regex("\\\"(.*?)\\\"");
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ICustomApiCLient customApiCLient;
        private readonly IUploadService uploadService;
        private static WebClient client = new WebClient
        {
            Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA),
        };
        public FtpService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ICustomApiCLient customApiCLient, IUploadService uploadService)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.customApiCLient = customApiCLient;
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

                await SaveUpload(postedFile, localZipFilePath, "Ftp download", userEmail);
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

        public async Task<bool> UploadFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, long lineOfBusinessId)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-file");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filePath = Path.Combine(path, Path.GetTempFileName());

            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                postedFile.CopyTo(stream);
            }

            var rows = _context.SaveChanges();
            using (var stream = postedFile.OpenReadStream())
            {
                using (var archive = new ZipArchive(stream))
                {
                    var csvFileCount = archive.Entries.Count(e => Path.GetExtension(e.FullName).Equals(".csv"));
                    var innerFile = archive.Entries.FirstOrDefault(e => Path.GetExtension(e.FullName).Equals(".csv"));
                    if (innerFile == null || csvFileCount != 1)
                    {
                        return false;
                    }
                    var processed = await ProcessFile(userEmail, archive, autoOrManual, ORIGIN.FILE, lineOfBusinessId);
                    if (!processed)
                    {
                        return false;
                    }

                }
            }
            await SaveUpload(postedFile, filePath, "File upload", userEmail);
            return true;
        }

        private async Task SaveUpload(IFormFile file, string filePath, string description, string uploadedBy)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var company = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == uploadedBy);
            var fileModel = new FileOnFileSystemModel
            {
                CreatedOn = DateTime.Now,
                FileType = file.ContentType,
                Extension = extension,
                Name = fileName,
                Description = description,
                FilePath = filePath,
                UploadedBy = uploadedBy,
                CompanyId = company.ClientCompanyId
            };
            _context.FilesOnFileSystem.Add(fileModel);
            await _context.SaveChangesAsync();
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

        public async Task<bool> UploadCaseFile(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-file");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filePath = Path.Combine(path, Path.GetTempFileName());

            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                postedFile.CopyTo(stream);
            }

            var rows = _context.SaveChanges();
            using (var stream = postedFile.OpenReadStream())
            {
                using (var archive = new ZipArchive(stream))
                {
                    var csvFileCount = archive.Entries.Count(e => Path.GetExtension(e.FullName).Equals(".csv"));
                    var innerFile = archive.Entries.FirstOrDefault(e => Path.GetExtension(e.FullName).Equals(".csv"));
                    if (innerFile == null || csvFileCount != 1)
                    {
                        return false;
                    }
                    var processed = await ProcessCaseFile(userEmail, archive, autoOrManual, ORIGIN.FILE);
                    if (!processed)
                    {
                        return false;
                    }

                }
            }
            await SaveUpload(postedFile, filePath, "File upload", userEmail);
            return true;
        }

        private async Task<bool> ProcessCaseFile(string userEmail, ZipArchive archive, CREATEDBY autoOrManual, ORIGIN fileOrFtp)
        {

            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);
            bool userCanCreate = true;
            var totalClaimsCreated = await _context.CaseVerification.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
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

                return await DoCaseUpload(companyUser, dataRows, autoOrManual, archive, fileOrFtp);
            }
            return false;

        }
        private async Task<bool> DoCaseUpload(ClientCompanyApplicationUser companyUser, string[] dataRows, CREATEDBY autoOrManual, ZipArchive archive, ORIGIN fileOrFtp)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var autoEnabled = companyUser.ClientCompany.AutoAllocation;
            DataTable dt = new DataTable();
            bool firstRow = true;

            foreach (string row in dataRows)
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (firstRow)
                    {
                        foreach (string cell in row.Split(','))
                        {
                            dt.Columns.Add(cell.Trim());
                        }
                        firstRow = false;
                    }
                    else
                    {
                        try
                        {
                            dt.Rows.Add();
                            int i = 0;
                            var output = regex.Replace(row, m => m.Value.Replace(',', '@'));
                            var rowData = output.Split(',').ToList();
                            foreach (string cell in rowData)
                            {
                                dt.Rows[dt.Rows.Count - 1][i] = cell?.Trim() ?? NO_DATA;
                                i++;
                            }

                            var pinCode = _context.PinCode
                                .Include(p => p.District)
                                .Include(p => p.State)
                                .Include(p => p.Country)
                                .FirstOrDefault(p => p.Code == rowData[19].Trim());
                            if (pinCode.CountryId != companyUser.ClientCompany.CountryId)
                            {
                                continue;
                            }
                            //CREATE CLAIM
                            var subStatus = companyUser.ClientCompany.AutoAllocation && autoOrManual == CREATEDBY.AUTO ? createdStatus : assignedStatus;
                            var claim = new CaseVerification
                            {
                                InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                                InvestigationCaseStatus = status,
                                InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                                InvestigationCaseSubStatus = subStatus,
                                Updated = DateTime.Now,
                                UpdatedBy = companyUser.Email,
                                CurrentUserEmail = companyUser.Email,
                                CurrentClaimOwner = companyUser.Email,
                                Deleted = false,
                                HasClientCompany = true,
                                AssignedToAgency = false,
                                IsReady2Assign = true,
                                IsReviewCase = false,
                                UserEmailActioned = companyUser.Email,
                                UserEmailActionedTo = companyUser.Email,
                                CREATEDBY = autoOrManual,
                                ORIGIN = fileOrFtp,
                                ClientCompanyId = companyUser.ClientCompanyId,
                                UserRoleActionedTo = $"{companyUser.ClientCompany.Email}",
                                CreatorSla = companyUser.ClientCompany.CreatorSla
                            };

                            //CREATE POLICY
                            var servicetype = _context.InvestigationServiceType.FirstOrDefault(s => s.Code.ToLower() == (rowData[4].Trim().ToLower()));
                            
                            claim.PolicyDetail = new PolicyDetail
                            {
                                ContractNumber = rowData[0]?.Trim(),
                                SumAssuredValue = Convert.ToDecimal(rowData[1]?.Trim()),
                                ContractIssueDate = DateTime.Now.AddDays(-20),
                                ClaimType = (ClaimType)Enum.Parse(typeof(ClaimType), rowData[3]?.Trim()),
                                InsuranceType = InsuranceType.LIFE,
                                InvestigationServiceTypeId = servicetype?.InvestigationServiceTypeId,
                                DateOfIncident = DateTime.Now.AddDays(-5),
                                CauseOfLoss = rowData[6]?.Trim(),
                                CaseEnablerId = _context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == rowData[7].Trim().ToLower()).CaseEnablerId,
                                LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Code.ToLower() == "underwriting")?.LineOfBusinessId,
                            };

                            //CREATE CUSTOMER

                            var district = _context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);

                            var state = _context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);

                            var country = _context.Country.FirstOrDefault(c => c.CountryId == pinCode.Country.CountryId);

                            var customerImage = archive.Entries.FirstOrDefault(p => p.FullName.ToLower().EndsWith(rowData[0]?.Trim().ToLower() + "/customer.jpg"));
                            byte[] customerNewImage = null;
                            using (var cImage = customerImage.Open())
                            {
                                using (var cs = new MemoryStream())
                                {
                                    await cImage.CopyToAsync(cs);
                                    customerNewImage = cs.ToArray();
                                }
                            }
                            claim.CustomerDetail = new Claimant
                            {
                                Name = rowData[10]?.Trim(),
                                CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), rowData[11]?.Trim()),
                                Gender = (Gender)Enum.Parse(typeof(Gender), rowData[12]?.Trim()),
                                DateOfBirth = DateTime.Now.AddYears(-20),
                                ContactNumber = (rowData[14]?.Trim()),
                                Education = (Education)Enum.Parse(typeof(Education), rowData[15]?.Trim()),
                                Occupation = (Occupation)Enum.Parse(typeof(Occupation), rowData[16]?.Trim()),
                                Income = (Income)Enum.Parse(typeof(Income), rowData[17]?.Trim()),
                                Addressline = rowData[18]?.Trim(),
                                CountryId = country.CountryId,
                                PinCodeId = pinCode.PinCodeId,
                                StateId = state.StateId,
                                DistrictId = district.DistrictId,
                                Description = rowData[20]?.Trim(),
                                ProfilePicture = customerNewImage,
                            };

                            var address = claim.CustomerDetail.Addressline + ", " +
                                pinCode.District.Name + ", " +
                                pinCode.State.Name + ", " +
                                pinCode.Country.Code + ", " +
                                pinCode.Code;

                            var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                            claim.CustomerDetail.Latitude = coordinates.Latitude;
                            claim.CustomerDetail.Longitude = coordinates.Longitude;
                            var customerLatLong = claim.CustomerDetail.Latitude + "," + claim.CustomerDetail.Longitude;
                            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                            claim.CustomerDetail.CustomerLocationMap = url;

                            
                            var addedCase = _context.CaseVerification.Add(claim);

                            var log = new CaseVerificationTransaction
                            {
                                CaseVerification = addedCase.Entity,
                                UserEmailActioned = claim.UserEmailActioned,
                                UserRoleActionedTo = claim.UserRoleActionedTo,
                                CurrentClaimOwner = companyUser.Email,
                                HopCount = 0,
                                Time2Update = 0,
                                InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                                InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId,
                                UpdatedBy = companyUser.Email
                            };
                            _context.CaseVerificationTransaction.Add(log);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                            return false;
                        }
                    }
                }
            }
            try
            {
                return _context.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}