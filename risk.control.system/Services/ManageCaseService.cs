using Google.Api;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

using SkiaSharp;

namespace risk.control.system.Services
{
    public interface IManageCaseService
    {
        CaseVerification AddCase(string userEmail, long? lineOfBusinessId);
        
        Task<CaseVerification> Create(string userEmail, CaseVerification caseVerification, IFormFile? claimDocument);
        Task<CaseVerification> Edit(string userEmail, CaseVerification caseVerification, IFormFile? claimDocument);
        //Task<ClaimsInvestigationVendorsModel> GetEmpanelledVendors(string selectedcase);
        IQueryable<CaseVerification> GetCases();
    }
    public class ManageCaseService : IManageCaseService
    {
        private readonly ApplicationDbContext context;
        private readonly INumberSequenceService numberService;
        private readonly ICustomApiCLient customApiCLient;

        public ManageCaseService(ApplicationDbContext context, INumberSequenceService numberService, ICustomApiCLient customApiCLient)
        {
            this.context = context;
            this.numberService = numberService;
            this.customApiCLient = customApiCLient;
        }
        public CaseVerification AddCase(string userEmail, long? lineOfBusinessId)
        {
            var createdStatus = context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var contractNumber = numberService.GetNumberSequence("PX");
                var currentUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == userEmail);
            var pinCode = context.PinCode.Include(s => s.Country).OrderBy(s => s.Name).FirstOrDefault(s => s.Country.CountryId == currentUser.ClientCompany.CountryId);
            var random = new Random();
            var model = new CaseVerification
            {
                PolicyDetail = new PolicyDetail
                {
                    LineOfBusinessId = lineOfBusinessId,
                    CaseEnablerId = context.CaseEnabler.FirstOrDefault().CaseEnablerId,
                    CauseOfLoss = "LOST IN ACCIDENT",
                    ClaimType = ClaimType.HEALTH,
                    ContractIssueDate = DateTime.Now.AddDays(-10),
                    CostCentreId = context.CostCentre.FirstOrDefault().CostCentreId,
                    DateOfIncident = DateTime.Now.AddDays(-3),
                    InvestigationServiceTypeId = context.InvestigationServiceType.FirstOrDefault(i => i.LineOfBusinessId == lineOfBusinessId).InvestigationServiceTypeId,
                    Comments = "SOMETHING FISHY",
                    SumAssuredValue = new Random().Next(10000, 99999),
                    ContractNumber = contractNumber,
                    InsuranceType = InsuranceType.LIFE,
                },
                InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId,
                UserEmailActioned = userEmail,
                UserEmailActionedTo = userEmail,
                CustomerDetail = new Claimant
                {
                    Addressline = random.Next(100, 999) + " GOOD STREET",
                    ContactNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                    DateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddDays(20),
                    Education = Education.PROFESSIONAL,
                    Income = Income.UPPER_INCOME,
                    Name = NameGenerator.GenerateName(),
                    Occupation = Occupation.SELF_EMPLOYED,
                    CustomerType = CustomerType.HNI,
                    Description = "DODGY PERSON",
                    Country = pinCode.Country,
                    CountryId = pinCode.CountryId,
                    SelectedCountryId = pinCode.CountryId,
                    StateId = pinCode.StateId,
                    SelectedStateId = pinCode.StateId.GetValueOrDefault(),
                    DistrictId = pinCode.DistrictId,
                    SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault(),
                    PinCodeId = pinCode.PinCodeId,
                    SelectedPincodeId = pinCode.PinCodeId,
                    Gender = Gender.MALE,
                },
            };
            return model;
        }

        public async Task<CaseVerification> Create(string userEmail, CaseVerification caseVerification, IFormFile? claimDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                caseVerification.ClientCompanyId = currentUser.ClientCompanyId;

                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    caseVerification.CustomerDetail.ProfilePicture = dataStream.ToArray();
                }
                var initiatedStatusId = context.InvestigationCaseStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId;
                var createdSubStatusId = context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId;

                var pincode = context.PinCode
                    .Include(p => p.District)
                    .Include(p => p.State)
                    .Include(p => p.Country)
                    .FirstOrDefault(p => p.PinCodeId == caseVerification.CustomerDetail.PinCodeId);

                var address = caseVerification.CustomerDetail.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latLong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latLong.Latitude + "," + latLong.Longitude;
                caseVerification.CustomerDetail.Latitude = latLong.Latitude;
                caseVerification.CustomerDetail.Longitude = latLong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                caseVerification.CustomerDetail.CustomerLocationMap = url;

                caseVerification.IsReady2Assign = true;
                caseVerification.PolicyDetail.InsuranceType = InsuranceType.LIFE;
                caseVerification.PolicyDetail.ClaimType = ClaimType.HEALTH;
                caseVerification.Updated = DateTime.Now;
                caseVerification.UserEmailActioned = userEmail;
                caseVerification.UserEmailActionedTo = userEmail;
                caseVerification.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
                caseVerification.UpdatedBy = userEmail;
                caseVerification.CurrentUserEmail = userEmail;
                caseVerification.CurrentClaimOwner = currentUser.Email;
                caseVerification.InvestigationCaseStatusId = initiatedStatusId;
                caseVerification.InvestigationCaseSubStatusId = createdSubStatusId;
                caseVerification.CreatorSla = currentUser.ClientCompany.CreatorSla;
                caseVerification.ClientCompanyId = currentUser.ClientCompanyId;
                caseVerification.ClientCompany = currentUser.ClientCompany;
                var aaddedClaimId = context.CaseVerification.Add(caseVerification);
                var log = new CaseVerificationTransaction
                {
                    CaseVerification = aaddedClaimId.Entity,
                    UserEmailActioned = userEmail,
                    UserEmailActionedTo = userEmail,
                    UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
                    CurrentClaimOwner = currentUser.Email,
                    HopCount = 0,
                    Time2Update = 0,
                    InvestigationCaseStatusId = initiatedStatusId,
                    InvestigationCaseSubStatusId = createdSubStatusId,
                    UpdatedBy = userEmail,
                };
                context.CaseVerificationTransaction.Add(log);

                return await context.SaveChangesAsync() > 0 ? caseVerification : null!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null!;
            }
        }
        public async Task<CaseVerification> Edit(string userEmail, CaseVerification caseVerification, IFormFile? claimDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    caseVerification.CustomerDetail.ProfilePicture = dataStream.ToArray();
                }
                else
                {
                    // Fetch existing customer to retain the existing ProfilePicture
                    var existingCase = await context.CaseVerification.Include(caseVerification => caseVerification.CustomerDetail)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CaseVerificationId == caseVerification.CaseVerificationId);
                    caseVerification.CustomerDetail.ProfilePicture ??= existingCase.CustomerDetail.ProfilePicture;
                }

                var initiatedStatusId = context.InvestigationCaseStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId;
                var createdSubStatusId = context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId;
               
                var pincode = context.PinCode
                    .Include(p => p.District)
                    .Include(p => p.State)
                    .Include(p => p.Country)
                    .FirstOrDefault(p => p.PinCodeId == caseVerification.CustomerDetail.PinCodeId);

                var address = caseVerification.CustomerDetail.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latLong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latLong.Latitude + "," + latLong.Longitude;
                caseVerification.CustomerDetail.Latitude = latLong.Latitude;
                caseVerification.CustomerDetail.Longitude = latLong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                caseVerification.CustomerDetail.CustomerLocationMap = url;

                caseVerification.IsReady2Assign = true;
                caseVerification.PolicyDetail.InsuranceType = InsuranceType.LIFE;
                caseVerification.PolicyDetail.ClaimType = ClaimType.HEALTH;
                caseVerification.Updated = DateTime.Now;
                caseVerification.UserEmailActioned = userEmail;
                caseVerification.UserEmailActionedTo = userEmail;
                caseVerification.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
                caseVerification.UpdatedBy = userEmail;
                caseVerification.CurrentUserEmail = userEmail;
                caseVerification.CurrentClaimOwner = currentUser.Email;
                caseVerification.InvestigationCaseStatusId = initiatedStatusId;
                caseVerification.InvestigationCaseSubStatusId = createdSubStatusId;
                caseVerification.CreatorSla = currentUser.ClientCompany.CreatorSla;
                caseVerification.ClientCompanyId = currentUser.ClientCompanyId;
                caseVerification.ClientCompany = currentUser.ClientCompany;
                var aaddedClaimId = context.CaseVerification.Update(caseVerification);
                return await context.SaveChangesAsync() > 0 ? caseVerification : null!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null!;
            }
        }

        public IQueryable<CaseVerification> GetCases()
        {
            IQueryable<CaseVerification> applicationDbContext = context.CaseVerification
               .Include(c => c.PolicyDetail)
               .Include(c => c.ClientCompany)
               .ThenInclude(c => c.Country)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
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
               .Include(c => c.Vendor)
               .Include(c => c.ClaimNotes)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderByDescending(o => o.Created);
        }
    }
}
