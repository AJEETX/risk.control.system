﻿using System.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}, {CREATOR.DISPLAY_NAME}")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;

        public CompanyController(ApplicationDbContext context, UserManager<ClientCompanyApplicationUser> userManager)
        {
            this.userManager = userManager;
            _context = context;
        }

        [HttpGet("AllCompanies")]
        public IActionResult AllCompanies()
        {
            var companies = _context.ClientCompany.
                Where(v =>!v.Deleted)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State).OrderBy(o => o.Name);
            var result =
                companies.Select(u =>
                new
                {
                    Id = u.ClientCompanyId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl,
                    Domain = $"<a href='/ClientCompany/Details?Id={u.ClientCompanyId}'>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Updated = u.Updated.HasValue ?  u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    Active = u.Status.GetEnumDisplayName(),
                    UpdatedBy = u.UpdatedBy
                });

            return Ok(result?.ToArray());
        }

        [HttpGet("CompanyUsers")]
        public async Task<IActionResult> CompanyUsers(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var adminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            if (adminUser is null || !adminUser.IsSuperAdmin)
            {
                return BadRequest();
            }

            var companyUsers = _context.ClientCompanyApplicationUser
                .Include(u => u.PinCode)
                .Include(u => u.Country)
                .Include(u => u.District)
                .Include(u => u.State)
                .Where(c => c.ClientCompanyId == id);

            var users = companyUsers
                .Where(u => !u.Deleted)?
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .AsQueryable();
            var result =
                users?.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    Email = "<a href=''>" + u.Email + "</a>",
                    Phone = u.PhoneNumber,
                    Photo = string.IsNullOrWhiteSpace(u.ProfilePictureUrl) ? Applicationsettings.NO_USER : u.ProfilePictureUrl,
                    Active = u.Active,
                    Addressline = "<span class='badge badge-light'>" + u.Addressline + ", " + u.District.Name + ", " + u.State.Name + ", " + u.Country.Code + "</span>",
                    Roles = u.UserRole != null ? $"<span class=\"badge badge-light\">{u.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Pincode = u.PinCode.Code,
                    Updated = u.Updated.HasValue ?  u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy
                })?.ToArray();
            return Ok(result);
        }

        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser =await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var company = await _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.PinCode)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.Country)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.District)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.State)
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var users = company.CompanyApplicationUser
                .Where(u => !u.Deleted)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .AsQueryable();
            var result =
                users?.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    Email = "<a href=/Company/EditUser?userId=" + u.Id + ">" + u.Email + "</a>",
                    Phone = u.PhoneNumber,
                    Photo = string.IsNullOrWhiteSpace(u.ProfilePictureUrl) ? Applicationsettings.NO_USER : u.ProfilePictureUrl,
                    Active = u.Active,
                    Addressline = "<span class='badge badge-light'>" + u.Addressline + ", " + u.District.Name + ", " + u.State.Name + ", " + u.Country.Code + "</span>",
                    Roles = u.UserRole != null ? $"<span class=\"badge badge-light\">{u.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Pincode = u.PinCode.Code,
                    Updated = u.Updated.HasValue ?  u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    Role = u.UserRole.GetEnumDisplayName(),
                    RawEmail = u.Email,
                    RawAddress = u.Addressline + ", " + u.District.Name + ", " + u.State.Name + ", " + u.Country.Code
                })?.ToArray();
            return Ok(result);
        }

        [HttpGet("GetEmpanelledVendors")]
        public async Task<IActionResult> GetEmpanelledVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name; var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR); 
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT); 
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var claimsCases = _context.ClaimsInvestigation
                .Where(c => c.ClientCompanyId == companyUser.ClientCompanyId && 
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId || 
                c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId || 
                c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId))
                ?.ToList();
            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.State)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.District)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.Country)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.PinCode)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.ratings)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var result = company.EmpanelledVendors?.Where(v => !v.Deleted && v.Status == VendorStatus.ACTIVE)
                .OrderBy(u => u.Name).Select(u => new 
                { 
                    Id = u.VendorId, 
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl, 
                    Domain = companyUser.Role == AppRoles.COMPANY_ADMIN ? "<a href=/Vendors/Details?id=" + u.VendorId + ">" + u.Email + "</a>" : u.Email, 
                    Name = u.Name, 
                    Code = u.Code, 
                    Phone = u.PhoneNumber, 
                    Address = u.Addressline, 
                    District = u.District.Name, 
                    State = u.State.Name, 
                    Country = u.Country.Name, 
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"), 
                    UpdateBy = u.UpdatedBy, 
                    CaseCount = claimsCases.Count(c => c.VendorId == u.VendorId), 
                    RateCount = u.RateCount, 
                    RateTotal = u.RateTotal ,
                    RawAddress = u.Addressline + ","+ u.District.Name +", " + u.State.Code + ", "+u.Country.Code
                });
            return Ok(result?.ToArray());
        }

        [HttpGet("GetAvailableVendors")]
        public async Task<IActionResult> GetAvailableVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = _context.Vendor
                .Where(v =>
                !v.Clients.Any(c => c.ClientCompanyId == companyUser.ClientCompanyId))
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .Where(v => !v.Deleted)
                .OrderBy(u => u.Name)
                .AsQueryable();

            var result =
                availableVendors?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl,
                    Domain = "<a href=/Vendors/Details?id=" + u.VendorId + ">" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    CanOnboard = u.Status == VendorStatus.ACTIVE && u.VendorInvestigationServiceTypes != null && u.VendorInvestigationServiceTypes.Count > 0,
                    VendorName = u.Email
                });
            return Ok(result?.ToArray());

        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var vendor = _context.Vendor
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.LineOfBusiness)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                 .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.Country)
                .Include(i => i.District)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.PincodeServices)
                .FirstOrDefault(a => a.VendorId == id);

            var result = vendor.VendorInvestigationServiceTypes?
                .OrderBy(s => s.InvestigationServiceType.Name)?
                .Select(s => new
                {
                    VendorId = s.VendorId,
                    Id = s.VendorInvestigationServiceTypeId,
                    CaseType = s.LineOfBusiness.Name,
                    ServiceType = s.InvestigationServiceType.Name,
                    District = s.District.Name,
                    State = s.State.Name,
                    Country = s.Country.Name,
                    Pincodes = s.PincodeServices.Count == 0 ?
                    "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                     string.Join("", s.PincodeServices.Select(c => "<span class='badge badge-light'>" + c.Pincode + "</span> ")),
                     RawPincodes = string.Join(", ", s.PincodeServices.Select(c =>  c.Pincode)),
                    Rate = s.Price,
                    UpdatedBy = s.UpdatedBy,
                    Updated = s.Updated.HasValue ? s.Updated.Value.ToString("dd-MM-yyyy") : s.Created.ToString("dd-MM-yyyy")

                });

            return Ok(result?.ToArray());
        }
    }
}