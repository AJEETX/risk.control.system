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
               .Include(c => c.ClientCompany)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.BeneficiaryRelation)
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
               .Include(c => c.ClaimNotes)
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
    }
}