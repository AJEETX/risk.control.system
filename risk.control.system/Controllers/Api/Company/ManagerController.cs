﻿using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;
using risk.control.system.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.Controllers.Api.Claims;
using Microsoft.AspNetCore.Hosting;
using System.Linq.Expressions;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = MANAGER.DISPLAY_NAME)]
    public class ManagerController : ControllerBase
    {
        private const string UNDERWRITING = "underwriting";
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IClaimsService claimsService;

        public ManagerController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IClaimsService claimsService)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            hindiNFO.CurrencySymbol = string.Empty;
            this.claimsService = claimsService;
        }

        [HttpGet("Get")]
        public async Task<IActionResult> Get()
        {
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("User not authenticated.");

            var companyUser = await _context.ClientCompanyApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null)
                return NotFound("Company user not found.");

            // Get required statuses in one query
            var statuses = await _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR
                }.Contains(i.Name))
                .ToListAsync();

            if (statuses.Count < 2)
                return BadRequest("Required statuses are missing.");

            var submittedToAssessorStatusId = statuses.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)?.InvestigationCaseSubStatusId;
            var replyToAssessorStatusId = statuses.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)?.InvestigationCaseSubStatusId;

            // Filter claims based on the required conditions
            var applicationDbContext = _context.ClaimsInvestigation
                .Include(a => a.Vendor)
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(a => a.PolicyDetail)
                .ThenInclude(a => a.LineOfBusiness)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(i => i.ClientCompanyId == companyUser.ClientCompanyId &&
                            (i.InvestigationCaseSubStatusId == submittedToAssessorStatusId ||
                             i.InvestigationCaseSubStatusId == replyToAssessorStatusId) &&
                            string.IsNullOrEmpty(i.UserEmailActionedTo) &&
                            i.UserRoleActionedTo == companyUser.ClientCompany.Email);

            // Create lists to hold the claims that need updates
            var claimsAssigned = new List<ClaimsInvestigation>();
            var newClaimsAssigned = new List<ClaimsInvestigation>();

            // Process claims and update views
            foreach (var claim in applicationDbContext)
            {
                claim.AssessView += 1;
                if (claim.AssessView <= 1)
                {
                    newClaimsAssigned.Add(claim);
                }
                claimsAssigned.Add(claim);
            }

            if (newClaimsAssigned.Any())
            {
                _context.ClaimsInvestigation.UpdateRange(newClaimsAssigned);
                await _context.SaveChangesAsync();
            }

            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            var response = claimsAssigned
                .Select(a => new ClaimsInvestigationResponse
                {
                    Id = a.ClaimsInvestigationId,
                    AutoAllocated = a.AutoAllocated,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                    Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                    Policy = a.PolicyDetail?.LineOfBusiness.Name,
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    ServiceType = $"{a.PolicyDetail?.LineOfBusiness.Name} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.InvestigationCaseSubStatus.Name,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetManagerTimePending(true),
                    PolicyNum = a.GetPolicyNum(),
                    BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                       Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                                      "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                                      a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.SubmittedToAssessorTime.GetValueOrDefault()).TotalSeconds,
                    Agency = a.Vendor?.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.Vendor.DocumentImage)),
                    IsNewAssigned = a.AssessView <= 1,
                    PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                })
                .ToList();

            return Ok(response);
        }


        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("User not authenticated.");

            var companyUser = await _context.ClientCompanyApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null)
                return NotFound("Company user not found.");

            // Get statuses in a single query
            var openStatuses = await _context.InvestigationCaseStatus
                .Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED))
                .ToListAsync();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();

            var caseSubStatuses = await _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR
                }.Contains(i.Name.ToUpper()))
                .ToListAsync();

            var createdStatus = caseSubStatuses.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedToAssignerStatus = caseSubStatuses.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var replyToAssessorStatus = caseSubStatuses.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);
            var submittedToAssessorStatus = caseSubStatuses.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            if (createdStatus == null || assignedToAssignerStatus == null || replyToAssessorStatus == null || submittedToAssessorStatus == null)
                return BadRequest("Some required case substatuses are missing.");

            var query = claimsService.GetClaims()
                .Where(a => a.ClientCompanyId == companyUser.ClientCompanyId)
                .Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId))
                .Where(a => a.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId)
                .Where(a => a.InvestigationCaseSubStatusId != assignedToAssignerStatus.InvestigationCaseSubStatusId)
                .Where(a => a.InvestigationCaseSubStatusId != replyToAssessorStatus.InvestigationCaseSubStatusId)
                .Where(a => a.InvestigationCaseSubStatusId != submittedToAssessorStatus.InvestigationCaseSubStatusId);
            
            int totalRecords = query.Count(); // Get total count before pagination

            // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                    a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
                    a.PolicyDetail.LineOfBusiness.Name.ToLower().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.DateOfBirth.ToString().ToLower().Contains(search) ||
                    a.CustomerDetail.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.ContactNumber.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Code.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Name.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.ContactNumber.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(caseType))
            {
                query = query.Where(c => c.PolicyDetail.LineOfBusiness.Name.ToLower() == caseType);  // Assuming CaseType is the field in your data model
            }

            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();

            var newClaims = new List<ClaimsInvestigation>();

            foreach (var claim in query)
            {
                claim.ManagerActiveView += 1;
                if (claim.ManagerActiveView <= 1)
                {
                    newClaims.Add(claim);
                }
            }

            if (newClaims.Any())
            {
                _context.ClaimsInvestigation.UpdateRange(newClaims);
                await _context.SaveChangesAsync();
            }

            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

            var transformedData = data.Select(a => new
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                CustomerFullName = a.CustomerDetail?.Name ?? string.Empty,
                BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? string.Empty,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                CaseWithPerson = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeCode = ClaimsInvestigationExtension.GetPincodeCode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage))
                        : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture))
                        : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i> </span>",
                Policy = a.PolicyDetail?.LineOfBusiness.Name,
                Status = a.ORIGIN.GetEnumDisplayName(),
                SubStatus = a.InvestigationCaseSubStatus.Name,
                Ready2Assign = a.IsReady2Assign,
                ServiceType = $"{a.PolicyDetail?.LineOfBusiness.Name} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.InvestigationCaseSubStatus.Name,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerActiveTimePending(a),
                Withdrawable = !a.NotWithdrawable,
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture))
                        : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name)
                        ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>"
                        : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds,
                IsNewAssigned = a.ManagerActiveView <= 1,
                PersonMapAddressUrl = a.GetMapUrl(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.InvestigationCaseSubStatus == assignedToAssignerStatus,
                                                      a.InvestigationCaseSubStatus == submittedToAssessorStatus)
            });

            // Apply Sorting AFTER Data Transformation
            if (!string.IsNullOrEmpty(orderDir))
            {
                switch (orderColumn)
                {
                    case 1: // Sort by Policy Number
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.PolicyId)
                            : transformedData.OrderByDescending(a => a.PolicyId);
                        break;

                    case 2: // Sort by Amount (Ensure proper sorting of numeric values)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.Amount)
                            : transformedData.OrderByDescending(a => a.Amount);
                        break;

                    case 3: // Sort by Amount (Ensure proper sorting of numeric values)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.PincodeCode)
                            : transformedData.OrderByDescending(a => a.PincodeCode);
                        break;

                    case 6: // Sort by Customer Full Name
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.CustomerFullName)
                            : transformedData.OrderByDescending(a => a.CustomerFullName);
                        break;

                    case 8: // Sort by Beneficiary Full Name
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.BeneficiaryFullName)
                            : transformedData.OrderByDescending(a => a.BeneficiaryFullName);
                        break;


                    case 9: // Sort by Status
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.ServiceType)
                            : transformedData.OrderByDescending(a => a.ServiceType);
                        break;

                    case 10: // Sort by Status
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.Location)
                            : transformedData.OrderByDescending(a => a.Location);
                        break;

                    case 11: // Sort by Created Date
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null))
                            : transformedData.OrderByDescending(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null));
                        break;

                    case 12: // Sort by TimeElapsed
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.TimeElapsed)
                            : transformedData.OrderByDescending(a => a.TimeElapsed);
                        break;

                    default: // Default Sorting (if needed)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.TimeElapsed)
                            : transformedData.OrderByDescending(a => a.TimeElapsed);
                        break;
                }
            }
            // Apply Pagination
            var pagedData = transformedData.Skip(start).Take(length).ToList();
            // Prepare Response

            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = recordsFiltered,
                data = pagedData
            };

            return Ok(response);
        }
        private static string GetManagerActiveTimePending(ClaimsInvestigation a)
        {
            if (a.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");

            else if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        [HttpGet("GetReport")]
        public async Task<IActionResult> GetReport()
        {
            var user = HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(user))
            {
                return BadRequest("User identity is missing.");
            }

            var companyUser = await _context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(u => u.Email == user);

            if (companyUser == null)
            {
                return NotFound("User not found.");
            }

            // Fetching the statuses once
            var approvedStatus = await _context.InvestigationCaseSubStatus
                .FirstOrDefaultAsync(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            
            var finishStatus = await _context.InvestigationCaseStatus
                .FirstOrDefaultAsync(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            if (approvedStatus == null || finishStatus == null)
            {
                return NotFound("Required statuses not found.");
            }

            // Optimized claim filtering
            var claimsQuery = claimsService.GetClaims()
                .Where(c => c.CustomerDetail != null && c.AgencyReport != null && c.ClientCompanyId == companyUser.ClientCompanyId)
                .Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId);

            var claimsSubmitted = new List<ClaimsInvestigation>();

            // Iterate over filtered claims
            foreach (var claim in await claimsQuery.ToListAsync())
            {
                // Directly add claim to list
                claimsSubmitted.Add(claim);
            }

            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            var response = claimsSubmitted.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        $"<span class='badge badge-light'>{a.UserEmailActionedTo}</span>" :
                        $"<span class='badge badge-light'>{a.UserRoleActionedTo}</span>",
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ?
                    $"data:image/*;base64,{Convert.ToBase64String(a.PolicyDetail.DocumentImage)}" :
                    Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ?
                    $"data:image/*;base64,{Convert.ToBase64String(a.CustomerDetail.ProfilePicture)}" :
                    Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ??
                    "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /></span>",
                Policy = a.PolicyDetail?.LineOfBusiness.Name,
                Status = a.ORIGIN.GetEnumDisplayName(),
                ServiceType = $"{a.PolicyDetail?.LineOfBusiness.Name} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.InvestigationCaseSubStatus.Name,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerTimeCompleted(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                    $"data:image/*;base64,{Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)}" :
                    Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i></span>" :
                    a.BeneficiaryDetail.Name,
                Agency = a.Vendor?.Name,
                OwnerDetail = $"data:image/*;base64,{Convert.ToBase64String(a.Vendor.DocumentImage)}",
                TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime ?? DateTime.Now).TotalSeconds,
                PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration
            }).ToList();

            return Ok(response);
        }

        private static string GetManagerTimeCompleted(ClaimsInvestigation a)
        {
            if (a.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");

            else if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ProcessedByAssessorTime.GetValueOrDefault()).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        [HttpGet("GetReject")]
        public async Task<IActionResult> GetReject()
        {
            var userEmail = HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest("User identity is missing.");
            }

            // Fetch the company user
            var companyUser = await _context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null)
            {
                return NotFound("User not found.");
            }

            // Get the rejected status
            var rejectedStatus = await _context.InvestigationCaseSubStatus
                .FirstOrDefaultAsync(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            if (rejectedStatus == null)
            {
                return NotFound("Rejected status not found.");
            }

            // Filter claims based on rejected status and client company ID
            var claimsQuery = claimsService.GetClaims()
                .Where(c => c.CustomerDetail != null && c.AgencyReport != null)
                .Where(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                            c.InvestigationCaseSubStatusId == rejectedStatus.InvestigationCaseSubStatusId);

            var claims = await claimsQuery.ToListAsync();

            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            var response = claims.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        $"<span class='badge badge-light'>{a.UserEmailActionedTo}</span>" :
                        $"<span class='badge badge-light'>{a.UserRoleActionedTo}</span>",
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ?
                    $"data:image/*;base64,{Convert.ToBase64String(a.PolicyDetail.DocumentImage)}" :
                    Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ?
                    $"data:image/*;base64,{Convert.ToBase64String(a.CustomerDetail.ProfilePicture)}" :
                    Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ??
                    "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /></span>",
                Policy = $"<span class='badge badge-light'>{a.PolicyDetail?.LineOfBusiness.Name}</span>",
                Status = $"ORIGIN of Claim: {a.ORIGIN.GetEnumDisplayName()}",
                ServiceType = $"{a.PolicyDetail?.LineOfBusiness.Name} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.InvestigationCaseSubStatus.Name,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerTimeCompleted(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                    $"data:image/*;base64,{Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)}" :
                    Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                    a.BeneficiaryDetail.Name,
                OwnerDetail = $"data:image/*;base64,{Convert.ToBase64String(a.Vendor.DocumentImage)}",
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration
            }).ToList();

            return Ok(response);
        }

        private byte[] GetOwner(ClaimsInvestigation a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            if (!string.IsNullOrWhiteSpace(a.UserEmailActionedTo) && a.InvestigationCaseSubStatusId == allocated2agent.InvestigationCaseSubStatusId)
            {
                ownerEmail = a.UserEmailActionedTo;
                var agentProfile = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == ownerEmail)?.ProfilePicture;
                if (agentProfile == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return agentProfile;
            }
            else if (string.IsNullOrWhiteSpace(a.UserEmailActionedTo) &&
                !string.IsNullOrWhiteSpace(a.UserRoleActionedTo)
                && a.AssignedToAgency)
            {
                ownerDomain = a.UserRoleActionedTo;
                var vendorImage = _context.Vendor.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (vendorImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return vendorImage;
            }
            else
            {
                ownerDomain = a.UserRoleActionedTo;
                var companyImage = _context.ClientCompany.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (companyImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return companyImage;
            }

        }
    }
}