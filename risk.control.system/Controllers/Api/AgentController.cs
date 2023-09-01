using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using risk.control.system.Helpers;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private Regex regex = new Regex(@"^[\w/\:.-]+;base64,");
        private readonly ApplicationDbContext _context;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AgentController(ApplicationDbContext context, IClaimsInvestigationService claimsInvestigationService, IMailboxService mailboxService, IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.webHostEnvironment = webHostEnvironment;
        }

        [AllowAnonymous]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("This is a test endpoint");
        }

        [AllowAnonymous]
        [HttpGet("agent")]
        public async Task<IActionResult> Index(string email = "agent@agency1.com")
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
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.State)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Country)
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
                .ThenInclude(c => c.State);

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
                var claimsAssigned = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                        && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                        && c.AssignedAgentUserEmail == email)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAssigned.Add(item);
                    }
                }
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-policy.jpg");

                var noDocumentimage = await System.IO.File.ReadAllBytesAsync(filePath);

                filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

                var noCustomerimage = await System.IO.File.ReadAllBytesAsync(filePath);

                var claim2Agent = claimsAssigned
                    .Select(c =>
                new
                {
                    claimId = c.ClaimsInvestigationId,
                    claimType = c.PolicyDetail.ClaimType.GetEnumDisplayName(),
                    DocumentPhoto = c.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c.PolicyDetail.DocumentImage)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDocumentimage)),
                    CustomerName = c.CustomerDetail.CustomerName,
                    CustomerEmail = email,
                    PolicyNumber = c.PolicyDetail.ContractNumber,
                    Gender = c.CustomerDetail.Gender.GetEnumDisplayName(),
                    c.CustomerDetail.Addressline,
                    c.CustomerDetail.PinCode.Code,
                    CustomerPhoto = c?.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(c?.CustomerDetail.ProfilePicture)) :
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                    Country = c.CustomerDetail.Country.Name,
                    State = c.CustomerDetail.State.Name,
                    District = c.CustomerDetail.District.Name,
                    c.CustomerDetail.Description,
                    Locations = c.CaseLocations.Select(l => new
                    {
                        l.CaseLocationId,
                        Photo = l?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(l.ProfilePicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                        l.Country.Name,
                        l.BeneficiaryName,
                        l.Addressline,
                        l?.Addressline2,
                        l.PinCode.Code,
                        District = l.District.Name,
                        State = l.State.Name
                    })
                });
                return Ok(claim2Agent);
            }
            return Unauthorized();
        }

        [AllowAnonymous]
        [HttpGet("get")]
        public async Task<IActionResult> Get(string claimId)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId
                );
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var claimCase = _context.CaseLocation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-policy.jpg");

            var noDocumentimage = await System.IO.File.ReadAllBytesAsync(filePath);

            filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "user.png");

            var noCustomerimage = await System.IO.File.ReadAllBytesAsync(filePath);

            filePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(filePath);

            return Ok(
                new
                {
                    Policy = new
                    {
                        ClaimId = claim.ClaimsInvestigationId,
                        PolicyNumber = claim.PolicyDetail.ContractNumber,
                        ClaimType = claim.PolicyDetail.ClaimType.GetEnumDisplayName(),
                        Document = claim.PolicyDetail.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.PolicyDetail.DocumentImage)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDocumentimage)),
                        IssueDate = claim.PolicyDetail.ContractIssueDate.ToString("dd-MMM-yyyy"),
                        IncidentDate = claim.PolicyDetail.DateOfIncident.ToString("dd-MMM-yyyy"),
                        Amount = claim.PolicyDetail.SumAssuredValue,
                        BudgetCentre = claim.PolicyDetail.CostCentre.Name,
                        Reason = claim.PolicyDetail.CaseEnabler.Name
                    },
                    beneficiary = new
                    {
                        BeneficiaryId = claimCase.CaseLocationId,
                        Name = claimCase.BeneficiaryName,
                        Photo = claimCase.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimCase.ProfilePicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                        Relation = claimCase.BeneficiaryRelation.Name,
                        Income = claimCase.BeneficiaryIncome.GetEnumDisplayName(),
                        Phone = claimCase.BeneficiaryContactNumber,
                        DateOfBirth = claimCase.BeneficiaryDateOfBirth.ToString("dd-MMM-yyyy"),
                        Address = claimCase.Addressline + " " + claimCase.District.Name + " " + claimCase.State.Name + " " + claimCase.Country.Name + " " + claimCase.PinCode.Code
                    },
                    Customer = new
                    {
                        Name = claim.CustomerDetail.CustomerName,
                        Occupation = claim.CustomerDetail.CustomerOccupation.GetEnumDisplayName(),
                        Photo = claim.CustomerDetail.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.CustomerDetail.ProfilePicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noCustomerimage)),
                        Income = claim.CustomerDetail.CustomerIncome.GetEnumDisplayName(),
                        Phone = claim.CustomerDetail.ContactNumber,
                        DateOfBirth = claim.CustomerDetail.CustomerDateOfBirth.ToString("dd-MMM-yyyy"),
                        Address = claim.CustomerDetail.Addressline + " " + claim.CustomerDetail.District.Name + " " + claim.CustomerDetail.State.Name + " " + claim.CustomerDetail.Country.Name + " " + claim.CustomerDetail.PinCode.Code
                    },
                    InvestigationData = new
                    {
                        LocationImage = claimCase?.ClaimReport?.AgentLocationPicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimCase?.ClaimReport?.AgentLocationPicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                        OcrImage = claimCase?.ClaimReport?.AgentOcrPicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimCase?.ClaimReport?.AgentOcrPicture)) :
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(noDataimage)),
                        OcrData = claimCase?.ClaimReport?.AgentOcrData,
                        LocationLongLat = claimCase?.ClaimReport?.LocationLongLat,
                        OcrLongLat = claimCase?.ClaimReport?.OcrLongLat,
                    },
                    Remarks = claimCase?.ClaimReport?.AgentRemarks
                });
        }

        [AllowAnonymous]
        [RequestSizeLimit(100_000_000)]
        [HttpPost("post")]
        public async Task<IActionResult> Post(Data data)
        {
            ClaimReport claimReport = null;
            var claimCase = _context.CaseLocation
               .Include(c => c.BeneficiaryRelation)
               .Include(c => c.ClaimReport)
               .Include(c => c.PinCode)
               .Include(c => c.District)
               .Include(c => c.State)
               .Include(c => c.Country)
               .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            if (claimCase == null)
            {
                return BadRequest();
            }
            if (claimCase.ClaimReport == null)
            {
                claimReport = new ClaimReport
                {
                    AgentEmail = data.Email,
                };
            }
            else
            {
                claimReport = claimCase.ClaimReport;
            }

            if (!string.IsNullOrWhiteSpace(data.LocationImage))
            {
                var image = Convert.FromBase64String(data.LocationImage);
                var locationRealImage = ByteArrayToImage(image);
                MemoryStream stream = new MemoryStream(image);
                claimReport.AgentLocationPicture = image;
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"loc{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{locationRealImage.ImageType()}");
                claimReport.AgentLocationPictureUrl = filePath;
                CompressImage.Compressimage(stream, filePath);
                claimReport.LocationLongLatTime = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(data.OcrImage))
            {
                var image = Convert.FromBase64String(data.OcrImage);
                var OcrRealImage = ByteArrayToImage(image);
                MemoryStream stream = new MemoryStream(image);
                claimReport.AgentOcrPicture = image;
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"ocr{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{OcrRealImage.ImageType()}");
                claimReport.AgentOcrUrl = filePath;
                CompressImage.Compressimage(stream, claimReport.AgentOcrUrl);
                claimReport.OcrLongLatTime = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(data.LocationLongLat))
            {
                claimReport.LocationLongLatTime = DateTime.UtcNow;
                claimReport.LocationLongLat = data.LocationLongLat;
            }
            if (!string.IsNullOrWhiteSpace(data.OcrData))
            {
                claimReport.AgentOcrData = data.OcrData;
            }

            if (!string.IsNullOrWhiteSpace(data.OcrLongLat))
            {
                claimReport.OcrLongLat = data.OcrLongLat;
                claimReport.OcrLongLatTime = DateTime.UtcNow;
            }

            claimCase.ClaimReport = claimReport;

            _context.CaseLocation.Update(claimCase);

            await _context.SaveChangesAsync();
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.png");

            var noDataimage = await System.IO.File.ReadAllBytesAsync(noDataImagefilePath);

            return Ok(new
            {
                BeneficiaryId = claimCase.CaseLocationId,
                LocationImage = !string.IsNullOrWhiteSpace(claimReport.AgentLocationPictureUrl) ?
                Convert.ToBase64String(System.IO.File.ReadAllBytes(claimReport.AgentLocationPictureUrl)) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claimReport.LocationLongLat,
                LocationTime = claimReport.LocationLongLatTime,
                OcrImage = !string.IsNullOrWhiteSpace(claimReport.AgentOcrUrl) ?
                Convert.ToBase64String(System.IO.File.ReadAllBytes(claimReport.AgentOcrUrl)) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claimReport.OcrLongLat,
                OcrTime = claimReport.OcrLongLatTime
            });
        }

        [AllowAnonymous]
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(string email, string remarks, string claimId, long BeneficiaryId)
        {
            await claimsInvestigationService.SubmitToVendorSupervisor(email, BeneficiaryId, claimId, remarks);

            await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(email, claimId, BeneficiaryId);

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("Vendors")]
        public async Task<IActionResult> Vendors()
        {
            var applicationDbContext = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes).ToListAsync();

            var data = applicationDbContext.Select(a => new VendorData
            {
                Image = a.DocumentImage,
                Name = a.Name,
                Code = a.Code,
                PhoneNumber = a.PhoneNumber,
                Email = a.Email,
                Addressline = a.Addressline,
                State = a.State.Name,
                Created = a.Created.ToString("dd/MM/yyyy")
            });

            var response = new VendorDataDataTable
            {
                data = data.ToList()
            };
            return Ok(response);
        }

        public Image? ByteArrayToImage(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }
    }

    public class Data
    {
        public string Email { get; set; }
        public string ClaimId { get; set; }
        public string? LocationImage { get; set; }
        public string? LocationLongLat { get; set; }
        public string? OcrImage { get; set; }
        public string? OcrLongLat { get; set; }
        public string? OcrData { get; set; }
        public string? Remarks { get; set; }
    }

    public class VendorData
    {
        public byte[]? Image { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Addressline { get; set; }
        public string State { get; set; }
        public string Created { get; set; }
    }

    public class VendorDataDataTable
    {
        public List<VendorData> data { get; set; }
    }
}