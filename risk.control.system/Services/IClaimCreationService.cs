using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Company;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IClaimCreationService
    {
        Task<CaseVerification> Create(string userEmail, CaseVerification claimsInvestigation, IFormFile? claimDocument);
        Task<ClaimsInvestigation> CreatePolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument);

        Task<ClaimsInvestigation> EdiPolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument);

        Task<ClientCompany> CreateCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument);

        Task<ClientCompany> EditCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument);
        Task<ClientCompany> CreateBeneficiary(string userEmail, string ClaimsInvestigationId, BeneficiaryDetail beneficiary, IFormFile? customerDocument);
        Task<ClientCompany> EditBeneficiary(string userEmail, long beneficiaryDetailId, BeneficiaryDetail beneficiary, IFormFile? customerDocument);
    }
    public class ClaimCreationService : IClaimCreationService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext context;
        private readonly ICustomApiCLient customApiCLient;

        public ClaimCreationService(ApplicationDbContext context, ICustomApiCLient customApiCLient)
        {
            this.context = context;
            this.customApiCLient = customApiCLient;
        }
        public async Task<ClaimsInvestigation> CreatePolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var claimId = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;
                var underwritingId = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    claimsInvestigation.PolicyDetail.DocumentImage = dataStream.ToArray();
                }
                var initiatedStatusId = context.InvestigationCaseStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId;
                var createdSubStatusId = context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId;

                claimsInvestigation.PolicyDetail.ClaimType = claimsInvestigation.PolicyDetail.LineOfBusinessId == claimId ? ClaimType.DEATH : ClaimType.HEALTH;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UserEmailActioned = userEmail;
                claimsInvestigation.UserEmailActionedTo = userEmail;
                claimsInvestigation.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.CurrentUserEmail = userEmail;
                claimsInvestigation.CurrentClaimOwner = currentUser.Email;
                claimsInvestigation.InvestigationCaseStatusId = initiatedStatusId;
                claimsInvestigation.InvestigationCaseSubStatusId = createdSubStatusId;
                claimsInvestigation.CreatorSla = currentUser.ClientCompany.CreatorSla;
                claimsInvestigation.ClientCompany = currentUser.ClientCompany;
                var aaddedClaimId = context.ClaimsInvestigation.Add(claimsInvestigation);
                var log = new InvestigationTransaction
                {
                    ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
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
                context.InvestigationTransaction.Add(log);

                return await context.SaveChangesAsync() > 0 ? claimsInvestigation : null!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null!;
            }
        }

        public async Task<ClaimsInvestigation> EdiPolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument)
        {
            try
            {
                var claimId = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;
                var existingPolicy = await context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ClientCompany)
                        .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId);
                existingPolicy.PolicyDetail.ContractIssueDate = claimsInvestigation.PolicyDetail.ContractIssueDate;
                existingPolicy.PolicyDetail.InvestigationServiceTypeId = claimsInvestigation.PolicyDetail.InvestigationServiceTypeId;
                existingPolicy.PolicyDetail.ClaimType = claimsInvestigation.PolicyDetail.ClaimType;
                existingPolicy.PolicyDetail.CostCentreId = claimsInvestigation.PolicyDetail.CostCentreId;
                existingPolicy.PolicyDetail.CaseEnablerId = claimsInvestigation.PolicyDetail.CaseEnablerId;
                existingPolicy.PolicyDetail.DateOfIncident = claimsInvestigation.PolicyDetail.DateOfIncident;
                existingPolicy.PolicyDetail.ContractNumber = claimsInvestigation.PolicyDetail.ContractNumber;
                existingPolicy.PolicyDetail.SumAssuredValue = claimsInvestigation.PolicyDetail.SumAssuredValue;
                existingPolicy.PolicyDetail.CauseOfLoss = claimsInvestigation.PolicyDetail.CauseOfLoss;
                existingPolicy.Updated = DateTime.Now;
                existingPolicy.UpdatedBy = userEmail;
                existingPolicy.CurrentUserEmail = userEmail;
                existingPolicy.CurrentClaimOwner = userEmail;
                existingPolicy.PolicyDetail.ClaimType = claimsInvestigation.PolicyDetail.LineOfBusinessId == claimId? ClaimType.DEATH: ClaimType.HEALTH;
                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    existingPolicy.PolicyDetail.DocumentImage = dataStream.ToArray();
                }

                context.ClaimsInvestigation.Update(existingPolicy);

                return await context.SaveChangesAsync() > 0 ? existingPolicy:null! ;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null!;
            }
        }

        public async Task<ClientCompany> EditCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    await customerDocument.CopyToAsync(dataStream);
                    customerDetail.ProfilePicture = dataStream.ToArray();
                }
                else
                {
                    // Fetch existing customer to retain the existing ProfilePicture
                    var existingCustomer = await context.CustomerDetail
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == customerDetail.ClaimsInvestigationId);
                    customerDetail.ProfilePicture ??= existingCustomer.ProfilePicture;
                }

                // Update foreign key IDs
                customerDetail.CountryId = customerDetail.SelectedCountryId;
                customerDetail.StateId = customerDetail.SelectedStateId;
                customerDetail.DistrictId = customerDetail.SelectedDistrictId;
                customerDetail.PinCodeId = customerDetail.SelectedPincodeId;

                var pincode = context.PinCode
                        .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                        .FirstOrDefault(p => p.PinCodeId == customerDetail.PinCodeId);

                var address = customerDetail.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latLong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latLong.Latitude + "," + latLong.Longitude;
                customerDetail.Latitude = latLong.Latitude;
                customerDetail.Longitude = latLong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                customerDetail.CustomerLocationMap = url;

                // Attach the customerDetail object to the context and mark it as modified
                context.CustomerDetail.Attach(customerDetail);
                context.Entry(customerDetail).State = EntityState.Modified;

                // Save changes to the database
                return await context.SaveChangesAsync() > 0 ? currentUser.ClientCompany: null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        public async Task<ClientCompany> CreateCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    customerDetail.ProfilePicture = dataStream.ToArray();
                }

                customerDetail.CountryId = customerDetail.SelectedCountryId;
                customerDetail.StateId = customerDetail.SelectedStateId;
                customerDetail.DistrictId = customerDetail.SelectedDistrictId;
                customerDetail.PinCodeId = customerDetail.SelectedPincodeId;

                var pincode = context.PinCode
                    .Include(p => p.District)
                    .Include(p => p.State)
                    .Include(p => p.Country)
                    .FirstOrDefault(p => p.PinCodeId == customerDetail.PinCodeId);

                var address = customerDetail.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latLong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latLong.Latitude + "," + latLong.Longitude;
                customerDetail.Latitude = latLong.Latitude;
                customerDetail.Longitude = latLong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                customerDetail.CustomerLocationMap = url;

                var addedClaim = context.CustomerDetail.Add(customerDetail);

                return await context.SaveChangesAsync() > 0? currentUser.ClientCompany : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        public async Task<ClientCompany> CreateBeneficiary(string userEmail, string ClaimsInvestigationId, BeneficiaryDetail beneficiary, IFormFile? customerDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                beneficiary.Updated = DateTime.Now;
                beneficiary.UpdatedBy = userEmail;

                if (customerDocument != null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    beneficiary.ProfilePicture = dataStream.ToArray();
                }

                beneficiary.CountryId = beneficiary.SelectedCountryId;
                beneficiary.StateId = beneficiary.SelectedStateId;
                beneficiary.DistrictId = beneficiary.SelectedDistrictId;
                beneficiary.PinCodeId = beneficiary.SelectedPincodeId;

                var pincode = context.PinCode
                    .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                    .FirstOrDefault(p => p.PinCodeId == beneficiary.PinCodeId);

                var address = beneficiary.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latlong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latlong.Latitude + "," + latlong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                beneficiary.BeneficiaryLocationMap = url;
                beneficiary.Latitude = latlong.Latitude;
                beneficiary.Longitude = latlong.Longitude;
                context.BeneficiaryDetail.Add(beneficiary);

                var claimsInvestigation = await context.ClaimsInvestigation
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == ClaimsInvestigationId);
                claimsInvestigation.IsReady2Assign = true;

                context.ClaimsInvestigation.Update(claimsInvestigation);
                return await context.SaveChangesAsync() > 0 ? currentUser.ClientCompany: null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        public async Task<ClientCompany> EditBeneficiary(string userEmail, long beneficiaryDetailId, BeneficiaryDetail beneficiary, IFormFile? customerDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    beneficiary.ProfilePicture = dataStream.ToArray();
                }
                else
                {
                    var existingBeneficiary = context.BeneficiaryDetail.AsNoTracking().Where(c => c.BeneficiaryDetailId == beneficiaryDetailId).FirstOrDefault();
                    if (existingBeneficiary.ProfilePicture != null)
                    {
                        beneficiary.ProfilePicture = existingBeneficiary.ProfilePicture;
                    }
                }
                beneficiary.CountryId = beneficiary.SelectedCountryId;
                beneficiary.StateId = beneficiary.SelectedStateId;
                beneficiary.DistrictId = beneficiary.SelectedDistrictId;
                beneficiary.PinCodeId = beneficiary.SelectedPincodeId;

                var pincode = context.PinCode
                    .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                    .FirstOrDefault(p => p.PinCodeId == beneficiary.PinCodeId);

                var address = beneficiary.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latlong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latlong.Latitude + "," + latlong.Longitude;
                beneficiary.Latitude = latlong.Latitude;
                beneficiary.Longitude = latlong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                beneficiary.BeneficiaryLocationMap = url;

                context.BeneficiaryDetail.Attach(beneficiary);
                context.Entry(beneficiary).State = EntityState.Modified;
                return await context.SaveChangesAsync() > 0 ? currentUser.ClientCompany : null;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
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
                caseVerification.ClientCompany = currentUser.ClientCompany;
                var aaddedClaimId = context.CaseVerification.Add(caseVerification);
                var log = new CaseVerificationTransaction
                {
                    CaseVerificationId = caseVerification.CaseVerificationId,
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
    }
}
