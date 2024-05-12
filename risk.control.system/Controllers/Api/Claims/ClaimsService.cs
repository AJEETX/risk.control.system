using System.Security.Claims;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api.Claims
{
    public interface IClaimsService
    {
        IQueryable<ClaimsInvestigation> GetClaims();
        List<ClaimsInvesgationResponse> ToResponseModel(List<ClaimsInvestigation> claimsSubmitted);
        decimal? GetLat(ClaimType? claimType, CustomerDetail a, BeneficiaryDetail location);
        decimal? GetLng(ClaimType? claimType, CustomerDetail a, BeneficiaryDetail location);
    }
    public class ClaimsService : IClaimsService
    {
        private readonly ApplicationDbContext _context;

        public ClaimsService(ApplicationDbContext context)
        {
            _context = context;
        }
        public IQueryable<ClaimsInvestigation> GetClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.ClientCompany)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.BeneficiaryRelation)
               .Include(c => c.BeneficiaryDetail.ClaimReport)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
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
               .Include(c => c.Vendor)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(l => l.PreviousClaimReports)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderByDescending(o => o.Created);
        }

        public decimal? GetLat(ClaimType? claimType, CustomerDetail a, BeneficiaryDetail location)
        {
            if (claimType == ClaimType.HEALTH)
            {
                if (a is null)
                    return null;
                return decimal.Parse(a.PinCode.Latitude);
            }
            else
            {
                if (location is null)
                    return null;
                return decimal.Parse(location.PinCode.Latitude);
            }
        }

        public decimal? GetLng(ClaimType? claimType, CustomerDetail a, BeneficiaryDetail location)
        {
            if (claimType == ClaimType.HEALTH)
            {
                if (a is null)
                    return null;
                return decimal.Parse(a.PinCode.Longitude);
            }
            else
            {
                if (location is null)
                    return null;
                return decimal.Parse(location.PinCode.Longitude);
            }
        }

        public List<ClaimsInvesgationResponse> ToResponseModel(List<ClaimsInvestigation> claimsSubmitted)
        {
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var response = claimsSubmitted
                   .Select(a => new ClaimsInvesgationResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       AutoAllocated = a.AutoAllocated,
                       CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.CustomerName) ? "" : a.CustomerDetail.CustomerName,
                       BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ? "" : a.BeneficiaryDetail.BeneficiaryName,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       AssignedToAgency = a.AssignedToAgency,
                       Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                       string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                       string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Document = a.PolicyDetail?.DocumentImage != null ?
                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = a.CustomerDetail?.ProfilePicture != null ?
                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                       Name = a.CustomerDetail?.CustomerName != null ?
                       a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                       Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                       SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Ready2Assign = a.IsReady2Assign,
                       ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "(" + a.PolicyDetail.InvestigationServiceType.Name + ")</span>"),
                       Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                       Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                       timePending = a.GetTimePending(),
                       Withdrawable = a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ? true : false,
                       PolicyNum = a.GetPolicyNum(),
                       BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                      string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                     Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                       "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                       a.BeneficiaryDetail.BeneficiaryName,
                       TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                       IsNewAssigned = a.ActiveView <= 1
                   })?
                   .ToList();
            return response;
        }
    }
}