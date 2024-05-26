using Microsoft.CodeAnalysis;
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
        Task<bool> UploadFile(string userEmail, IFormFile postedFile, string uploadingway);

        Task<bool> DownloadFtpFile(string userEmail, IFormFile postedFile, string uploadingway);
    }

    public class FtpService : IFtpService
    {
        private static string NO_DATA = " NO - DATA ";
        private static Regex regex = new Regex("\\\"(.*?)\\\"");
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;

        private static WebClient client = new WebClient
        {
            Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA),
        };

        public FtpService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<bool> DownloadFtpFile(string userEmail, IFormFile postedFile, string uploadingway)
        {
            try
            {
                string folder = Path.Combine(webHostEnvironment.WebRootPath, "upload-ftp");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = Path.GetFileName(postedFile.FileName);
                string filePath = Path.Combine(folder, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                var response = client.UploadFile(Applicationsettings.FTP_SITE + fileName, filePath);

                var data = Encoding.UTF8.GetString(response);

                var processed =await DownloadFtp(userEmail, uploadingway);
                if(!processed)
                {
                    return false;
                }

                SaveUpload(postedFile, filePath, "Ftp download", userEmail);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        private async Task <bool> DownloadFtp(string userEmail, string uploadingway)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "download-ftp");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var files = GetFtpData();
            var zipFiles = files.Where(f => Path.GetExtension(f).Equals(".zip"));
            client.Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA);

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
                    var processed = await ProcessFile(userEmail, archive, uploadingway, ORIGIN.FTP);
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

        public async Task<bool> UploadFile(string userEmail, IFormFile postedFile, string uploadingway)
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
                    if(innerFile == null || csvFileCount != 1)
                    {
                        return false;
                    }
                    var processed = await ProcessFile(userEmail, archive, uploadingway, ORIGIN.FILE);
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

        private async Task<bool> ProcessFile(string userEmail, ZipArchive archive, string createdAs, ORIGIN uploadingway)
        {
            var createdAsMethod = (CREATEDBY)Enum.Parse(typeof(CREATEDBY), createdAs, true);

            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail);
            bool userCanCreate = true;
            var totalClaimsCreated = _context.ClaimsInvestigation.Include(c=>c.PolicyDetail).Where(c => !c.Deleted && c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
            if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
            {
                if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                }
            }

            if (!userCanCreate)
            {
                return userCanCreate;
            }

            var innerFile = archive.Entries.FirstOrDefault(e => Path.GetExtension(e.FullName).Equals(".csv"));
            using (var ss = innerFile.Open())
            {
                using (var memoryStream = new MemoryStream())
                {
                    ss.CopyTo(memoryStream);
                    var bytes = memoryStream.ToArray();
                    string csvData = Encoding.UTF8.GetString(bytes);
                    var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
                    var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name ==CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
                    var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
                    var autoEnabled = companyUser.ClientCompany.AutoAllocation;
                    DataTable dt = new DataTable();
                    bool firstRow = true;
                    var dataRows = csvData.Split('\n');

                    if(userCanCreate)
                    {
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
                                        var subStatus = companyUser.ClientCompany.AutoAllocation && createdAsMethod == CREATEDBY.AUTO ? createdStatus : assignedStatus;
                                        var claim = new ClaimsInvestigation
                                        {
                                            InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                                            InvestigationCaseStatus = status,
                                            InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                                            InvestigationCaseSubStatus = subStatus,
                                            Updated = DateTime.Now,
                                            UpdatedBy = userEmail,
                                            CurrentUserEmail = userEmail,
                                            CurrentClaimOwner = userEmail,
                                            Deleted = false,
                                            HasClientCompany = true,
                                            AssignedToAgency = false,
                                            IsReady2Assign = true,
                                            IsReviewCase = false,
                                            SelectedToAssign = false,
                                            UserEmailActioned = userEmail,
                                            UserEmailActionedTo = userEmail,
                                            CREATEDBY = createdAsMethod,
                                            ORIGIN = uploadingway,
                                            UserRoleActionedTo = $"{companyUser.ClientCompany.Email}"
                                    };

                                        var servicetype = _context.InvestigationServiceType.FirstOrDefault(s => s.Code.ToLower() == (rowData[4].Trim().ToLower()));

                                        var policyImage = archive.Entries.FirstOrDefault(p => p.FullName.ToLower().EndsWith(rowData[0]?.Trim().ToLower() + "/policy.jpg"));
                                        using (var pImage = policyImage.Open())
                                        {
                                            using (var ps = new MemoryStream())
                                            {
                                                pImage.CopyTo(ps);
                                                var savedNewImage = ps.ToArray();
                                                dt.Rows[dt.Rows.Count - 1][9] = $"{Convert.ToBase64String(savedNewImage)}";
                                                claim.PolicyDetail = new PolicyDetail
                                                {
                                                    ContractNumber = rowData[0]?.Trim(),
                                                    SumAssuredValue = Convert.ToDecimal(rowData[1]?.Trim()),
                                                    ContractIssueDate = DateTime.Now.AddDays(-20),
                                                    ClaimType = (ClaimType)Enum.Parse(typeof(ClaimType), rowData[3]?.Trim()),
                                                    InvestigationServiceTypeId = servicetype?.InvestigationServiceTypeId,
                                                    DateOfIncident = DateTime.Now.AddDays(-5),
                                                    CauseOfLoss = rowData[6]?.Trim(),
                                                    CaseEnablerId = _context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == rowData[7].Trim().ToLower()).CaseEnablerId,
                                                    CostCentreId = _context.CostCentre.FirstOrDefault(c => c.Code.ToLower() == rowData[8].Trim().ToLower()).CostCentreId,
                                                    LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Code.ToLower() == "claims")?.LineOfBusinessId,
                                                    ClientCompanyId = companyUser?.ClientCompanyId,
                                                    DocumentImage = savedNewImage,
                                                };
                                            }
                                        }

                                        var pinCode = _context.PinCode
                                            .Include(p => p.District)
                                            .Include(p => p.State)
                                            .Include(p => p.Country)
                                            .FirstOrDefault(p => p.Code == rowData[19].Trim());

                                        var district = _context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);

                                        var state = _context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);

                                        var country = _context.Country.FirstOrDefault(c => c.CountryId == pinCode.Country.CountryId);

                                        var customerImage = archive.Entries.FirstOrDefault(p => p.FullName.ToLower().EndsWith(rowData[0]?.Trim().ToLower() + "/customer.jpg"));

                                        using (var cImage = customerImage.Open())
                                        {
                                            using (var cs = new MemoryStream())
                                            {
                                                cImage.CopyTo(cs);
                                                var customerNewImage = cs.ToArray();
                                                dt.Rows[dt.Rows.Count - 1][21] = $"{Convert.ToBase64String(customerNewImage)}";
                                                claim.CustomerDetail = new CustomerDetail
                                                {
                                                    CustomerName = rowData[10]?.Trim(),
                                                    CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), rowData[11]?.Trim()),
                                                    Gender = (Gender)Enum.Parse(typeof(Gender), rowData[12]?.Trim()),
                                                    CustomerDateOfBirth = DateTime.Now.AddYears(-20),
                                                    ContactNumber = Convert.ToInt64(rowData[14]?.Trim()),
                                                    CustomerEducation = (Education)Enum.Parse(typeof(Education), rowData[15]?.Trim()),
                                                    CustomerOccupation = (Occupation)Enum.Parse(typeof(Occupation), rowData[16]?.Trim()),
                                                    CustomerIncome = (Income)Enum.Parse(typeof(Income), rowData[17]?.Trim()),
                                                    Addressline = rowData[18]?.Trim(),
                                                    CountryId = country.CountryId,
                                                    PinCodeId = pinCode.PinCodeId,
                                                    StateId = state.StateId,
                                                    DistrictId = district.DistrictId,
                                                    Description = rowData[20]?.Trim(),
                                                    ProfilePicture = customerNewImage,
                                                };
                                            }
                                        }

                                        claim.CustomerDetail.PinCode = pinCode;
                                        claim.CustomerDetail.PinCode.Latitude = pinCode.Latitude;
                                        claim.CustomerDetail.PinCode.Longitude = pinCode.Longitude;
                                        var customerLatLong = pinCode.Latitude + "," + pinCode.Longitude;

                                        var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
                                        claim.CustomerDetail.CustomerLocationMap = url;

                                        var benePinCode = _context.PinCode
                                            .Include(p => p.District)
                                            .Include(p => p.State)
                                            .Include(p => p.Country)
                                            .FirstOrDefault(p => p.Code == rowData[28].Trim());

                                        var beneDistrict = _context.District.FirstOrDefault(c => c.DistrictId == benePinCode.District.DistrictId);

                                        var beneState = _context.State.FirstOrDefault(s => s.StateId == benePinCode.State.StateId);

                                        var beneCountry = _context.Country.FirstOrDefault(c => c.CountryId == benePinCode.Country.CountryId);

                                        var relation = _context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == rowData[23].Trim().ToLower());

                                        var beneficiaryImage = archive.Entries.FirstOrDefault(p => p.FullName.ToLower().EndsWith(rowData[0]?.Trim().ToLower() + "/beneficiary.jpg"));

                                        using (var bImage = beneficiaryImage.Open())
                                        {
                                            using var bs = new MemoryStream();
                                            bImage.CopyTo(bs);

                                            var beneficiaryNewImage = bs.ToArray();

                                            dt.Rows[dt.Rows.Count - 1][29] = $"{Convert.ToBase64String(beneficiaryNewImage)}";

                                            var beneficairy = new BeneficiaryDetail
                                            {
                                                BeneficiaryName = rowData[22]?.Trim(),
                                                BeneficiaryRelationId = relation.BeneficiaryRelationId,
                                                BeneficiaryDateOfBirth = DateTime.Now.AddYears(-22),
                                                BeneficiaryIncome = (Income)Enum.Parse(typeof(Income), rowData[25]?.Trim()),
                                                BeneficiaryContactNumber = Convert.ToInt64(rowData[26]?.Trim()),
                                                Addressline = rowData[27]?.Trim(),
                                                PinCodeId = benePinCode.PinCodeId,
                                                DistrictId = beneDistrict.DistrictId,
                                                StateId = beneState.StateId,
                                                CountryId = beneCountry.CountryId,
                                                ProfilePicture = beneficiaryNewImage,
                                                Updated = DateTime.Now,
                                                UpdatedBy = userEmail,
                                                ClaimsInvestigation = claim
                                            };
                                            beneficairy.ClaimsInvestigationId = claim.ClaimsInvestigationId;

                                            beneficairy.PinCode = benePinCode;
                                            beneficairy.PinCode.Latitude = benePinCode.Latitude;
                                            beneficairy.PinCode.Longitude = benePinCode.Longitude;
                                            var beneLatLong = benePinCode.Latitude + "," + benePinCode.Longitude;

                                            var beneUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneLatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneLatLong}&key={Applicationsettings.GMAPData}";
                                            beneficairy.BeneficiaryLocationMap = beneUrl;
                                            _context.BeneficiaryDetail.Add(beneficairy);
                                        }
                                        _context.ClaimsInvestigation.Add(claim);

                                        var log = new InvestigationTransaction
                                        {
                                            ClaimsInvestigationId = claim.ClaimsInvestigationId,
                                            UserEmailActioned = claim.UserEmailActioned,
                                            UserRoleActionedTo = claim.UserRoleActionedTo,
                                            CurrentClaimOwner = userEmail,
                                            HopCount = 0,
                                            Time2Update = 0,
                                            InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                                            InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId,
                                            UpdatedBy = userEmail
                                        };
                                        _context.InvestigationTransaction.Add(log);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.StackTrace);
                                        throw ex;
                                    }
                                }
                            }
                        }
                        var dataObject = ConvertDataTable<UploadClaim>(dt);
                        _context.UploadClaim.AddRange(dataObject);
                        _context.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
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

        private static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }

        private static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name == column.ColumnName)
                        pro.SetValue(obj, dr[column.ColumnName], null);
                    else
                        continue;
                }
            }
            return obj;
        }
    }
}