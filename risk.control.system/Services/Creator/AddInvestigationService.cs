using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
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

    internal class AddInvestigationService(ApplicationDbContext context,
        ILogger<AddInvestigationService> logger,
        IFileStorageService fileStorageService,
        INumberSequenceService numberService,
        ICloneReportService cloneService,
        ITimelineService timelineService,
        ICustomApiClient customApiClient) : IAddInvestigationService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AddInvestigationService> _logger = logger;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly INumberSequenceService _numberService = numberService;
        private readonly ICloneReportService _cloneService = cloneService;
        private readonly ITimelineService _timelineService = timelineService;
        private readonly ICustomApiClient _customApiClient = customApiClient;

        public async Task<InvestigationTask> CreateCase(string userEmail, CreateCaseViewModel model)
        {
            try
            {
                var currentUser = await _context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                var reportTemplate = await _cloneService.DeepCloneReportTemplate(currentUser!.ClientCompanyId!.Value, model.PolicyDetailDto.InsuranceType.GetValueOrDefault());
                _context.ReportTemplates.Add(reportTemplate);
                await _context.SaveChangesAsync();
                var (fileName, relativePath) = await _fileStorageService.SaveAsync(model.Document!, CONSTANTS.CASE, model.PolicyDetailDto.ContractNumber);
                var caseTask = new InvestigationTask
                {
                    PolicyDetail = new PolicyDetail
                    {
                        ContractNumber = WebUtility.HtmlEncode(model.PolicyDetailDto.ContractNumber.ToUpper()),
                        InsuranceType = model.PolicyDetailDto.InsuranceType,
                        InvestigationServiceTypeId = model.PolicyDetailDto.InvestigationServiceTypeId,
                        CaseEnablerId = model.PolicyDetailDto.CaseEnablerId,
                        SumAssuredValue = model.PolicyDetailDto.SumAssuredValue,
                        ContractIssueDate = model.PolicyDetailDto.ContractIssueDate!.Value,
                        DateOfIncident = model.PolicyDetailDto.DateOfIncident!.Value,
                        CauseOfLoss = model.PolicyDetailDto.CauseOfLoss,
                        CostCentreId = model.PolicyDetailDto.CostCentreId,
                        DocumentPath = relativePath,
                        DocumentImageExtension = Path.GetExtension(fileName),
                    },
                    IsNew = true,
                    CreatedUser = userEmail,
                    CaseOwner = userEmail,
                    Updated = DateTime.UtcNow,
                    ORIGIN = ORIGIN.USER,
                    UpdatedBy = userEmail,
                    Status = CONSTANTS.CASE_STATUS.INITIATED,
                    SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                    CreatorSla = currentUser.ClientCompany!.CreatorSla,
                    ClientCompanyId = currentUser.ClientCompanyId,
                    ReportTemplateId = reportTemplate.Id
                };
                _context.Investigations.Add(caseTask);
                await _numberService.SaveNumberSequence("PX");
                var saved = await _context.SaveChangesAsync() > 0;
                await _timelineService.UpdateTaskStatus(caseTask.Id, userEmail);
                return saved ? caseTask : null!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred creating Case detail. {UserEmail}", userEmail);
                return null!;
            }
        }

        public async Task<InvestigationTask> EditCase(string userEmail, EditPolicyDto model)
        {
            try
            {
                var existingPolicy = await _context.Investigations.Include(c => c.PolicyDetail).FirstOrDefaultAsync(c => c.Id == model.Id);
                var reportTemplate = await _cloneService.DeepCloneReportTemplate(existingPolicy!.ClientCompanyId!.Value, model.PolicyDetailDto.InsuranceType.GetValueOrDefault());
                _context.ReportTemplates.Add(reportTemplate);
                await _context.SaveChangesAsync();
                if (model.Document is not null)
                {
                    var (fileName, relativePath) = await _fileStorageService.SaveAsync(model.Document, CONSTANTS.CASE, model.PolicyDetailDto.ContractNumber);
                    existingPolicy.PolicyDetail!.DocumentPath = relativePath;
                    existingPolicy.PolicyDetail.DocumentImageExtension = Path.GetExtension(fileName);
                }
                existingPolicy.IsNew = true;
                existingPolicy.PolicyDetail!.ContractNumber = WebUtility.HtmlEncode(model.PolicyDetailDto.ContractNumber.ToUpper());
                existingPolicy.PolicyDetail.InsuranceType = model.PolicyDetailDto.InsuranceType;
                existingPolicy.PolicyDetail.InvestigationServiceTypeId = model.PolicyDetailDto.InvestigationServiceTypeId;
                existingPolicy.PolicyDetail.CaseEnablerId = model.PolicyDetailDto.CaseEnablerId;
                existingPolicy.PolicyDetail.SumAssuredValue = model.PolicyDetailDto.SumAssuredValue;
                existingPolicy.PolicyDetail.ContractIssueDate = model.PolicyDetailDto.ContractIssueDate!.Value;
                existingPolicy.PolicyDetail.DateOfIncident = model.PolicyDetailDto.DateOfIncident!.Value;
                existingPolicy.PolicyDetail.CauseOfLoss = model.PolicyDetailDto.CauseOfLoss;
                existingPolicy.PolicyDetail.CostCentreId = model.PolicyDetailDto.CostCentreId;
                existingPolicy.Updated = DateTime.UtcNow;
                existingPolicy.UpdatedBy = userEmail;
                existingPolicy.ReportTemplateId = reportTemplate.Id;
                _context.Investigations.Update(existingPolicy);
                var saved = await _context.SaveChangesAsync() > 0;
                return saved ? existingPolicy : null!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred editing Case detail. {UserEmail}", userEmail);
                return null!;
            }
        }

        public async Task<bool> CreateCustomer(string userEmail, CustomerDetail customerDetail)
        {
            try
            {
                var caseTask = await _context.Investigations.Include(c => c.PolicyDetail).FirstOrDefaultAsync(c => c.Id == customerDetail.InvestigationTaskId);
                if (customerDetail?.ProfileImage is not null)
                {
                    var (fileName, relativePath) = await _fileStorageService.SaveAsync(customerDetail?.ProfileImage!, CONSTANTS.CASE, caseTask!.PolicyDetail!.ContractNumber);
                    customerDetail!.ProfilePictureExtension = Path.GetExtension(fileName);
                    customerDetail.ImagePath = relativePath;
                }
                caseTask!.UpdatedBy = userEmail;
                caseTask.IsNew = true;
                caseTask.Updated = DateTime.UtcNow;
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                customerDetail!.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(customerDetail.Name.ToLower()));
                customerDetail.PhoneNumber = customerDetail.PhoneNumber.TrimStart('0');
                customerDetail.CountryId = customerDetail.SelectedCountryId;
                customerDetail.StateId = customerDetail.SelectedStateId;
                customerDetail.DistrictId = customerDetail.SelectedDistrictId;
                customerDetail.PinCodeId = customerDetail.SelectedPincodeId;
                var pincode = await _context.PinCode.AsNoTracking().Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.PinCodeId == customerDetail.PinCodeId);
                var address = customerDetail.Addressline + ", " + pincode!.District!.Name + ", " + pincode.State!.Name + ", " + pincode.Country!.Code;
                var (Latitude, Longitude) = await _customApiClient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = Latitude + "," + Longitude;
                customerDetail.Latitude = Latitude;
                customerDetail.Longitude = Longitude;
                var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}", customerLatLong, EnvHelper.Get("GOOGLE_MAP_KEY"));
                customerDetail.CustomerLocationMap = url;
                var addedClaim = _context.CustomerDetail.Add(customerDetail);
                _context.Investigations.Update(caseTask);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred creating Customer detail. {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> EditCustomer(string userEmail, CustomerDetail customerDetail)
        {
            try
            {
                var caseTask = await _context.Investigations.Include(c => c.PolicyDetail).FirstOrDefaultAsync(c => c.Id == customerDetail.InvestigationTaskId);

                caseTask!.UpdatedBy = userEmail;
                caseTask.IsNew = true;
                caseTask.Updated = DateTime.UtcNow;

                if (customerDetail?.ProfileImage is not null)
                {
                    var (fileName, relativePath) = await _fileStorageService.SaveAsync(customerDetail.ProfileImage, CONSTANTS.CASE, caseTask!.PolicyDetail!.ContractNumber);
                    customerDetail.ProfilePictureExtension = Path.GetExtension(fileName);
                    customerDetail.ImagePath = relativePath;
                }
                else
                {
                    var existingCustomer = await _context.CustomerDetail.AsNoTracking().FirstOrDefaultAsync(c => c.InvestigationTaskId == customerDetail!.InvestigationTaskId);
                    customerDetail!.ImagePath = existingCustomer!.ImagePath;
                }

                customerDetail.PhoneNumber = customerDetail.PhoneNumber.TrimStart('0');
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                customerDetail.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(customerDetail.Name.ToLower()));
                customerDetail.CountryId = customerDetail.SelectedCountryId;
                customerDetail.StateId = customerDetail.SelectedStateId;
                customerDetail.DistrictId = customerDetail.SelectedDistrictId;
                customerDetail.PinCodeId = customerDetail.SelectedPincodeId;
                var pincode = await _context.PinCode.AsNoTracking().Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.PinCodeId == customerDetail.PinCodeId);
                var address = customerDetail.Addressline + ", " + pincode!.District!.Name + ", " + pincode.State!.Name + ", " + pincode.Country!.Code;
                var (Latitude, Longitude) = await _customApiClient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = Latitude + "," + Longitude;
                customerDetail.Latitude = Latitude;
                customerDetail.Longitude = Longitude;
                var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}", customerLatLong, EnvHelper.Get("GOOGLE_MAP_KEY"));
                customerDetail.CustomerLocationMap = url;
                _context.CustomerDetail.Attach(customerDetail);
                _context.Entry(customerDetail).State = EntityState.Modified;
                _context.Investigations.Update(caseTask);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred editing Customer detail. {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> CreateBeneficiary(string userEmail, BeneficiaryDetail beneficiary)
        {
            try
            {
                var caseTask = await _context.Investigations.Include(c => c.PolicyDetail).FirstOrDefaultAsync(m => m.Id == beneficiary.InvestigationTaskId); if (beneficiary?.ProfileImage != null)
                {
                    var (fileName, relativePath) = await _fileStorageService.SaveAsync(beneficiary.ProfileImage!, CONSTANTS.CASE, caseTask!.PolicyDetail!.ContractNumber);
                    beneficiary.ProfilePictureExtension = Path.GetExtension(fileName);
                    beneficiary.ImagePath = relativePath;
                }

                caseTask!.UpdatedBy = userEmail;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.IsNew = true;
                caseTask.IsReady2Assign = true;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR;

                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                beneficiary!.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(beneficiary.Name.ToLower()));
                beneficiary.Updated = DateTime.UtcNow;
                beneficiary.UpdatedBy = userEmail;

                beneficiary.PhoneNumber = beneficiary.PhoneNumber.TrimStart('0');
                beneficiary.CountryId = beneficiary.SelectedCountryId;
                beneficiary.StateId = beneficiary.SelectedStateId;
                beneficiary.DistrictId = beneficiary.SelectedDistrictId;
                beneficiary.PinCodeId = beneficiary.SelectedPincodeId;
                var pincode = await _context.PinCode.AsNoTracking().Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.PinCodeId == beneficiary.PinCodeId);
                var address = beneficiary.Addressline + ", " + pincode!.District!.Name + ", " + pincode.State!.Name + ", " + pincode.Country!.Code;
                var (Latitude, Longitude) = await _customApiClient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = Latitude + "," + Longitude;
                var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}", customerLatLong, EnvHelper.Get("GOOGLE_MAP_KEY"));
                beneficiary.BeneficiaryLocationMap = url;
                beneficiary.Latitude = Latitude;
                beneficiary.Longitude = Longitude;
                _context.BeneficiaryDetail.Add(beneficiary);
                _context.Investigations.Update(caseTask);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred creating Beneficiary detail. {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> EditBeneficiary(string userEmail, BeneficiaryDetail beneficiary)
        {
            try
            {
                var caseTask = await _context.Investigations.Include(c => c.PolicyDetail).FirstOrDefaultAsync(m => m.Id == beneficiary.InvestigationTaskId);

                caseTask!.UpdatedBy = userEmail;
                caseTask.IsNew = true;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR;
                caseTask.IsReady2Assign = true;

                if (beneficiary?.ProfileImage != null)
                {
                    var (fileName, relativePath) = await _fileStorageService.SaveAsync(beneficiary.ProfileImage!, CONSTANTS.CASE, caseTask!.PolicyDetail!.ContractNumber);
                    beneficiary.ProfilePictureExtension = Path.GetExtension(fileName);
                    beneficiary.ImagePath = relativePath;
                }
                else
                {
                    var existingBeneficiary = await _context.BeneficiaryDetail.AsNoTracking().Where(c => c.BeneficiaryDetailId == beneficiary!.BeneficiaryDetailId).FirstOrDefaultAsync();
                    if (existingBeneficiary!.ImagePath != null)
                    {
                        beneficiary!.ImagePath = existingBeneficiary.ImagePath;
                    }
                }

                beneficiary!.PhoneNumber = beneficiary.PhoneNumber.TrimStart('0');
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                beneficiary.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(beneficiary.Name.ToLower()));
                beneficiary.CountryId = beneficiary.SelectedCountryId;
                beneficiary.StateId = beneficiary.SelectedStateId;
                beneficiary.DistrictId = beneficiary.SelectedDistrictId;
                beneficiary.PinCodeId = beneficiary.SelectedPincodeId;
                var pincode = await _context.PinCode.AsNoTracking().Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.PinCodeId == beneficiary.PinCodeId);
                var address = beneficiary.Addressline + ", " + pincode!.District!.Name + ", " + pincode.State!.Name + ", " + pincode.Country!.Code;
                var (Latitude, Longitude) = await _customApiClient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = Latitude + "," + Longitude;
                beneficiary.Latitude = Latitude;
                beneficiary.Longitude = Longitude;
                var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}", customerLatLong, EnvHelper.Get("GOOGLE_MAP_KEY"));
                beneficiary.BeneficiaryLocationMap = url;
                _context.BeneficiaryDetail.Attach(beneficiary);
                _context.Entry(beneficiary).State = EntityState.Modified;
                _context.Investigations.Update(caseTask);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred editing Beneficiary detail. {UserEmail}", userEmail);
                return false;
            }
        }
    }
}