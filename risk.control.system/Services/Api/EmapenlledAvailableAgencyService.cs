using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IEmpanelledAvailableAgencyService
    {
        Task<object[]> GetAllEmpanelledAgenciesAsync(string userEmail);

        Task<object[]> GetEmpanelledAgency(string userEmail, long caseId);

        Task<object[]> GetAvailableAgencies(string userEmail);
    }

    internal class EmpanelledAvailableAgencyService : IEmpanelledAvailableAgencyService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IBase64FileService base64FileService;

        public EmpanelledAvailableAgencyService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IBase64FileService base64FileService)
        {
            _contextFactory = contextFactory;
            this.base64FileService = base64FileService;
        }

        public async Task<object[]> GetAllEmpanelledAgenciesAsync(string userEmail)
        {
            var statuses = GetValidStatuses();
            await using var _context = _contextFactory.CreateDbContext();

            var claimsCases = await _context.Investigations.AsNoTracking()
                .Where(c => c.AssignedToAgency &&
                            !c.Deleted &&
                            c.VendorId.HasValue &&
                            statuses.Contains(c.SubStatus))
                .ToListAsync(); ;

            var companyUser = await _context.ApplicationUser.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

            var company = await _context.ClientCompany.AsNoTracking()
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.State)
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.District)
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.Country)
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.PinCode)
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.ratings)
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId.Value);
            if (company == null) return Array.Empty<object>();

            var vendorTasks = company.EmpanelledVendors
                .Where(IsActiveVendor)
                .OrderBy(v => v.Name)
                .Select(v => MapVendor(v, companyUser, claimsCases));

            var result = await Task.WhenAll(vendorTasks);

            ResetVendorUpdateFlags(company.EmpanelledVendors);
            await _context.SaveChangesAsync(null, false);

            return result;
        }

        public async Task<object[]> GetAvailableAgencies(string userEmail)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
            var company = await _context.ClientCompany.AsNoTracking()
                .Include(c => c.EmpanelledVendors)
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = await _context.Vendor.AsNoTracking()
                .Where(v => !company.EmpanelledVendors.Contains(v) && !v.Deleted && v.CountryId == company.CountryId)
                .Include(v => v.ApplicationUser)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .OrderBy(u => u.Name).ToListAsync();

            var result =
                availableVendors?.Select(async u =>
                {
                    var documentImage = await base64FileService.GetBase64FileAsync(u.DocumentUrl, Applicationsettings.NO_IMAGE);
                    return new
                    {
                        Id = u.VendorId,
                        Document = documentImage,
                        Domain = u.Email,
                        Name = u.Name,
                        Code = u.Code,
                        Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                        Address = u.Addressline,
                        District = u.District.Name,
                        State = u.State.Name,
                        Country = u.Country.Code,
                        Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                        Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                        UpdateBy = u.UpdatedBy,
                        CanOnboard = u.Status == VendorStatus.ACTIVE &&
                        u.VendorInvestigationServiceTypes != null &&
                        u.ApplicationUser != null &&
                        u.ApplicationUser.Count > 0 &&
                        u.VendorInvestigationServiceTypes.Count > 0,
                        VendorName = u.Email,
                        IsUpdated = u.IsUpdated,
                        LastModified = u.Updated,
                        Deletable = u.CreatedUser == userEmail
                    };
                });
            var awaitedResult = await Task.WhenAll(result);
            availableVendors?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return awaitedResult;
        }

        public async Task<object[]> GetEmpanelledAgency(string userEmail, long caseId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var claimsCases = await _context.Investigations.AsNoTracking().Where(c => c.AssignedToAgency && !c.Deleted && c.VendorId.HasValue && GetValidStatuses().Contains(c.SubStatus)).ToListAsync();
            var companyUser = await _context.ApplicationUser.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

            var company = await _context.ClientCompany.AsNoTracking()
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.State)
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.District)
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.Country)
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.PinCode)
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.ratings)
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            if (company == null)
            {
                return null!;
            }
            var result = company.EmpanelledVendors?.Where(v => !v.Deleted && v.Status == VendorStatus.ACTIVE).OrderBy(u => u.Name)
                .Select(async u =>
                {
                    var hasService = GetPinCodeAndServiceForTheCase(caseId, u.VendorId);
                    var document = base64FileService.GetBase64FileAsync(u.DocumentUrl, Applicationsettings.NO_IMAGE);
                    await Task.WhenAll(document, hasService);
                    return new
                    {
                        Id = u.VendorId,
                        Document = await document,
                        Domain = u.Email,
                        Name = u.Name,
                        Code = u.Code,
                        Phone = $"(+{u.Country.ISDCode}) {u.PhoneNumber}",
                        Address = $"{u.Addressline}",
                        District = u.District.Name,
                        State = u.State.Code,
                        Country = u.Country.Code,
                        Flag = $"/flags/{u.Country.Code.ToLower()}.png",
                        Updated = u.Updated?.ToString("dd-MM-yyyy") ?? u.Created.ToString("dd-MM-yyyy"),
                        UpdateBy = u.UpdatedBy,
                        CaseCount = claimsCases.Count(c => c.VendorId == u.VendorId),
                        RateCount = u.RateCount,
                        RateTotal = u.RateTotal,
                        RawAddress = $"{u.Addressline}, {u.District.Name}, {u.State.Code}, {u.Country.Code}",
                        IsUpdated = u.IsUpdated,
                        LastModified = u.Updated,
                        HasService = await hasService
                    };
                });
            var awaitedResults = await Task.WhenAll(result);
            company.EmpanelledVendors?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return awaitedResults;
        }

        private static string[] GetValidStatuses() => new[]
            {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
            };

        private async Task<bool> GetPinCodeAndServiceForTheCase(long caseId, long vendorId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var selectedCase = await _context.Investigations.AsNoTracking()
                .Include(p => p.PolicyDetail)
                .Include(p => p.CustomerDetail)
                .Include(p => p.BeneficiaryDetail)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            var serviceType = selectedCase.PolicyDetail.InvestigationServiceTypeId;

            long? countryId;
            long? stateId;
            long? districtId;

            if (selectedCase.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                countryId = selectedCase.CustomerDetail.CountryId;
                stateId = selectedCase.CustomerDetail.StateId;
                districtId = selectedCase.CustomerDetail.DistrictId;
            }
            else
            {
                countryId = selectedCase.BeneficiaryDetail.CountryId;
                stateId = selectedCase.BeneficiaryDetail.StateId;
                districtId = selectedCase.BeneficiaryDetail.DistrictId;
            }

            var vendor = await _context.Vendor.AsNoTracking()
                .Include(v => v.VendorInvestigationServiceTypes)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);

            var hasService = vendor?.VendorInvestigationServiceTypes
                .Any(v => v.InvestigationServiceTypeId == serviceType &&
                    v.InsuranceType == selectedCase.PolicyDetail.InsuranceType &&
                            (
                            v.DistrictId == 0 ||
                            v.DistrictId == null ||
                            v.DistrictId == districtId
                            ) &&
                            v.StateId == stateId &&
                            v.CountryId == countryId
                            );
            return hasService ?? false;
        }

        private static bool IsActiveVendor(Vendor v) => !v.Deleted && v.Status == VendorStatus.ACTIVE;

        private async Task<object> MapVendor(Vendor u, ApplicationUser companyUser, List<InvestigationTask> caseTasks)
        {
            var document = await base64FileService.GetBase64FileAsync(u.DocumentUrl, Applicationsettings.NO_IMAGE);
            return new
            {
                Id = u.VendorId,
                Document = document,
                Domain = GetDomain(u, companyUser),
                Name = u.Name,
                Code = u.Code,
                Phone = $"(+{u.Country.ISDCode}) {u.PhoneNumber}",
                Address = u.Addressline,
                District = u.District.Name,
                StateCode = u.State.Code,
                State = u.State.Name,
                CountryCode = u.Country.Code,
                Country = u.Country.Name,
                Flag = $"/flags/{u.Country.Code.ToLower()}.png",
                Updated = (u.Updated ?? u.Created).ToString("dd-MM-yyyy"),
                UpdateBy = u.UpdatedBy,
                CaseCount = caseTasks.Count(c => c.VendorId == u.VendorId),
                RateCount = u.RateCount,
                RateTotal = u.RateTotal,
                RawAddress = $"{u.Addressline}, {u.District.Name}, {u.State.Code}, {u.Country.Code}",
                IsUpdated = u.IsUpdated,
                LastModified = u.Updated
            };
        }

        private static string GetDomain(Vendor u, ApplicationUser user) =>
            user.Role == AppRoles.COMPANY_ADMIN
                ? $"<a href='/Company/AgencyDetail?id={u.VendorId}'>{u.Email}</a>"
                : u.Email;

        private static void ResetVendorUpdateFlags(IEnumerable<Vendor> vendors)
        {
            foreach (var vendor in vendors)
                vendor.IsUpdated = false;
        }
    }
}