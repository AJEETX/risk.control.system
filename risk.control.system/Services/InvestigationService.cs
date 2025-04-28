using System;
using System.Security.Claims;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SkiaSharp;

using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Services
{
    public interface IInvestigationService
    {
        Task<int> GetAutoCount(string currentUserEmail);
        Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<object> GetManagerActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        InvestigationCreateModel Create(string currentUserEmail);
        InvestigationTask AddCasePolicy(string userEmail);
        Task<InvestigationTask> CreatePolicy(string userEmail, InvestigationTask claimsInvestigation, IFormFile? claimDocument);
        Task<InvestigationTask> EditPolicy(string userEmail, InvestigationTask claimsInvestigation, IFormFile? claimDocument);
        Task<ClientCompany> CreateCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument);
        Task<ClientCompany> EditCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument);
        Task<ClientCompany> CreateBeneficiary(string userEmail, long ClaimsInvestigationId, BeneficiaryDetail beneficiary, IFormFile? customerDocument);
        Task<ClientCompany> EditBeneficiary(string userEmail, long beneficiaryDetailId, BeneficiaryDetail beneficiary, IFormFile? customerDocument);
        Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id);
        List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors);
        Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id);
    }
    public class InvestigationService : IInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly INumberSequenceService numberService;
        private readonly ICloneReportService cloneService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ITimelineService timelineService;
        private readonly ICustomApiCLient customApiCLient;

        public InvestigationService(ApplicationDbContext context, 
            INumberSequenceService numberService,
            ICloneReportService cloneService,
            IWebHostEnvironment webHostEnvironment,
            ITimelineService timelineService,
            ICustomApiCLient customApiCLient)
        {
            this.context = context;
            this.numberService = numberService;
            this.cloneService = cloneService;
            this.webHostEnvironment = webHostEnvironment;
            this.timelineService = timelineService;
            this.customApiCLient = customApiCLient;
        }

        public InvestigationCreateModel Create(string currentUserEmail)
        {
            var companyUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);
            var claim = new InvestigationTask
            {
                ClientCompany = companyUser.ClientCompany
            };
            bool userCanCreate = true;
            int availableCount = 0;
            var trial = companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial;
            if (trial)
            {
                var totalClaimsCreated = context.Investigations.Include(c => c.PolicyDetail).Where(c => !c.Deleted &&
                    c.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated.Count;

                if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                }
            }
            var model = new InvestigationCreateModel
            {
                InvestigationTask = claim,
                AllowedToCreate = userCanCreate,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                BeneficiaryDetail = new BeneficiaryDetail { },
                AvailableCount = availableCount,
                TotalCount = companyUser.ClientCompany.TotalCreatedClaimAllowed,
                Trial = trial
            };
            return model;
        }
        public InvestigationTask AddCasePolicy(string userEmail)
        {
            var contractNumber = numberService.GetNumberSequence("PX");
            var model = new InvestigationTask
            {
                PolicyDetail = new PolicyDetail
                {
                    InsuranceType = InsuranceType.CLAIM,
                    CaseEnablerId = context.CaseEnabler.FirstOrDefault().CaseEnablerId,
                    CauseOfLoss = "LOST IN ACCIDENT",
                    ContractIssueDate = DateTime.Now.AddDays(-10),
                    CostCentreId = context.CostCentre.FirstOrDefault().CostCentreId,
                    DateOfIncident = DateTime.Now.AddDays(-3),
                    InvestigationServiceTypeId = context.InvestigationServiceType.FirstOrDefault(i => i.InsuranceType == InsuranceType.CLAIM).InvestigationServiceTypeId,
                    Comments = "SOMETHING FISHY",
                    SumAssuredValue = new Random().Next(10000, 99999),
                    ContractNumber = contractNumber
                },
                Status = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR
            };
            return model;
        }

        public async Task<InvestigationTask> CreatePolicy(string userEmail, InvestigationTask claimsInvestigation, IFormFile? claimDocument)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    claimsInvestigation.PolicyDetail.DocumentImage = dataStream.ToArray();
                }
                var reportTemplate = await CloneReportTemplate(currentUser.ClientCompanyId.Value, claimsInvestigation.PolicyDetail.InsuranceType.Value);

                claimsInvestigation.IsNew = true;
                claimsInvestigation.CreatedUser = userEmail;
                claimsInvestigation.CaseOwner = userEmail;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.ORIGIN = ORIGIN.USER;
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.Status = CONSTANTS.CASE_STATUS.INITIATED;
                claimsInvestigation.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR;
                claimsInvestigation.CreatorSla = currentUser.ClientCompany.CreatorSla;
                claimsInvestigation.ClientCompany = currentUser.ClientCompany;
                claimsInvestigation.ClientCompanyId = currentUser.ClientCompanyId;
                claimsInvestigation.ReportTemplate = reportTemplate;
                claimsInvestigation.ReportTemplateId = reportTemplate.Id;
                var aaddedClaimId = context.Investigations.Add(claimsInvestigation);

                var saved = await context.SaveChangesAsync() > 0;

                await timelineService.UpdateTaskStatus(claimsInvestigation.Id, userEmail);

                return saved ? claimsInvestigation : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null!;
            }
        }
        private async Task<ReportTemplate> CloneReportTemplate(long clientCompanyId, InsuranceType insuranceType)
        {
            var masterTemplate = await context.ReportTemplates
                .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.Questions)
            .FirstOrDefaultAsync(r => r.ClientCompanyId == clientCompanyId && r.InsuranceType == insuranceType && r.Basetemplate);
            var cloned = cloneService.DeepCloneReportTemplate(masterTemplate);
            context.ReportTemplates.Add(cloned);
            await context.SaveChangesAsync();
            return cloned;
        }
        public async Task<InvestigationTask> EditPolicy(string userEmail, InvestigationTask claimsInvestigation, IFormFile? claimDocument)
        {
            try
            {
                var existingPolicy = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ClientCompany)
                        .FirstOrDefaultAsync(c => c.Id == claimsInvestigation.Id);
                existingPolicy.IsNew = true;
                existingPolicy.PolicyDetail.ContractIssueDate = claimsInvestigation.PolicyDetail.ContractIssueDate;
                existingPolicy.PolicyDetail.InvestigationServiceTypeId = claimsInvestigation.PolicyDetail.InvestigationServiceTypeId;
                existingPolicy.PolicyDetail.CostCentreId = claimsInvestigation.PolicyDetail.CostCentreId;
                existingPolicy.PolicyDetail.CaseEnablerId = claimsInvestigation.PolicyDetail.CaseEnablerId;
                existingPolicy.PolicyDetail.DateOfIncident = claimsInvestigation.PolicyDetail.DateOfIncident;
                existingPolicy.PolicyDetail.ContractNumber = claimsInvestigation.PolicyDetail.ContractNumber;
                existingPolicy.PolicyDetail.SumAssuredValue = claimsInvestigation.PolicyDetail.SumAssuredValue;
                existingPolicy.PolicyDetail.CauseOfLoss = claimsInvestigation.PolicyDetail.CauseOfLoss;
                existingPolicy.Updated = DateTime.Now;
                existingPolicy.UpdatedBy = userEmail;
                existingPolicy.ORIGIN = ORIGIN.USER;
                existingPolicy.PolicyDetail.InsuranceType = claimsInvestigation.PolicyDetail.InsuranceType;
                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    existingPolicy.PolicyDetail.DocumentImage = dataStream.ToArray();
                }

                context.Investigations.Update(existingPolicy);

                var saved = await context.SaveChangesAsync() > 0;

                return saved ? existingPolicy : null;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null!;
            }
        }

        public async Task<ClientCompany> CreateCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument)
        {
            try
            {
                var claimsInvestigation = await context.Investigations.Include(c => c.PolicyDetail)
                   .FirstOrDefaultAsync(c => c.Id == customerDetail.InvestigationTaskId);

                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    customerDetail.ProfilePicture = dataStream.ToArray();
                }
                claimsInvestigation.IsNew = true;
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.ORIGIN = ORIGIN.USER;

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

                context.Investigations.Update(claimsInvestigation);
                var saved = await context.SaveChangesAsync() > 0;

                return saved ? currentUser.ClientCompany : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }
        public async Task<ClientCompany> EditCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument)
        {
            try
            {
                var claimsInvestigation = await context.Investigations.Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(c => c.Id == customerDetail.InvestigationTaskId);

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
                        .FirstOrDefaultAsync(c => c.InvestigationTaskId == customerDetail.InvestigationTaskId);
                    customerDetail.ProfilePicture ??= existingCustomer.ProfilePicture;
                }
                claimsInvestigation.IsNew = true;
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.ORIGIN = ORIGIN.USER;

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
                context.Investigations.Update(claimsInvestigation);
                // Save changes to the database
                var saved = await context.SaveChangesAsync() > 0;

                return saved ? currentUser.ClientCompany : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }
        public async Task<ClientCompany> CreateBeneficiary(string userEmail, long ClaimsInvestigationId, BeneficiaryDetail beneficiary, IFormFile? customerDocument)
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
                var claimsInvestigation = await context.Investigations.Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(m => m.Id == ClaimsInvestigationId);

                claimsInvestigation.IsNew = true;
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.IsReady2Assign = true;
                claimsInvestigation.ORIGIN = ORIGIN.USER;
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

                context.Investigations.Update(claimsInvestigation);
                var saved = await context.SaveChangesAsync() > 0;

                return saved ? currentUser.ClientCompany : null;
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
                var claimsInvestigation = await context.Investigations.Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(m => m.Id == beneficiary.InvestigationTaskId);

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

                claimsInvestigation.IsNew = true;
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.ORIGIN = ORIGIN.USER;
                claimsInvestigation.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR;
                claimsInvestigation.IsReady2Assign = true;
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
                context.Investigations.Update(claimsInvestigation);
                var saved = await context.SaveChangesAsync() > 0;

                return saved ? currentUser.ClientCompany : null;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }
        public async Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id)
        {
            var claim = await context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            var companyUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - claim.Created ;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                TimeTaken = totalTimeTaken,
                Withdrawable = (claim.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public async Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id)
        {
            var claim = await context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.AgentIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.CaseQuestionnaire)
                .ThenInclude(c => c.Questions)
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefaultAsync(m => m.Id == id);

            var companyUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - claim.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = context.VendorInvoice.FirstOrDefault(i => i.InvestigationReportId == claim.InvestigationReportId);

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                Withdrawable = (claim.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors)
        {
            // Get relevant status IDs in one query
            var relevantStatuses = new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
                }; // Improves lookup performance

            // Fetch cases that match the criteria
            var vendorCaseCount = context.Investigations
                .Where(c => !c.Deleted &&
                            c.VendorId.HasValue &&
                            c.AssignedToAgency &&
                            relevantStatuses.Contains(c.SubStatus))
                .GroupBy(c => c.VendorId.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // Create the list of VendorIdWithCases
            return existingVendors
                .Select(vendorId => new VendorIdWithCases
                {
                    VendorId = vendorId,
                    CaseCount = vendorCaseCount.GetValueOrDefault(vendorId, 0)
                })
                .ToList();
        }
        public async Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (companyUser == null)
                return null;
            var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            // Fetching all relevant substatuses in a single query for efficiency

            var query = context.Investigations
                .Include(i=>i.PolicyDetail)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i=>i.CustomerDetail)
                .ThenInclude(i=>i.PinCode)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.Country)
                .Include(i=>i.BeneficiaryDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.Country)

                .Where(a => !a.Deleted &&
                    a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == currentUserEmail &&
                    (
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY ||
                        a.SubStatus== CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
                    )
                );

            int totalRecords = query.Count(); // Get total count before pagination

            // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                    a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.ToLower().Contains(search) ||
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
                query = query.Where(c => c.PolicyDetail.InsuranceType == Enum.Parse<InsuranceType>(caseType));  // Assuming CaseType is the field in your data model
            }

            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();


            // Calculate TimeElapsed and Transform Data
            var transformedData = data.Select(a => new
            {
                Id = a.Id,
                IsNew = a.IsNew,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                PolicyId = a.PolicyDetail.ContractNumber,
                AssignedToAgency = a.IsNew,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeCode = ClaimsInvestigationExtension.GetPincodeCode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-light\">customer name</span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                IsUploaded = a.IsUploaded,
                Origin = a.ORIGIN.GetEnumDisplayName().ToLower(),
                SubStatus = a.SubStatus,
                Ready2Assign = a.IsValidCaseData(),
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.ORIGIN.GetEnumDisplayName(),
                Created = a.Created.ToString("dd-MM-yyyy"),
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ( {a.PolicyDetail.InvestigationServiceType.Name})",
                timePending = GetDraftedTimePending(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-light\">beneficiary name</span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds,
                BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "?" : a.BeneficiaryDetail.Name,
                CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "?" : a.CustomerDetail.Name,
                PersonMapAddressUrl = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail) != "..." ?
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap : Applicationsettings.NO_MAP
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

            var idsToMarkViewed = pagedData.Where(x => x.IsNew).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNew = false;

                await context.SaveChangesAsync(); // mark as viewed
            }

            var response = new
            {
                draw = draw,
                AutoAllocatopn = company.AutoAllocation,
                recordsTotal = totalRecords,
                recordsFiltered = recordsFiltered,
                data = pagedData
            };

            return response;

        }
        string GetDraftedTimePending(InvestigationTask a)
        {
            if (a.CreatorSla == 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} days since created!\"></i>");
            }
            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} day</span>");

            else if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} day</span>");
            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Days} day</span>");

            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Hours < 24 &&
                DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        public async Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            var subStatus = new[]
            {
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
            };
            var query = context.Investigations
                .Include(i => i.Vendor)
                .Include(i => i.PolicyDetail)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.Country)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.Country)
                .Where(a => !a.Deleted && a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                            a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == currentUserEmail &&
                            !subStatus
                            .Contains(a.SubStatus));

            int totalRecords = query.Count(); // Get total count before pagination
            // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                     a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
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
                query = query.Where(c => c.PolicyDetail.InsuranceType == Enum.Parse<InsuranceType>(caseType));  // Assuming CaseType is the field in your data model
            }

            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();

            var transformedData = data.Select(a => new
            {
                Id = a.Id,
                AutoAllocated = a.IsAutoAllocated,
                IsNew = a.IsNew,
                CustomerFullName = a.CustomerDetail?.Name ?? "",
                BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? "",
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent =  GetOwner(a),
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwnerImage(a))),
                CaseWithPerson = a.CaseOwner,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                Status = a.ORIGIN.GetEnumDisplayName(),
                SubStatus = a.SubStatus,
                Ready2Assign = a.IsReady2Assign,
                ServiceType = $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = a.GetCreatorTimePending(),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds, // Calculate here
                PersonMapAddressUrl = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap
            }); // Materialize the list

            // Apply Sorting AFTER Data Transformation
            if (!string.IsNullOrEmpty(orderDir))
            {
                switch (orderColumn)
                {
                    case 0: // Sort by Policy Number
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.PolicyId)
                            : transformedData.OrderByDescending(a => a.PolicyId);
                        break;

                    case 1: // Sort by Amount (Ensure proper sorting of numeric values)
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => a.Amount)
                            : transformedData.OrderByDescending(a => a.Amount);
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
                            ? transformedData.OrderBy(a => a.SubStatus)
                            : transformedData.OrderByDescending(a => a.SubStatus);
                        break;

                    case 11: // Sort by Created Date
                        transformedData = orderDir.ToLower() == "asc"
                            ? transformedData.OrderBy(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null))
                            : transformedData.OrderByDescending(a => DateTime.ParseExact(a.Created, "dd-MM-yyyy", null));
                        break;

                    case 13: // Sort by TimeElapsed
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

            var idsToMarkViewed = pagedData.Where(x => x.IsNew).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNew = false;

                await context.SaveChangesAsync(); // mark as viewed
            }
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = recordsFiltered,
                data = pagedData
            };

            return response;

        }

        public async Task<object> GetManagerActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var assignedToAssignerStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;
            var submittedToAssessorStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;

            var query = context.Investigations
                .Include(i => i.Vendor)
                .Include(i => i.PolicyDetail)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.CustomerDetail)
                .ThenInclude(i => i.Country)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.PinCode)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.District)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.State)
                .Include(i => i.BeneficiaryDetail)
                .ThenInclude(i => i.Country)
                .Where(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
                    a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                    (a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR));

            int totalRecords = query.Count(); // Get total count before pagination

            // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                    a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
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
                query = query.Where(c => c.PolicyDetail.InsuranceType == Enum.Parse<InsuranceType>(caseType));  // Assuming CaseType is the field in your data model
            }

            var data = query.AsEnumerable();
            int recordsFiltered = query.Count();

            var transformedData = data.Select(a => new
            {
                Id = a.Id,
                AutoAllocated = a.IsAutoAllocated,
                CustomerFullName = a.CustomerDetail?.Name ?? string.Empty,
                BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? string.Empty,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = GetOwner(a),
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwnerImage(a))),
                CaseWithPerson = a.CaseOwner,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeCode = ClaimsInvestigationExtension.GetPincodeCode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage))
                        : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture))
                        : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i> </span>",
                Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                Status = a.ORIGIN.GetEnumDisplayName(),
                SubStatus = a.SubStatus,
                Ready2Assign = a.IsReady2Assign,
                ServiceType = $"{a.PolicyDetail?.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetManagerActiveTimePending(a),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture))
                        : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name)
                        ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>"
                        : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds,
                IsNewAssigned = a.IsNewAssignedToManager,
                PersonMapAddressUrl = a.GetMap(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.SubStatus == assignedToAssignerStatus,
                                                      a.SubStatus == submittedToAssessorStatus)
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
            var idsToMarkViewed = pagedData.Where(x => x.IsNewAssigned).Select(x => x.Id).ToList();

            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewAssignedToManager = false;

                await context.SaveChangesAsync(); // mark as viewed
            }
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = recordsFiltered,
                data = pagedData
            };

            return response;
        }
        
        private static string GetManagerActiveTimePending(InvestigationTask a)
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
        private byte[] GetOwnerImage(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
            var noDataimage = File.ReadAllBytes(noDataImagefilePath);

            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                var agentProfile = context.Vendor.FirstOrDefault(u => u.VendorId == a.VendorId)?.DocumentImage;
                if (agentProfile != null)
                {
                    return agentProfile;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var vendorImage = context.VendorApplicationUser.FirstOrDefault(v => v.Email == a.TaskedAgentEmail)?.ProfilePicture;
                if (vendorImage != null)
                {
                    return vendorImage;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                )
            {
                var company = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == a.ClientCompanyId).DocumentImage;
                if (company != null)
                {
                    return company;
                }
            }
            return noDataimage;
        }
        public string GetOwner(InvestigationTask a)
        {
            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || 
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                return a.Vendor.Email;
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                return a.TaskedAgentEmail;
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                )
            {
                var company = context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == a.ClientCompanyId);
                if (company != null)
                {
                    return company.Email;
                }
            }
            return string.Empty;
        }
        public async Task<int> GetAutoCount(string currentUserEmail)
        {
            var companyUser = await context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (companyUser == null)
                return 0;
            var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            // Fetching all relevant substatuses in a single query for efficiency
            var subStatuses =  new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                };

            var query = context.Investigations
                .Where(a => !a.Deleted &&
                    a.ClientCompanyId == companyUser.ClientCompanyId &&
                    (
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY  ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY  ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
                ));

            int totalRecords = query.Count(); // Get total count before pagination
            return totalRecords;
        }
    }
}
