using CsvHelper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

using static risk.control.system.Helpers.Permissions;
using System.Text;
using System.Net.Http;
using risk.control.system.Helpers;

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        private static readonly string[] firstNames = { "John", "Paul", "Ringo", "George", "Laura", "Stephaney" };
        private static readonly string[] lastNames = { "Lennon", "McCartney", "Starr", "Harrison", "Blanc" };

        private static string NO_DATA = " NO - DATA ";
        private static Regex regex = new Regex("\\\"(.*?)\\\"");
        private readonly ApplicationDbContext _context;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IToastNotification toastNotification;
        private static HttpClient httpClient = new();

        public ClaimsInvestigationController(ApplicationDbContext context,
            IClaimsInvestigationService claimsInvestigationService,
            IMailboxService mailboxService,
            UserManager<ClientCompanyApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            RoleManager<ApplicationRole> roleManager,
            IToastNotification toastNotification)
        {
            _context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
            this.roleManager = roleManager;
            this.toastNotification = toastNotification;
        }

        private String _ftpPath = "ftp://files.000webhost.com/public_html/";
        private String RemoteFileName = "text.txt";
        private String LocalDestinationFilename = "sample.txt";
        private String _login = "holosync";
        private String _password = "C0##ect10n";

        [HttpPost]
        [Breadcrumb(" FTP")]
        public async Task<IActionResult> FtpDownload()
        {
            try
            {
                string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string docPath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(docPath))
                {
                    Directory.CreateDirectory(docPath);
                }
                var files = GetFtpData();
                var zipFiles = files.Where(f => f.EndsWith(".zip"));
                var userEmail = HttpContext.User.Identity.Name;

                WebClient client = new WebClient();
                client.Credentials = new NetworkCredential(_login, _password);

                foreach (var zipFile in zipFiles)
                {
                    string fileName = zipFile;
                    string fileNameWithoutExtension = fileName.Substring(0, fileName.Length - 4);
                    string filePath = Path.Combine(path, fileName);

                    string ftpPath = $"ftp://files.000webhost.com/public_html/{zipFile}";

                    client.DownloadFile(
                        ftpPath, filePath);
                    using var archive = ZipFile.OpenRead(filePath);

                    ZipFile.ExtractToDirectory(filePath, docPath, true);

                    string zipFilePath = Path.Combine(docPath, fileNameWithoutExtension);
                    var dirNames = Directory.EnumerateDirectories(zipFilePath);
                    var fileNames = Directory.EnumerateFiles(zipFilePath);

                    string csvData = await System.IO.File.ReadAllTextAsync(fileNames.First());

                    var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
                    var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

                    var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                    DataTable dt = new DataTable();
                    bool firstRow = true;
                    foreach (string row in csvData.Split('\n'))
                    {
                        if (!string.IsNullOrEmpty(row))
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
                                        var claim = new ClaimsInvestigation
                                        {
                                            InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                                            InvestigationCaseStatus = status,
                                            InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                                            InvestigationCaseSubStatus = subStatus,
                                            Updated = DateTime.UtcNow,
                                            UpdatedBy = userEmail,
                                            CurrentUserEmail = userEmail,
                                            CurrentClaimOwner = userEmail,
                                            Created = DateTime.UtcNow,
                                            Deleted = false,
                                            HasClientCompany = true,
                                            IsReady2Assign = true,
                                            IsReviewCase = false,
                                            SelectedToAssign = false,
                                        };

                                        var servicetype = _context.InvestigationServiceType.FirstOrDefault(s => s.Code.ToLower() == (rowData[4].Trim().ToLower()));
                                        //var directoryName = dirNames.FirstOrDefault(d => d.EndsWith(rowData[0].Trim()));

                                        //var dname = directoryName.Substring(directoryName.LastIndexOf('\\') + 1);

                                        //var folders = Directory.GetFiles(Path.Combine(webHostEnvironment.WebRootPath, "upload-case", fileNameWithoutExtension, dname, "policy.jpg"));

                                        var policyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case", fileNameWithoutExtension, rowData[0].Trim(), "POLICY.jpg");
                                        //var policyImagePath = folders.FirstOrDefault(i => i.ToLower().EndsWith("policy.jpg"));

                                        var image = System.IO.File.ReadAllBytes(policyImagePath);
                                        dt.Rows[dt.Rows.Count - 1][9] = $"{Convert.ToBase64String(image)}";
                                        claim.PolicyDetail = new PolicyDetail
                                        {
                                            ContractNumber = rowData[0]?.Trim(),
                                            SumAssuredValue = Convert.ToDecimal(rowData[1]?.Trim()),
                                            ContractIssueDate = DateTime.UtcNow.AddDays(-20),
                                            ClaimType = (ClaimType)Enum.Parse(typeof(ClaimType), rowData[3]?.Trim()),
                                            InvestigationServiceTypeId = servicetype?.InvestigationServiceTypeId,
                                            DateOfIncident = DateTime.UtcNow.AddDays(-5),
                                            CauseOfLoss = rowData[6]?.Trim(),
                                            CaseEnablerId = _context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == rowData[7].Trim().ToLower()).CaseEnablerId,
                                            CostCentreId = _context.CostCentre.FirstOrDefault(c => c.Code.ToLower() == rowData[8].Trim().ToLower()).CostCentreId,
                                            LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Code.ToLower() == "claims")?.LineOfBusinessId,
                                            ClientCompanyId = companyUser?.ClientCompanyId,
                                            DocumentImage = image
                                        };

                                        var pinCode = _context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == rowData[19].Trim());

                                        var district = _context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);

                                        var state = _context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);

                                        var country = _context.Country.FirstOrDefault(c => c.Code.ToLower() == "IND".ToLower());

                                        var customerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case", fileNameWithoutExtension, rowData[0].Trim(), "CUSTOMER.jpg");

                                        var customerImage = System.IO.File.ReadAllBytes(customerImagePath);

                                        dt.Rows[dt.Rows.Count - 1][21] = $"{Convert.ToBase64String(customerImage)}";

                                        claim.CustomerDetail = new CustomerDetail
                                        {
                                            CustomerName = rowData[10]?.Trim(),
                                            CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), rowData[11]?.Trim()),
                                            Gender = (Gender)Enum.Parse(typeof(Gender), rowData[12]?.Trim()),
                                            CustomerDateOfBirth = DateTime.UtcNow.AddYears(-20),
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
                                            ProfilePicture = customerImage
                                        };

                                        var benePinCode = _context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == rowData[28].Trim());

                                        var beneDistrict = _context.District.FirstOrDefault(c => c.DistrictId == benePinCode.District.DistrictId);

                                        var beneState = _context.State.FirstOrDefault(s => s.StateId == benePinCode.State.StateId);
                                        var relation = _context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == rowData[23].Trim().ToLower());

                                        var beneficairyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case", fileNameWithoutExtension, rowData[0].Trim(), "BENEFICIARY.jpg");

                                        var beneficairyImage = System.IO.File.ReadAllBytes(beneficairyImagePath);
                                        dt.Rows[dt.Rows.Count - 1][29] = $"{Convert.ToBase64String(beneficairyImage)}";

                                        var beneficairy = new CaseLocation
                                        {
                                            BeneficiaryName = rowData[22]?.Trim(),
                                            BeneficiaryRelationId = relation.BeneficiaryRelationId,
                                            BeneficiaryDateOfBirth = DateTime.UtcNow.AddYears(-22),
                                            BeneficiaryIncome = (Income)Enum.Parse(typeof(Income), rowData[25]?.Trim()),
                                            BeneficiaryContactNumber = Convert.ToInt64(rowData[26]?.Trim()),
                                            Addressline = rowData[27]?.Trim(),
                                            PinCodeId = benePinCode.PinCodeId,
                                            DistrictId = beneDistrict.DistrictId,
                                            StateId = beneState.StateId,
                                            CountryId = country.CountryId,
                                            InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                                            ProfilePicture = beneficairyImage
                                        };

                                        var addedClaim = _context.ClaimsInvestigation.Add(claim);

                                        beneficairy.ClaimsInvestigationId = addedClaim.Entity.ClaimsInvestigationId;

                                        _context.CaseLocation.Add(beneficairy);

                                        var log = new InvestigationTransaction
                                        {
                                            ClaimsInvestigationId = addedClaim.Entity.ClaimsInvestigationId,
                                            CurrentClaimOwner = userEmail,
                                            Created = DateTime.UtcNow,
                                            HopCount = 0,
                                            Time2Update = 0,
                                            InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId,
                                            InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId,
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
                    }
                    var dataObject = ConvertDataTable<UploadClaim>(dt);
                    _context.UploadClaim.AddRange(dataObject);

                    //await SaveTheClaims(dataObject);
                    var fileModel = new FileOnFileSystemModel
                    {
                        CreatedOn = DateTime.UtcNow,
                        FileType = "application/x-zip-compressed",
                        Extension = "zip",
                        Name = fileName,
                        Description = "Ftp Download",
                        FilePath = filePath,
                        UploadedBy = userEmail
                    };
                    _context.FilesOnFileSystem.Add(fileModel);
                }

                var rows = _context.SaveChanges();

                foreach (var zipFile in zipFiles)
                {
                    string fileName = zipFile;

                    FtpWebRequest requestFileDelete = (FtpWebRequest)WebRequest.Create($"{_ftpPath}" + fileName);
                    requestFileDelete.Credentials = new NetworkCredential(_login, _password);
                    requestFileDelete.Method = WebRequestMethods.Ftp.DeleteFile;

                    FtpWebResponse responseFileDelete = (FtpWebResponse)requestFileDelete.GetResponse();
                }

                toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Ftp Downloaded Claims ready"));

                return RedirectToAction("Draft");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        private byte[] DownloadData(string path)
        {
            var request = new WebClient();
            request.Credentials = new NetworkCredential(_login, _password);

            //This will download the file as byte array. Easy for you to convert it to any type.
            return request.DownloadData(path);
        }

        private List<string> GetFtpData()
        {
            var request = WebRequest.Create(_ftpPath);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(_login, _password);

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

        [Breadcrumb(" Upload New")]
        [AllowAnonymous]
        public IActionResult UploadClaims()
        {
            DataTable dt = new DataTable();

            return View(dt);
        }

        [HttpPost]
        public async Task<IActionResult> UploadClaims(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string docPath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(docPath))
                {
                    Directory.CreateDirectory(docPath);
                }
                string fileName = Path.GetFileName(postedFile.FileName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(postedFile.FileName);
                string filePath = Path.Combine(path, fileName);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                using var archive = ZipFile.OpenRead(filePath);

                ZipFile.ExtractToDirectory(filePath, docPath, true);

                string zipFilePath = Path.Combine(docPath, fileNameWithoutExtension);
                var dirNames = Directory.EnumerateDirectories(zipFilePath);
                var fileNames = Directory.EnumerateFiles(zipFilePath);

                string csvData = await System.IO.File.ReadAllTextAsync(fileNames.First());

                var userEmail = HttpContext.User.Identity.Name;

                var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
                var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                DataTable dt = new DataTable();
                bool firstRow = true;
                foreach (string row in csvData.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(row))
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
                                    var claim = new ClaimsInvestigation
                                    {
                                        InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                                        InvestigationCaseStatus = status,
                                        InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                                        InvestigationCaseSubStatus = subStatus,
                                        Updated = DateTime.UtcNow,
                                        UpdatedBy = userEmail,
                                        CurrentUserEmail = userEmail,
                                        CurrentClaimOwner = userEmail,
                                        Created = DateTime.UtcNow,
                                        Deleted = false,
                                        HasClientCompany = true,
                                        IsReady2Assign = true,
                                        IsReviewCase = false,
                                        SelectedToAssign = false,
                                    };

                                    var servicetype = _context.InvestigationServiceType.FirstOrDefault(s => s.Code.ToLower() == (rowData[4].Trim().ToLower()));
                                    //var directoryName = dirNames.FirstOrDefault(d => d.EndsWith(rowData[0].Trim()));

                                    //var dname = directoryName.Substring(directoryName.LastIndexOf('\\') + 1);

                                    //var folders = Directory.GetFiles(Path.Combine(webHostEnvironment.WebRootPath, "upload-case", fileNameWithoutExtension, dname, "policy.jpg"));

                                    var policyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case", fileNameWithoutExtension, rowData[0].Trim(), "POLICY.jpg");
                                    //var policyImagePath = folders.FirstOrDefault(i => i.ToLower().EndsWith("policy.jpg"));

                                    var image = System.IO.File.ReadAllBytes(policyImagePath);
                                    dt.Rows[dt.Rows.Count - 1][9] = $"{Convert.ToBase64String(image)}";
                                    claim.PolicyDetail = new PolicyDetail
                                    {
                                        ContractNumber = rowData[0]?.Trim(),
                                        SumAssuredValue = Convert.ToDecimal(rowData[1]?.Trim()),
                                        ContractIssueDate = DateTime.UtcNow.AddDays(-20),
                                        ClaimType = (ClaimType)Enum.Parse(typeof(ClaimType), rowData[3]?.Trim()),
                                        InvestigationServiceTypeId = servicetype?.InvestigationServiceTypeId,
                                        DateOfIncident = DateTime.UtcNow.AddDays(-5),
                                        CauseOfLoss = rowData[6]?.Trim(),
                                        CaseEnablerId = _context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == rowData[7].Trim().ToLower()).CaseEnablerId,
                                        CostCentreId = _context.CostCentre.FirstOrDefault(c => c.Code.ToLower() == rowData[8].Trim().ToLower()).CostCentreId,
                                        LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Code.ToLower() == "claims")?.LineOfBusinessId,
                                        ClientCompanyId = companyUser?.ClientCompanyId,
                                        DocumentImage = image
                                    };

                                    var pinCode = _context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == rowData[19].Trim());

                                    var district = _context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);

                                    var state = _context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);

                                    var country = _context.Country.FirstOrDefault(c => c.Code.ToLower() == "IND".ToLower());

                                    var customerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case", fileNameWithoutExtension, rowData[0].Trim(), "CUSTOMER.jpg");

                                    var customerImage = System.IO.File.ReadAllBytes(customerImagePath);

                                    dt.Rows[dt.Rows.Count - 1][21] = $"{Convert.ToBase64String(customerImage)}";

                                    claim.CustomerDetail = new CustomerDetail
                                    {
                                        CustomerName = rowData[10]?.Trim(),
                                        CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), rowData[11]?.Trim()),
                                        Gender = (Gender)Enum.Parse(typeof(Gender), rowData[12]?.Trim()),
                                        CustomerDateOfBirth = DateTime.UtcNow.AddYears(-20),
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
                                        ProfilePicture = customerImage
                                    };

                                    var benePinCode = _context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == rowData[28].Trim());

                                    var beneDistrict = _context.District.FirstOrDefault(c => c.DistrictId == benePinCode.District.DistrictId);

                                    var beneState = _context.State.FirstOrDefault(s => s.StateId == benePinCode.State.StateId);
                                    var relation = _context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == rowData[23].Trim().ToLower());

                                    var beneficairyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case", fileNameWithoutExtension, rowData[0].Trim(), "BENEFICIARY.jpg");

                                    var beneficairyImage = System.IO.File.ReadAllBytes(beneficairyImagePath);
                                    dt.Rows[dt.Rows.Count - 1][29] = $"{Convert.ToBase64String(beneficairyImage)}";

                                    var beneficairy = new CaseLocation
                                    {
                                        BeneficiaryName = rowData[22]?.Trim(),
                                        BeneficiaryRelationId = relation.BeneficiaryRelationId,
                                        BeneficiaryDateOfBirth = DateTime.UtcNow.AddYears(-22),
                                        BeneficiaryIncome = (Income)Enum.Parse(typeof(Income), rowData[25]?.Trim()),
                                        BeneficiaryContactNumber = Convert.ToInt64(rowData[26]?.Trim()),
                                        Addressline = rowData[27]?.Trim(),
                                        PinCodeId = benePinCode.PinCodeId,
                                        DistrictId = beneDistrict.DistrictId,
                                        StateId = beneState.StateId,
                                        CountryId = country.CountryId,
                                        InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                                        ProfilePicture = beneficairyImage
                                    };

                                    var addedClaim = _context.ClaimsInvestigation.Add(claim);

                                    beneficairy.ClaimsInvestigationId = addedClaim.Entity.ClaimsInvestigationId;

                                    _context.CaseLocation.Add(beneficairy);

                                    var log = new InvestigationTransaction
                                    {
                                        ClaimsInvestigationId = addedClaim.Entity.ClaimsInvestigationId,
                                        CurrentClaimOwner = userEmail,
                                        Created = DateTime.UtcNow,
                                        HopCount = 0,
                                        Time2Update = 0,
                                        InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId,
                                        InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId,
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
                }
                var dataObject = ConvertDataTable<UploadClaim>(dt);
                _context.UploadClaim.AddRange(dataObject);

                //await SaveTheClaims(dataObject);
                await SaveUpload(postedFile, filePath, "File upload", userEmail);
                try
                {
                    var rows = _context.SaveChanges();
                    toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> File uploaded Claims ready"));

                    return RedirectToAction("Draft");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
            }
            return Problem();
        }

        private async Task SaveTheClaims(List<UploadClaim> claims)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            foreach (var uclaim in claims)
            {
                var claim = new ClaimsInvestigation
                {
                    InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                    InvestigationCaseStatus = status,
                    InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                    InvestigationCaseSubStatus = subStatus,
                    Updated = DateTime.UtcNow,
                    UpdatedBy = userEmail,
                    CurrentUserEmail = userEmail,
                    CurrentClaimOwner = userEmail,
                    Created = DateTime.UtcNow,
                    Deleted = false,
                    HasClientCompany = true,
                    IsReady2Assign = false,
                    IsReviewCase = false,
                    SelectedToAssign = false,
                };
                var servicetype = _context.InvestigationServiceType.FirstOrDefault();

                claim.PolicyDetail = new PolicyDetail
                {
                    ContractNumber = uclaim.Policy,
                    SumAssuredValue = Convert.ToDecimal(uclaim.Amount),
                    ContractIssueDate = DateTime.UtcNow.AddDays(-10),
                    ClaimType = (ClaimType)Enum.Parse(typeof(ClaimType), uclaim.ClaimType),
                    InvestigationServiceTypeId = servicetype?.InvestigationServiceTypeId,
                    DateOfIncident = DateTime.UtcNow.AddDays(-5),
                    CauseOfLoss = uclaim.CauseOfLoss,
                    CaseEnablerId = _context.CaseEnabler.FirstOrDefault().CaseEnablerId,
                    CostCentreId = _context.CostCentre.FirstOrDefault().CostCentreId,
                    LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Code.ToLower() == "claims")?.LineOfBusinessId,
                    ClientCompanyId = companyUser?.ClientCompanyId,
                    //DocumentImage = System.IO.File.ReadAllBytes(uclaim.PDocument)
                };

                var pinCode = _context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault();

                var district = _context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);

                var state = _context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);

                var country = _context.Country.FirstOrDefault(c => c.Code.ToLower() == "IND".ToLower());

                claim.CustomerDetail = new CustomerDetail
                {
                    CustomerName = uclaim.Name,
                    CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), uclaim.CustomerType?.Trim()),
                    Gender = (Gender)Enum.Parse(typeof(Gender), uclaim.Gender?.Trim()),
                    CustomerDateOfBirth = DateTime.UtcNow.AddYears(-23),
                    ContactNumber = Convert.ToInt64(uclaim.Contact?.Trim()),
                    CustomerEducation = (Education)Enum.Parse(typeof(Education), uclaim.Education?.Trim()),
                    CustomerOccupation = (Occupation)Enum.Parse(typeof(Occupation), uclaim.Occupation?.Trim()),
                    CustomerIncome = (Income)Enum.Parse(typeof(Income), uclaim.Occupation?.Trim()),
                    Addressline = uclaim.CAddress?.Trim(),
                    CountryId = country.CountryId,
                    PinCodeId = pinCode.PinCodeId,
                    StateId = state.StateId,
                    DistrictId = district.DistrictId,
                    Description = uclaim.Comment,
                    //ProfilePicture = System.IO.File.ReadAllBytes(uclaim.CPhoto)
                };

                var benePinCode = _context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault();

                var beneDistrict = _context.District.FirstOrDefault(c => c.DistrictId == benePinCode.District.DistrictId);

                var beneState = _context.State.FirstOrDefault(s => s.StateId == benePinCode.State.StateId);
                var relation = _context.BeneficiaryRelation.FirstOrDefault();

                var beneficairy = new CaseLocation
                {
                    BeneficiaryName = uclaim.BeneficiaryName,
                    BeneficiaryRelationId = relation.BeneficiaryRelationId,
                    BeneficiaryDateOfBirth = DateTime.UtcNow.AddYears(-33),
                    BeneficiaryIncome = (Income)Enum.Parse(typeof(Income), uclaim.BeneficiaryIncome?.Trim()),
                    BeneficiaryContactNumber = Convert.ToInt64(uclaim.BeneficiaryContact?.Trim()),
                    Addressline = uclaim.BAddress,
                    PinCodeId = benePinCode.PinCodeId,
                    DistrictId = beneDistrict.DistrictId,
                    StateId = beneState.StateId,
                    CountryId = country.CountryId,
                    InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId
                    //ProfilePicture = System.IO.File.ReadAllBytes(uclaim.BPhoto)
                };

                var addedClaim = _context.ClaimsInvestigation.Add(claim);

                beneficairy.ClaimsInvestigationId = addedClaim.Entity.ClaimsInvestigationId;

                _context.CaseLocation.Add(beneficairy);

                var log = new InvestigationTransaction
                {
                    ClaimsInvestigationId = addedClaim.Entity.ClaimsInvestigationId,
                    CurrentClaimOwner = userEmail,
                    Created = DateTime.UtcNow,
                    HopCount = 0,
                    Time2Update = 0,
                    InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail
                };
                _context.InvestigationTransaction.Add(log);
                var rows = _context.SaveChanges();
            }
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

        [HttpPost]
        public async Task<IActionResult> SaveUploadedClaims(DataTable td, string filePath)
        {
            string docPath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
            string fileNameWithoutExtension = filePath;

            string zipFilePath = Path.Combine(docPath, fileNameWithoutExtension);
            var dirNames = Directory.EnumerateDirectories(zipFilePath).ToList();
            var fileNames = Directory.EnumerateFiles(zipFilePath);

            string csvData = await System.IO.File.ReadAllTextAsync(fileNames.First());

            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            DataTable dt = new DataTable();
            bool firstRow = true;
            foreach (string row in csvData.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
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
                                var claim = new ClaimsInvestigation { };
                                claim.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
                                claim.InvestigationCaseStatus = status;
                                claim.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
                                claim.InvestigationCaseSubStatus = subStatus;
                                claim.Updated = DateTime.UtcNow;
                                claim.UpdatedBy = userEmail;
                                claim.CurrentUserEmail = userEmail;
                                claim.CurrentClaimOwner = userEmail;

                                var servicetype = _context.InvestigationServiceType.FirstOrDefault(s => s.Code.ToLower() == (rowData[4].Trim().ToLower()));
                                var directoryName = dirNames.FirstOrDefault(d => d.EndsWith(rowData[0].Trim()));
                                DirectoryInfo dir = new DirectoryInfo($"{directoryName}");
                                FileInfo[] imageFiles = dir.GetFiles("*.jpg").Union(dir.GetFiles("*.jpeg")).ToArray();

                                var policyImagePath = imageFiles.FirstOrDefault(i => i.Name.ToLower() == "policy.jpg" || i.Name.ToLower() == "policy.jpeg")?.FullName;

                                var image = System.IO.File.ReadAllBytes(policyImagePath);
                                dt.Rows[dt.Rows.Count - 1][9] = $"{Convert.ToBase64String(image)}";
                                claim.PolicyDetail = new PolicyDetail
                                {
                                    ContractNumber = rowData[0].Trim(),
                                    SumAssuredValue = Convert.ToDecimal(rowData[1].Trim()),
                                    ContractIssueDate = DateTime.Parse(rowData[2].Trim()),
                                    ClaimType = (ClaimType)Enum.Parse(typeof(ClaimType), rowData[3].Trim()),
                                    InvestigationServiceTypeId = servicetype?.InvestigationServiceTypeId,
                                    DateOfIncident = DateTime.Parse(rowData[5].Trim()),
                                    CauseOfLoss = rowData[6].Trim(),
                                    CaseEnablerId = _context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == rowData[7].Trim().ToLower()).CaseEnablerId,
                                    CostCentreId = _context.CostCentre.FirstOrDefault(c => c.Code.ToLower() == rowData[8].Trim().ToLower()).CostCentreId,
                                    LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Code.ToLower() == "claims")?.LineOfBusinessId,
                                    ClientCompanyId = companyUser?.ClientCompanyId,
                                    DocumentImage = image
                                };

                                var pinCode = _context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == rowData[19].Trim());

                                var district = _context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);

                                var state = _context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);

                                var country = _context.Country.FirstOrDefault(c => c.Code.ToLower() == "IND".ToLower());

                                var customerImagePath = imageFiles.FirstOrDefault(i => i.Name.ToLower() == "customer.jpg" || i.Name.ToLower() == "customer.jpeg")?.FullName;

                                var customerImage = System.IO.File.ReadAllBytes(customerImagePath);
                                dt.Rows[dt.Rows.Count - 1][21] = $"{Convert.ToBase64String(customerImage)}";

                                claim.CustomerDetail = new CustomerDetail
                                {
                                    CustomerName = rowData[10].Trim(),
                                    CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), rowData[11].Trim()),
                                    Gender = (Gender)Enum.Parse(typeof(Gender), rowData[12].Trim()),
                                    CustomerDateOfBirth = DateTime.Parse(rowData[13].Trim()),
                                    ContactNumber = Convert.ToInt64(rowData[14].Trim()),
                                    CustomerEducation = (Education)Enum.Parse(typeof(Education), rowData[15].Trim()),
                                    CustomerOccupation = (Occupation)Enum.Parse(typeof(Occupation), rowData[16].Trim()),
                                    CustomerIncome = (Income)Enum.Parse(typeof(Income), rowData[17].Trim()),
                                    Addressline = rowData[18].Trim(),
                                    CountryId = country.CountryId,
                                    PinCodeId = pinCode.PinCodeId,
                                    StateId = state.StateId,
                                    DistrictId = district.DistrictId,
                                    Description = rowData[20].Trim(),
                                    ProfilePicture = customerImage
                                };

                                var benePinCode = _context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == rowData[28].Trim());

                                var beneDistrict = _context.District.FirstOrDefault(c => c.DistrictId == benePinCode.District.DistrictId);

                                var beneState = _context.State.FirstOrDefault(s => s.StateId == benePinCode.State.StateId);
                                var relation = _context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == rowData[23].Trim().ToLower());

                                var beneficairyImagePath = imageFiles.FirstOrDefault(i => i.Name.ToLower() == "beneficiary.jpg" || i.Name.ToLower() == "beneficiary.jpeg")?.FullName;

                                var beneficairyImage = System.IO.File.ReadAllBytes(beneficairyImagePath);
                                dt.Rows[dt.Rows.Count - 1][29] = $"{Convert.ToBase64String(beneficairyImage)}";

                                var beneficairy = new CaseLocation
                                {
                                    BeneficiaryName = rowData[22].Trim(),
                                    BeneficiaryRelationId = relation.BeneficiaryRelationId,
                                    BeneficiaryDateOfBirth = DateTime.Parse(rowData[24].Trim()),
                                    BeneficiaryIncome = (Income)Enum.Parse(typeof(Income), rowData[25].Trim()),
                                    BeneficiaryContactNumber = Convert.ToInt64(rowData[26].Trim()),
                                    Addressline = rowData[27].Trim(),
                                    PinCodeId = benePinCode.PinCodeId,
                                    DistrictId = beneDistrict.DistrictId,
                                    StateId = beneState.StateId,
                                    CountryId = country.CountryId,
                                    InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                                    ProfilePicture = beneficairyImage
                                };

                                var addedClaim = _context.ClaimsInvestigation.Add(claim);

                                beneficairy.ClaimsInvestigationId = addedClaim.Entity.ClaimsInvestigationId;

                                _context.CaseLocation.Add(beneficairy);

                                var log = new InvestigationTransaction
                                {
                                    ClaimsInvestigationId = addedClaim.Entity.ClaimsInvestigationId,
                                    CurrentClaimOwner = userEmail,
                                    Created = DateTime.UtcNow,
                                    HopCount = 0,
                                    Time2Update = 0,
                                    InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId,
                                    InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId,
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
            }

            var rows = _context.SaveChanges();
            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i>Uploaded Claims saved as Draft"));

            return RedirectToAction("Draft");
        }

        private async Task SaveUpload(IFormFile file, string filePath, string description, string uploadedBy)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var fileModel = new FileOnFileSystemModel
            {
                CreatedOn = DateTime.UtcNow,
                FileType = file.ContentType,
                Extension = extension,
                Name = fileName,
                Description = description,
                FilePath = filePath,
                UploadedBy = uploadedBy
            };
            _context.FilesOnFileSystem.Add(fileModel);
            _context.SaveChanges();
        }

        [Breadcrumb(" Add New")]
        public async Task<IActionResult> CreateClaim()
        {
            var claim = new ClaimsInvestigation { PolicyDetail = new PolicyDetail { LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId } };

            var model = new ClaimTransactionModel
            {
                Claim = claim,
                Log = null,
                Location = new CaseLocation { }
            };

            return View(model);
        }

        [Breadcrumb(" Claims")]
        public IActionResult Index()
        {
            var claimsPage = new MvcBreadcrumbNode("Active", "Claims", "Claims");
            ViewData["BreadcrumbNode"] = claimsPage;
            return View();
        }

        [Breadcrumb(" Assign", FromAction = "Index")]
        public IActionResult Assign()
        {
            return View();
        }

        [Breadcrumb(" Assess", FromAction = "Index")]
        public async Task<IActionResult> Assessor()
        {
            return View();
        }

        [Breadcrumb(" Allocate", FromAction = "Index")]
        public IActionResult Assigner()
        {
            return View();
        }

        // GET: ClaimsInvestigation

        [Breadcrumb(" Assign", FromAction = "Index")]
        public IActionResult Draft()
        {
            return View();
        }

        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction = "Assigner")]
        public async Task<IActionResult> EmpanelledVendors(string selectedcase)
        {
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Assigner));
            }

            if (_context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLocations = claimsInvestigation.CaseLocations.Where(c => string.IsNullOrWhiteSpace(c.VendorId)
            && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId).ToList();

            claimsInvestigation.CaseLocations = caseLocations;

            var location = claimsInvestigation.CaseLocations.FirstOrDefault()?.CaseLocationId;

            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.CaseLocationId == location
                //&& c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId
                );

            var existingVendors = await _context.Vendor
                .Where(c => c.ClientCompanyId == claimCase.ClaimsInvestigation.PolicyDetail.ClientCompanyId)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .ToListAsync();

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var claimsCases = _context.ClaimsInvestigation
                .Include(c => c.Vendors)
                .Include(c => c.CaseLocations.Where(c =>
                !string.IsNullOrWhiteSpace(c.VendorId) &&
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
                ));

            var vendorCaseCount = new Dictionary<string, int>();

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.CaseLocations.Count > 0)
                {
                    foreach (var CaseLocation in claimsCase.CaseLocations)
                    {
                        if (!string.IsNullOrEmpty(CaseLocation.VendorId))
                        {
                            if (CaseLocation.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    CaseLocation.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    CaseLocation.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
                                    )
                            {
                                if (!vendorCaseCount.TryGetValue(CaseLocation.VendorId, out countOfCases))
                                {
                                    vendorCaseCount.Add(CaseLocation.VendorId, 1);
                                }
                                else
                                {
                                    int currentCount = vendorCaseCount[CaseLocation.VendorId];
                                    ++currentCount;
                                    vendorCaseCount[CaseLocation.VendorId] = currentCount;
                                }
                            }
                        }
                    }
                }
            }

            List<VendorCaseModel> vendorWithCaseCounts = new();

            foreach (var existingVendor in existingVendors)
            {
                var vendorCase = vendorCaseCount.FirstOrDefault(v => v.Key == existingVendor.VendorId);
                if (vendorCase.Key == existingVendor.VendorId)
                {
                    vendorWithCaseCounts.Add(new VendorCaseModel
                    {
                        CaseCount = vendorCase.Value,
                        Vendor = existingVendor,
                    });
                }
                else
                {
                    vendorWithCaseCounts.Add(new VendorCaseModel
                    {
                        CaseCount = 0,
                        Vendor = existingVendor,
                    });
                }
            }

            ViewBag.CompanyId = claimCase.ClaimsInvestigation.PolicyDetail.ClientCompanyId;

            ViewBag.Selectedcase = selectedcase;

            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = url;

            var beneficiarylatLong = claimCase.PinCode.Latitude + "," + claimCase.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;

            return View(new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, Vendors = vendorWithCaseCounts, ClaimsInvestigation = claimsInvestigation });
        }

        [HttpGet]
        [Breadcrumb(" Allocate (to agency)")]
        public async Task<IActionResult> AllocateToVendor(string selectedcase)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Assigner));
            }

            if (_context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var caseLocations = claimsInvestigation.CaseLocations.Where(c => string.IsNullOrWhiteSpace(c.VendorId)
            && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId).ToList();

            claimsInvestigation.CaseLocations = caseLocations;
            return View(claimsInvestigation);
        }

        [HttpGet]
        [Breadcrumb(" Re-allocate to agency")]
        public async Task<IActionResult> ReAllocateToVendor(string selectedcase)
        {
            if (_context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var caseLocations = claimsInvestigation.CaseLocations.Where(c => !string.IsNullOrWhiteSpace(c.VendorId)
            && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId).ToList();

            claimsInvestigation.CaseLocations = caseLocations;
            return View(claimsInvestigation);
        }

        [HttpPost]
        public async Task<IActionResult> CaseAllocatedToVendor(string selectedcase, string claimId, long caseLocationId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            var policy = await claimsInvestigationService.AllocateToVendor(userEmail, claimId, selectedcase, caseLocationId);

            await mailboxService.NotifyClaimAllocationToVendor(userEmail, policy.PolicyDetail.ContractNumber, claimId, selectedcase, caseLocationId);

            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == selectedcase);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # {0}] submitted to Agency {1} !", policy.PolicyDetail.ContractNumber, vendor.Name));

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var isCreator = await userManager.IsInRoleAsync(companyUser, AppRoles.Creator.ToString());

            //if(isCreator)
            //{
            //    return RedirectToAction(nameof(ClaimsInvestigationController.Draft), "ClaimsInvestigation");
            //}
            return RedirectToAction(nameof(ClaimsInvestigationController.Assigner), "ClaimsInvestigation");
        }

        [Breadcrumb(" Case-locations")]
        public IActionResult CaseLocation(string id)
        {
            if (id == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }

            var applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefault(a => a.ClaimsInvestigationId == id);

            return View(applicationDbContext);
        }

        [Breadcrumb(" Assessed")]
        public async Task<IActionResult> Approved()
        {
            return View();
        }

        [Breadcrumb(" Rejected", FromAction = "Index")]
        public async Task<IActionResult> Reject()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State);

            ViewBag.HasClientCompany = true;
            ViewBag.HasVendorCompany = true;

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var assessorApprovedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                ViewBag.HasClientCompany = false;
                ViewBag.HasVendorCompany = false;
                applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
                ViewBag.HasVendorCompany = false;
            }

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            if (userRole.Value.Contains(AppRoles.Assessor.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null));

                var claimsSubmitted = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.InvestigationCaseSubStatusId == assessorApprovedStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsSubmitted.Add(item);
                    }
                }
                return View(claimsSubmitted);
            }
            return Problem();
        }

        [Breadcrumb(" Re Allocate", FromAction = "Index")]
        public async Task<IActionResult> Review()
        {
            return View();
        }

        [Breadcrumb(title: "Active")]
        public IActionResult Active()
        {
            return View();
        }

        [Breadcrumb(title: "Withdraw", FromAction = "Index")]
        public async Task<IActionResult> ToInvestigate()
        {
            return View();
        }

        [Breadcrumb(title: "Report", FromAction = "Assessor")]
        public async Task<IActionResult> GetInvestigateReport(string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId
            );

            if (claimCase.ClaimReport.LocationLongLat != null)
            {
                var longLat = claimCase.ClaimReport.LocationLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.LocationLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.LocationLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.LocationUrl = url;
                RootObject rootObject = getAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                else
                {
                    registeredLatitude = Convert.ToDouble(claimCase.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimCase.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;
                if (distance > 0)
                {
                    address = address + $" \n\r The location is {distance} km away from expected address";
                }
                ViewBag.LocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = getAddress("-37.839542", "145.164834");
                ViewBag.LocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.LocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }
            if (claimCase.ClaimReport.OcrLongLat != null)
            {
                var longLat = claimCase.ClaimReport.OcrLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.OcrLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.OcrLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.OcrLocationUrl = url;
                RootObject rootObject = getAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;
                if (distance > 0)
                {
                    address = address + $" \n\r The location is {distance} km away from expected address";
                }
                ViewBag.OcrLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = getAddress("-37.839542", "145.164834");
                ViewBag.OcrLocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.OcrLocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }

            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var curl = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = curl;

            var beneficiarylatLong = claimCase.PinCode.Latitude + "," + claimCase.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;

            return View(new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, ClaimsInvestigation = claimsInvestigation });
        }

        public static RootObject getAddress(string lat, string lon)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            webClient.Headers.Add("Referer", "http://www.microsoft.com");
            var jsonData = webClient.DownloadData("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(RootObject));
            RootObject rootObject = (RootObject)ser.ReadObject(new MemoryStream(jsonData));
            return rootObject;
        }

        [Breadcrumb(title: "Report", FromAction = "Approved")]
        public async Task<IActionResult> GetApprovedReport(string selectedcase)
        {
            if (selectedcase == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == selectedcase)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.ClientCompany)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CaseEnabler)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CostCentre)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.InvestigationCaseSubStatus)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.BeneficiaryRelation)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.Country)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.District)
              .Include(c => c.InvestigationCaseStatus)
              .Include(c => c.InvestigationCaseSubStatus)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.InvestigationServiceType)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.LineOfBusiness)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

            var location = await _context.CaseLocation
                .Include(l => l.ClaimReport)
                .Include(l => l.Vendor)
                .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == selectedcase);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            if (location.ClaimReport.LocationLongLat != null)
            {
                var longLat = location.ClaimReport.LocationLongLat.IndexOf("/");
                var latitude = location.ClaimReport.LocationLongLat.Substring(0, longLat)?.Trim();
                var longitude = location.ClaimReport.LocationLongLat.Substring(longLat + 1)?.Trim().Replace("/", "");
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.LocationUrl = url;
                RootObject rootObject = getAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;
                if (distance > 0)
                {
                    address = address + $" \n\r The location is {distance} km away from expected address";
                }
                ViewBag.LocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = getAddress("-37.839542", "145.164834");
                ViewBag.LocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.LocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }
            if (location.ClaimReport.OcrLongLat != null)
            {
                var longLat = location.ClaimReport.OcrLongLat.IndexOf("/");
                var latitude = location.ClaimReport.OcrLongLat.Substring(0, longLat)?.Trim();
                var longitude = location.ClaimReport.OcrLongLat.Substring(longLat + 1)?.Trim().Replace("/", "");
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.OcrLocationUrl = url;
                RootObject rootObject = getAddress((latitude), (longitude));

                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                else
                {
                    registeredLatitude = Convert.ToDouble(value: location.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(location.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;
                if (distance > 0)
                {
                    address = address + $" \n\r The location is {distance} km away from expected address";
                }
                ViewBag.OcrLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = getAddress("-37.839542", "145.164834");
                ViewBag.OcrLocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.OcrLocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }

            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var curl = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = curl;

            var beneficiarylatLong = location.PinCode.Latitude + "," + location.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            var reportUpdateStatus = AssessorRemarkType.OK;

            var claim = await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

            await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # <i> {0} </i>] report submitted to Company !", claim.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(ClaimsInvestigationController.Assessor));
        }

        [HttpPost]
        public async Task<IActionResult> ReProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            var reportUpdateStatus = AssessorRemarkType.REVIEW;

            var claim = await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

            await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # <i> {0} </i> ] investigation reassigned !", claim.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(ClaimsInvestigationController.Assessor));
        }

        [HttpPost]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be assigned.");
                return RedirectToAction(nameof(Draft));
            }

            //IF AUTO ALLOCATION TRUE
            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(e => e.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(e => e.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId);

            if (company is not null && company.Auto)
            {
                var autoAllocatedClaims = new List<string>();
                foreach (var claim in claims)
                {
                    string pinCode2Verify = string.Empty;
                    //1. GET THE PINCODE FOR EACH CLAIM
                    var claimsInvestigation = _context.ClaimsInvestigation
                        .Include(c => c.PolicyDetail)
                        .Include(c => c.CustomerDetail)
                        .ThenInclude(c => c.PinCode)
                        .First(c => c.ClaimsInvestigationId == claim);
                    var beneficiary = _context.CaseLocation.Include(b => b.PinCode).FirstOrDefault(b => b.ClaimsInvestigationId == claim);

                    if (claimsInvestigation.PolicyDetail?.ClaimType == ClaimType.HEALTH)
                    {
                        pinCode2Verify = claimsInvestigation.CustomerDetail?.PinCode?.Code;
                    }
                    else
                    {
                        pinCode2Verify = beneficiary.PinCode?.Code;
                    }

                    var vendorsInPincode = new List<Vendor>();

                    //2. GET THE VENDORID FOR EACH CLAIM BASED ON PINCODE
                    foreach (var empanelledVendor in company.EmpanelledVendors)
                    {
                        foreach (var serviceType in empanelledVendor.VendorInvestigationServiceTypes)
                        {
                            if (serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                                    serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId)
                            {
                                foreach (var pincodeService in serviceType.PincodeServices)
                                {
                                    if (pincodeService.Pincode == pinCode2Verify)
                                    {
                                        vendorsInPincode.Add(empanelledVendor);
                                        continue;
                                    }
                                }
                            }
                            var added = vendorsInPincode.Any(v => v.VendorId == empanelledVendor.VendorId);
                            if (added)
                            {
                                continue;
                            }
                        }
                    }

                    if (vendorsInPincode.Count == 0)
                    {
                        foreach (var empanelledVendor in company.EmpanelledVendors)
                        {
                            foreach (var serviceType in empanelledVendor.VendorInvestigationServiceTypes)
                            {
                                if (serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                                        serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId)
                                {
                                    foreach (var pincodeService in serviceType.PincodeServices)
                                    {
                                        if (pincodeService.Pincode.Contains(pinCode2Verify.Substring(0, pinCode2Verify.Length - 2)))
                                        {
                                            vendorsInPincode.Add(empanelledVendor);
                                            continue;
                                        }
                                    }
                                }
                                var added = vendorsInPincode.Any(v => v.VendorId == empanelledVendor.VendorId);
                                if (added)
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    if (vendorsInPincode.Count == 0)
                    {
                        foreach (var empanelledVendor in company.EmpanelledVendors)
                        {
                            foreach (var serviceType in empanelledVendor.VendorInvestigationServiceTypes)
                            {
                                if (serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                                        serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId)
                                {
                                    var pincode = _context.PinCode.Include(p => p.District).FirstOrDefault(p => p.Code == pinCode2Verify);
                                    if (serviceType.District.DistrictId == pincode.District.DistrictId)
                                    {
                                        vendorsInPincode.Add(empanelledVendor);
                                        continue;
                                    }
                                }
                                var added = vendorsInPincode.Any(v => v.VendorId == empanelledVendor.VendorId);
                                if (added)
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    var distinctVendors = vendorsInPincode.Distinct()?.ToList();

                    //3. CALL SERVICE WITH VENDORID
                    if (vendorsInPincode is not null && vendorsInPincode.Count > 0)
                    {
                        var vendorsWithCaseLoad = (await claimsInvestigationService.GetAgencyLoad(distinctVendors)).OrderBy(o => o.CaseCount)?.ToList();

                        if (vendorsWithCaseLoad is not null && vendorsWithCaseLoad.Count > 0)
                        {
                            var selectedVendor = vendorsWithCaseLoad.FirstOrDefault();

                            var policy = await claimsInvestigationService.AllocateToVendor(userEmail, claimsInvestigation.ClaimsInvestigationId, selectedVendor.Vendor.VendorId, beneficiary.CaseLocationId);

                            autoAllocatedClaims.Add(claim);

                            await mailboxService.NotifyClaimAllocationToVendor(userEmail, policy.PolicyDetail.ContractNumber, claimsInvestigation.ClaimsInvestigationId, selectedVendor.Vendor.VendorId, beneficiary.CaseLocationId);
                        }
                    }
                }
                if (claims.Count == autoAllocatedClaims.Count)
                {
                    toastNotification.AddSuccessToastMessage($"<i class='far fa-file-powerpoint'></i> {autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-allocated !");
                }

                if (claims.Count > autoAllocatedClaims.Count)
                {
                    toastNotification.AddWarningToastMessage($"<i class='far fa-file-powerpoint'></i> {autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-allocated !");

                    var notAutoAllocated = claims.Except(autoAllocatedClaims)?.ToList();

                    await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                    await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                    toastNotification.AddWarningToastMessage($"<i class='far fa-file-powerpoint'></i> {notAutoAllocated.Count}/{claims.Count} claim(s) assigned successfully !");

                    return RedirectToAction(nameof(Assigner));
                }
            }
            else
            {
                await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, claims);

                await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, claims);

                toastNotification.AddSuccessToastMessage($"<i class='far fa-file-powerpoint'></i> {claims.Count}/{claims.Count} claim(s) assigned successfully !");
            }

            return RedirectToAction(nameof(Draft));
        }

        // GET: ClaimsInvestigation/Details/5
        [Breadcrumb("Details", FromAction = "Draft")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            if (claimsInvestigation.CustomerDetail is not null)
            {
                var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.CustomerLocationUrl = url;
            }

            if (location is not null)
            {
                var beneficiarylatLong = location.PinCode.Latitude + "," + location.PinCode.Longitude;
                var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.BeneficiaryLocationUrl = bUrl;
            }

            return View(model);
        }

        [Breadcrumb("Start New", FromAction = "Draft")]
        public async Task<IActionResult> CreatedPolicy(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = await _context.CaseLocation.FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = null,
                Location = location
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CaseReadyToAssign(ClaimTransactionModel model)
        {
            if (model == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }
            var claimsInvestigation = await _context.ClaimsInvestigation
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == model.Claim.ClaimsInvestigationId);
            claimsInvestigation.IsReady2Assign = true;
            _context.ClaimsInvestigation.Update(claimsInvestigation);

            await _context.SaveChangesAsync();

            toastNotification.AddSuccessToastMessage("<i class='far fa-file-powerpoint'></i> Claim details completed successfully!");

            return RedirectToAction(nameof(Draft));
        }

        [Breadcrumb(title: " Detail", FromAction = "Active")]
        public async Task<IActionResult> Detail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };
            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = url;

            var beneficiarylatLong = location.PinCode.Latitude + "," + location.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Index")]
        public async Task<IActionResult> ReadyDetail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Assign")]
        public async Task<IActionResult> AssignDetail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        [Breadcrumb(title: " Add New")]
        public async Task<IActionResult> CreatePolicy()
        {
            var userEmailToSend = string.Empty;
            var lineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId;

            var random = new Random();
            var model = new ClaimsInvestigation
            {
                PolicyDetail = new PolicyDetail
                {
                    LineOfBusinessId = lineOfBusinessId,
                    CaseEnablerId = _context.CaseEnabler.FirstOrDefault().CaseEnablerId,
                    CauseOfLoss = "LOST IN ACCIDENT",
                    ClaimType = ClaimType.DEATH,
                    ContractIssueDate = DateTime.UtcNow.AddDays(-10),
                    CostCentreId = _context.CostCentre.FirstOrDefault().CostCentreId,
                    DateOfIncident = DateTime.UtcNow.AddDays(-3),
                    InvestigationServiceTypeId = _context.InvestigationServiceType.FirstOrDefault(i => i.Code == "COMP").InvestigationServiceTypeId,
                    Comments = "SOMETHING FISHY",
                    SumAssuredValue = random.Next(100000, 9999999),
                    ContractNumber = "POLX" + random.Next(1000, 9999),
                }
            };

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (clientCompanyUser == null)
            {
                model.HasClientCompany = false;
                userEmailToSend = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin).Email;
            }
            else
            {
                var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Assigner.ToString()));

                var assignerUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var assignedUser in assignerUsers)
                {
                    var isTrue = await userManager.IsInRoleAsync(assignedUser, assignerRole.Name);
                    if (isTrue)
                    {
                        userEmailToSend = assignedUser.Email;
                        break;
                    }
                }

                model.PolicyDetail.ClientCompanyId = clientCompanyUser.ClientCompanyId;
            }
            ViewBag.ClientCompanyId = clientCompanyUser.ClientCompanyId;

            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.LineOfBusinessId == model.PolicyDetail.LineOfBusinessId), "InvestigationServiceTypeId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name");
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePolicy(ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claim = await claimsInvestigationService.CreatePolicy(userEmail, claimsInvestigation, documentFile, profileFile);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Policy # <i><b> {0} </b> </i>  created successfully !", claim.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(Details), new { id = claim.ClaimsInvestigationId });
        }

        [Breadcrumb(title: " Edit Policy", FromAction = "Draft")]
        public async Task<IActionResult> EditPolicy(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("Edit", "ClaimsInvestigation", "Edit Policy") { Parent = incompleteClaim, RouteValues = new { id = id } };

            ViewData["BreadcrumbNode"] = locationPage;

            return View(claimsInvestigation);
        }

        [HttpPost]
        public async Task<IActionResult> EditPolicy(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claim = await claimsInvestigationService.EdiPolicy(userEmail, claimsInvestigation, documentFile);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Policy # <i><b> {0} </b></i>  edited successfully !", claimsInvestigation.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(Details), new { id = claim.ClaimsInvestigationId });
        }

        [Breadcrumb(title: " Add Customer", FromAction = "Draft")]
        public async Task<IActionResult> CreateCustomer(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var countryId = _context.Country.FirstOrDefault().CountryId;
            var stateId = _context.State.FirstOrDefault().StateId;
            var districtId = _context.District.Include(d => d.State).FirstOrDefault(d => d.StateId == stateId).DistrictId;
            var pinCodeId = _context.PinCode.Include(p => p.District).FirstOrDefault(p => p.DistrictId == districtId).PinCodeId;
            var random = new Random();
            claimsInvestigation.CustomerDetail = new CustomerDetail
            {
                Addressline = random.Next(100, 999) + " GOOD STREET",
                ContactNumber = random.NextInt64(5555555555, 9999999999),
                CountryId = countryId,
                CustomerDateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddDays(20),
                CustomerEducation = Education.PROFESSIONAL,
                CustomerIncome = Income.UPPER_INCOME,
                CustomerName = GenerateName(),
                CustomerOccupation = Occupation.SELF_EMPLOYED,
                CustomerType = CustomerType.HNI,
                Description = "DODGY PERSON",
                StateId = stateId,
                DistrictId = districtId,
                PinCodeId = pinCodeId,
                Gender = Gender.MALE,
            };

            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);

            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("CreateCustomer", "ClaimsInvestigation", "Add Customer") { Parent = incompleteClaim, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = locationPage;
            return View(claimsInvestigation);
        }

        public static string GenerateName()
        {
            var random = new Random();
            string firstName = firstNames[random.Next(0, firstNames.Length)];
            string lastName = lastNames[random.Next(0, lastNames.Length)];

            return $"{firstName} {lastName}";
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, bool create = true)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claim = await claimsInvestigationService.CreateCustomer(userEmail, claimsInvestigation, documentFile, profileFile, create);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='fas fa-user-plus'></i> Customer {0} added successfully !", claimsInvestigation.CustomerDetail.CustomerName));

            return RedirectToAction(nameof(Details), new { id = claim.ClaimsInvestigationId });
        }

        [Breadcrumb(title: " Edit Customer", FromAction = "Draft")]
        public async Task<IActionResult> EditCustomer(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("EditCustomer", "ClaimsInvestigation", "Edit Customer") { Parent = incompleteClaim, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = locationPage;

            return View(claimsInvestigation);
        }

        [HttpPost]
        public async Task<IActionResult> EditCustomer(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, bool create = true)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claim = await claimsInvestigationService.EditCustomer(userEmail, claimsInvestigation, profileFile);

            //await mailboxService.NotifyClaimCreation(userEmail, claimsInvestigation);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='fas fa-user-check'></i> Customer {0} edited successfully !", claimsInvestigation.CustomerDetail.CustomerName));

            return RedirectToAction(nameof(Details), new { id = claim.ClaimsInvestigationId });
        }

        // GET: ClaimsInvestigation/Create
        [Breadcrumb(title: " Create Claim")]
        public async Task<IActionResult> Create()
        {
            var userEmailToSend = string.Empty;
            var model = new ClaimsInvestigation { PolicyDetail = new PolicyDetail { LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId } };

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (clientCompanyUser == null)
            {
                model.HasClientCompany = false;
                userEmailToSend = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin).Email;
            }
            else
            {
                var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Assigner.ToString()));

                var assignerUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var assignedUser in assignerUsers)
                {
                    var isTrue = await userManager.IsInRoleAsync(assignedUser, assignerRole.Name);
                    if (isTrue)
                    {
                        userEmailToSend = assignedUser.Email;
                        break;
                    }
                }

                model.PolicyDetail.ClientCompanyId = clientCompanyUser.ClientCompanyId;
            }
            ViewBag.ClientCompanyId = clientCompanyUser.ClientCompanyId;

            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name");
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.LineOfBusinessId == model.PolicyDetail.LineOfBusinessId), "InvestigationServiceTypeId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name");
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View(model);
        }

        // POST: ClaimsInvestigation/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            if (status == null || !ModelState.IsValid)
            {
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
                ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);
                ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
                ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);
                toastNotification.AddErrorToastMessage("Error!!!");
                return View(claimsInvestigation);
            }
            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claimId = await claimsInvestigationService.CreatePolicy(userEmail, claimsInvestigation, documentFile, profileFile);

            //await mailboxService.NotifyClaimCreation(userEmail, claimsInvestigation);

            toastNotification.AddSuccessToastMessage("<i class=\"fas fa-newspaper\"></i> Claim created successfully!");

            return RedirectToAction(nameof(Details), new { id = claimId });
        }

        // GET: ClaimsInvestigation/Edit/5
        [Breadcrumb(title: " Edit Claim", FromAction = "Draft")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation.Include(c => c.PolicyDetail).FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);

            return View(claimsInvestigation);
        }

        // POST: ClaimsInvestigation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            if (claimsInvestigationId != claimsInvestigation.ClaimsInvestigationId || !ModelState.IsValid)
            {
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
                ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);
                ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
                ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);
                toastNotification.AddErrorToastMessage("Error!!!");

                return View(claimsInvestigation);
            }
            try
            {
                IFormFile documentFile = null;
                IFormFile profileFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail.Document?.Name);
                    if (file != null)
                    {
                        documentFile = file;
                    }
                    file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail.ProfileImage?.Name);
                    if (file != null)
                    {
                        profileFile = file;
                    }
                }

                await claimsInvestigationService.Create(userEmail, claimsInvestigation, documentFile, profileFile, false);
                toastNotification.AddSuccessToastMessage("claim case edited successfully!");
            }
            catch (Exception)
            {
                if (!ClaimsInvestigationExists(claimsInvestigation.ClaimsInvestigationId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Draft));
        }

        // GET: ClaimsInvestigation/Edit/5
        [Breadcrumb(title: " Withdraw", FromAction = "Active")]
        public async Task<IActionResult> Withdraw(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            return View(model);
        }

        // POST: ClaimsInvestigation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        public async Task<IActionResult> SetWithdraw(ClaimsInvestigation claimsInvestigation)
        {
            if (claimsInvestigation == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var finishedStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var withDrawnByCompanySubStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);

            var existingClaim = await _context.ClaimsInvestigation.FindAsync(claimsInvestigation.ClaimsInvestigationId);
            if (existingClaim != null)
            {
                string userEmail = HttpContext?.User?.Identity.Name;
                existingClaim.Updated = DateTime.UtcNow;
                existingClaim.UpdatedBy = userEmail;
                existingClaim.Deleted = true;
                existingClaim.InvestigationCaseSubStatus = withDrawnByCompanySubStatus;
                existingClaim.InvestigationCaseStatus = finishedStatus;
                existingClaim.PolicyDetail.Comments = claimsInvestigation.PolicyDetail.Comments;
                _context.ClaimsInvestigation.Update(existingClaim);
                await _context.SaveChangesAsync();

                toastNotification.AddSuccessToastMessage("claim case withdrawn successfully!");

                return RedirectToAction(nameof(ToInvestigate));
            }

            toastNotification.AddErrorToastMessage("Err: claim withdrawl!");

            return RedirectToAction(nameof(ToInvestigate));
        }

        // GET: ClaimsInvestigation/Delete/5
        [Breadcrumb(title: " Delete", FromAction = "Draft")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            return View(model);
        }

        // POST: ClaimsInvestigation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ClaimTransactionModel model)
        {
            if (model == null || _context.ClaimsInvestigation == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ClaimsInvestigation'  is null.");
            }
            var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(model.Claim.ClaimsInvestigationId);
            string userEmail = HttpContext?.User?.Identity.Name;
            claimsInvestigation.Updated = DateTime.UtcNow;
            claimsInvestigation.UpdatedBy = userEmail;
            claimsInvestigation.Deleted = true;
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim deleted successfully !"));
            return RedirectToAction(nameof(Draft));
        }

        [Breadcrumb(title: " Agency detail", FromAction = "Draft")]
        public async Task<IActionResult> VendorDetail(string companyId, string id, string backurl, string selectedcase)
        {
            if (id == null || _context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendor == null)
            {
                return NotFound();
            }
            ViewBag.CompanyId = companyId;
            ViewBag.Backurl = backurl;
            ViewBag.Selectedcase = selectedcase;

            return View(vendor);
        }

        [Breadcrumb(" Upload Log")]
        public async Task<IActionResult> UploadNewLogs()
        {
            var fileuploadViewModel = await LoadAllFiles();
            ViewBag.Message = TempData["Message"];
            return View(fileuploadViewModel);
        }

        public async Task<IActionResult> DownloadLog(int id)
        {
            var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (file == null) return null;
            var memory = new MemoryStream();
            using (var stream = new FileStream(file.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, file.FileType, file.Name + file.Extension);
        }

        public async Task<IActionResult> DeleteLog(int id)
        {
            var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (file == null) return null;
            if (System.IO.File.Exists(file.FilePath))
            {
                System.IO.File.Delete(file.FilePath);
            }
            _context.FilesOnFileSystem.Remove(file);
            _context.SaveChanges();
            TempData["Message"] = $"Removed {file.Name + file.Extension} successfully from File System.";
            return RedirectToAction("UploadNewLogs");
        }

        private async Task<FileUploadViewModel> LoadAllFiles()
        {
            var viewModel = new FileUploadViewModel();
            viewModel.FilesOnFileSystem = await _context.FilesOnFileSystem.ToListAsync();
            return viewModel;
        }

        private bool ClaimsInvestigationExists(string id)
        {
            return (_context.ClaimsInvestigation?.Any(e => e.ClaimsInvestigationId == id)).GetValueOrDefault();
        }
    }
}