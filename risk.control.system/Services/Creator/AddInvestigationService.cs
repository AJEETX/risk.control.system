using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;

namespace risk.control.system.Services.Creator
{
    public interface IAddInvestigationService
    {
        Task<InvestigationTask> CreateCase(string userEmail, CreateCaseViewModel model);

        Task<InvestigationTask> EditCase(string userEmail, EditPolicyDto dto);

        Task<bool> CreateCustomer(string userEmail, CustomerDetail customerDetail);

        Task<bool> EditCustomer(string userEmail, CustomerDetail customerDetail);

        Task<bool> CreateBeneficiary(string userEmail, BeneficiaryDetail beneficiary);

        Task<bool> EditBeneficiary(string userEmail, BeneficiaryDetail beneficiary);
    }

    internal class AddInvestigationService : IAddInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<AddInvestigationService> logger;
        private readonly IFileStorageService fileStorageService;
        private readonly INumberSequenceService numberService;
        private readonly ICloneReportService cloneService;
        private readonly ITimelineService timelineService;
        private readonly ICustomApiClient customApiCLient;

        public AddInvestigationService(ApplicationDbContext context,
            ILogger<AddInvestigationService> logger,
            IFileStorageService fileStorageService,
            INumberSequenceService numberService,
            ICloneReportService cloneService,
            ITimelineService timelineService,
            ICustomApiClient customApiCLient)
        {
            this.context = context;
            this.logger = logger;
            this.fileStorageService = fileStorageService;
            this.numberService = numberService;
            this.cloneService = cloneService;
            this.timelineService = timelineService;
            this.customApiCLient = customApiCLient;
        }

        public async Task<InvestigationTask> CreateCase(string userEmail, CreateCaseViewModel model)
        {
            try
            {
                var currentUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);

                var (fileName, relativePath) = await fileStorageService.SaveAsync(model.Document, "Case", model.PolicyDetailDto.ContractNumber);
                var caseTask = new InvestigationTask
                {
                    PolicyDetail = new PolicyDetail
                    {
                        ContractNumber = model.PolicyDetailDto.ContractNumber,
                        InsuranceType = model.PolicyDetailDto.InsuranceType,
                        InvestigationServiceTypeId = model.PolicyDetailDto.InvestigationServiceTypeId,
                        CaseEnablerId = model.PolicyDetailDto.CaseEnablerId,
                        SumAssuredValue = model.PolicyDetailDto.SumAssuredValue,
                        ContractIssueDate = model.PolicyDetailDto.ContractIssueDate,
                        DateOfIncident = model.PolicyDetailDto.DateOfIncident,
                        CauseOfLoss = model.PolicyDetailDto.CauseOfLoss,
                        CostCentreId = model.PolicyDetailDto.CostCentreId,
                    }
                };

                caseTask.PolicyDetail.DocumentPath = relativePath;
                caseTask.PolicyDetail.DocumentImageExtension = Path.GetExtension(fileName);

                var reportTemplate = await cloneService.DeepCloneReportTemplate(currentUser.ClientCompanyId.Value, caseTask.PolicyDetail.InsuranceType.Value);

                caseTask.IsNew = true;
                caseTask.CreatedUser = userEmail;
                caseTask.CaseOwner = userEmail;
                caseTask.Updated = DateTime.Now;
                caseTask.ORIGIN = ORIGIN.USER;
                caseTask.UpdatedBy = userEmail;
                caseTask.Status = CONSTANTS.CASE_STATUS.INITIATED;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR;
                caseTask.CreatorSla = currentUser.ClientCompany.CreatorSla;
                caseTask.ClientCompany = currentUser.ClientCompany;
                caseTask.ClientCompanyId = currentUser.ClientCompanyId;
                caseTask.ReportTemplate = reportTemplate;
                caseTask.ReportTemplateId = reportTemplate.Id;
                var aaddedClaimId = context.Investigations.Add(caseTask);
                await numberService.SaveNumberSequence("PX");
                var saved = await context.SaveChangesAsync() > 0;

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return saved ? caseTask : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return null!;
            }
        }

        public async Task<InvestigationTask> EditCase(string userEmail, EditPolicyDto dto)
        {
            try
            {
                var existingPolicy = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ClientCompany)
                        .FirstOrDefaultAsync(c => c.Id == dto.Id);

                existingPolicy.PolicyDetail.ContractNumber = dto.PolicyDetailDto.ContractNumber;
                existingPolicy.PolicyDetail.InsuranceType = dto.PolicyDetailDto.InsuranceType;
                existingPolicy.PolicyDetail.InvestigationServiceTypeId = dto.PolicyDetailDto.InvestigationServiceTypeId;
                existingPolicy.PolicyDetail.CaseEnablerId = dto.PolicyDetailDto.CaseEnablerId;
                existingPolicy.PolicyDetail.SumAssuredValue = dto.PolicyDetailDto.SumAssuredValue;
                existingPolicy.PolicyDetail.ContractIssueDate = dto.PolicyDetailDto.ContractIssueDate;
                existingPolicy.PolicyDetail.DateOfIncident = dto.PolicyDetailDto.DateOfIncident;
                existingPolicy.PolicyDetail.CauseOfLoss = dto.PolicyDetailDto.CauseOfLoss;
                existingPolicy.PolicyDetail.CostCentreId = dto.PolicyDetailDto.CostCentreId;

                existingPolicy.IsNew = true;
                existingPolicy.Updated = DateTime.Now;
                existingPolicy.UpdatedBy = userEmail;
                existingPolicy.ORIGIN = ORIGIN.USER;
                if (dto.Document is not null)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(dto.Document, "Case", dto.PolicyDetailDto.ContractNumber);

                    existingPolicy.PolicyDetail.DocumentPath = relativePath;
                    existingPolicy.PolicyDetail.DocumentImageExtension = Path.GetExtension(fileName);
                }
                var currentUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                var reportTemplate = await cloneService.DeepCloneReportTemplate(currentUser.ClientCompanyId.Value, existingPolicy.PolicyDetail.InsuranceType.Value);
                existingPolicy.ReportTemplate = reportTemplate;
                existingPolicy.ReportTemplateId = reportTemplate.Id;
                context.Investigations.Update(existingPolicy);

                var saved = await context.SaveChangesAsync() > 0;

                return saved ? existingPolicy : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return null!;
            }
        }

        public async Task<bool> CreateCustomer(string userEmail, CustomerDetail customerDetail)
        {
            try
            {
                var caseTask = await context.Investigations.Include(c => c.PolicyDetail)
                   .FirstOrDefaultAsync(c => c.Id == customerDetail.InvestigationTaskId);

                var currentUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                if (customerDetail?.ProfileImage is not null)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(customerDetail?.ProfileImage, "Case", caseTask.PolicyDetail.ContractNumber);
                    customerDetail.ProfilePictureExtension = Path.GetExtension(fileName);
                    customerDetail.ImagePath = relativePath;
                }
                caseTask.IsNew = true;
                caseTask.UpdatedBy = userEmail;
                caseTask.Updated = DateTime.Now;
                caseTask.ORIGIN = ORIGIN.USER;

                customerDetail.PhoneNumber = customerDetail.PhoneNumber.TrimStart('0');
                customerDetail.CountryId = customerDetail.SelectedCountryId;
                customerDetail.StateId = customerDetail.SelectedStateId;
                customerDetail.DistrictId = customerDetail.SelectedDistrictId;
                customerDetail.PinCodeId = customerDetail.SelectedPincodeId;

                var pincode = await context.PinCode
                    .Include(p => p.District)
                    .Include(p => p.State)
                    .Include(p => p.Country)
                    .FirstOrDefaultAsync(p => p.PinCodeId == customerDetail.PinCodeId);

                var address = customerDetail.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latLong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latLong.Latitude + "," + latLong.Longitude;
                customerDetail.Latitude = latLong.Latitude;
                customerDetail.Longitude = latLong.Longitude;

                var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    customerLatLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
                customerDetail.CustomerLocationMap = url;

                var addedClaim = context.CustomerDetail.Add(customerDetail);

                context.Investigations.Update(caseTask);
                return await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return false;
            }
        }

        public async Task<bool> EditCustomer(string userEmail, CustomerDetail customerDetail)
        {
            try
            {
                var caseTask = await context.Investigations.Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(c => c.Id == customerDetail.InvestigationTaskId);

                var currentUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);

                if (customerDetail?.ProfileImage is not null)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(customerDetail?.ProfileImage, "Case", caseTask.PolicyDetail.ContractNumber);
                    customerDetail.ProfilePictureExtension = Path.GetExtension(fileName);
                    customerDetail.ImagePath = relativePath;
                }
                else
                {
                    var existingCustomer = await context.CustomerDetail.AsNoTracking().FirstOrDefaultAsync(c => c.InvestigationTaskId == customerDetail.InvestigationTaskId);
                    customerDetail.ImagePath = existingCustomer.ImagePath;
                }
                caseTask.IsNew = true;
                caseTask.UpdatedBy = userEmail;
                caseTask.Updated = DateTime.Now;
                caseTask.ORIGIN = ORIGIN.USER;
                customerDetail.PhoneNumber = customerDetail.PhoneNumber.TrimStart('0');

                customerDetail.CountryId = customerDetail.SelectedCountryId;
                customerDetail.StateId = customerDetail.SelectedStateId;
                customerDetail.DistrictId = customerDetail.SelectedDistrictId;
                customerDetail.PinCodeId = customerDetail.SelectedPincodeId;

                var pincode = await context.PinCode
                        .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                        .FirstOrDefaultAsync(p => p.PinCodeId == customerDetail.PinCodeId);

                var address = customerDetail.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latLong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latLong.Latitude + "," + latLong.Longitude;
                customerDetail.Latitude = latLong.Latitude;
                customerDetail.Longitude = latLong.Longitude;
                var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    customerLatLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
                customerDetail.CustomerLocationMap = url;

                context.CustomerDetail.Attach(customerDetail);
                context.Entry(customerDetail).State = EntityState.Modified;
                context.Investigations.Update(caseTask);
                // Save changes to the database
                return await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return false;
            }
        }

        public async Task<bool> CreateBeneficiary(string userEmail, BeneficiaryDetail beneficiary)
        {
            try
            {
                var currentUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);

                var caseTask = await context.Investigations.Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(m => m.Id == beneficiary.InvestigationTaskId);
                if (beneficiary?.ProfileImage != null)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(beneficiary?.ProfileImage, "Case", caseTask.PolicyDetail.ContractNumber);
                    beneficiary.ProfilePictureExtension = Path.GetExtension(fileName);
                    beneficiary.ImagePath = relativePath;
                }

                beneficiary.Updated = DateTime.Now;
                beneficiary.UpdatedBy = userEmail;
                caseTask.IsNew = true;
                caseTask.UpdatedBy = userEmail;
                caseTask.Updated = DateTime.Now;
                caseTask.IsReady2Assign = true;
                caseTask.ORIGIN = ORIGIN.USER;
                beneficiary.PhoneNumber = beneficiary.PhoneNumber.TrimStart('0');

                beneficiary.CountryId = beneficiary.SelectedCountryId;
                beneficiary.StateId = beneficiary.SelectedStateId;
                beneficiary.DistrictId = beneficiary.SelectedDistrictId;
                beneficiary.PinCodeId = beneficiary.SelectedPincodeId;

                var pincode = await context.PinCode
                    .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                    .FirstOrDefaultAsync(p => p.PinCodeId == beneficiary.PinCodeId);

                var address = beneficiary.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latlong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latlong.Latitude + "," + latlong.Longitude;
                var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    customerLatLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
                beneficiary.BeneficiaryLocationMap = url;
                beneficiary.Latitude = latlong.Latitude;
                beneficiary.Longitude = latlong.Longitude;
                context.BeneficiaryDetail.Add(beneficiary);

                context.Investigations.Update(caseTask);
                return await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return false;
            }
        }

        public async Task<bool> EditBeneficiary(string userEmail, BeneficiaryDetail beneficiary)
        {
            try
            {
                var caseTask = await context.Investigations.Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(m => m.Id == beneficiary.InvestigationTaskId);

                var currentUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                if (beneficiary?.ProfileImage != null)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(beneficiary?.ProfileImage, "Case", caseTask.PolicyDetail.ContractNumber);
                    beneficiary.ProfilePictureExtension = Path.GetExtension(fileName);
                    beneficiary.ImagePath = relativePath;
                }
                else
                {
                    var existingBeneficiary = await context.BeneficiaryDetail.AsNoTracking().Where(c => c.BeneficiaryDetailId == beneficiary.BeneficiaryDetailId).FirstOrDefaultAsync();
                    if (existingBeneficiary.ImagePath != null)
                    {
                        beneficiary.ImagePath = existingBeneficiary.ImagePath;
                    }
                }

                caseTask.IsNew = true;
                caseTask.UpdatedBy = userEmail;
                caseTask.Updated = DateTime.Now;
                caseTask.ORIGIN = ORIGIN.USER;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR;
                caseTask.IsReady2Assign = true;
                beneficiary.PhoneNumber = beneficiary.PhoneNumber.TrimStart('0');

                beneficiary.CountryId = beneficiary.SelectedCountryId;
                beneficiary.StateId = beneficiary.SelectedStateId;
                beneficiary.DistrictId = beneficiary.SelectedDistrictId;
                beneficiary.PinCodeId = beneficiary.SelectedPincodeId;

                var pincode = await context.PinCode
                    .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                    .FirstOrDefaultAsync(p => p.PinCodeId == beneficiary.PinCodeId);

                var address = beneficiary.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latlong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latlong.Latitude + "," + latlong.Longitude;
                beneficiary.Latitude = latlong.Latitude;
                beneficiary.Longitude = latlong.Longitude;
                var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    customerLatLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
                beneficiary.BeneficiaryLocationMap = url;

                context.BeneficiaryDetail.Attach(beneficiary);
                context.Entry(beneficiary).State = EntityState.Modified;
                context.Investigations.Update(caseTask);
                return await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                return false;
            }
        }
    }
}