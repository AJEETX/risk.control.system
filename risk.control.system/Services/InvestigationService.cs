using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IInvestigationService
    {
        Task<int> GetAutoCount(string currentUserEmail);
        Task<object> GetAuto(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<object> GetActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<object> GetManagerActive(string currentUserEmail, int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc");
        Task<CaseTransactionModel> GetClaimDetails(string currentUserEmail, long id);
        List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors);
        Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id);
        Task<CaseTransactionModel> GetClaimPdfReport(string currentUserEmail, long id);
        Task<CaseTransactionModel> GetPdfReport(long id);
        Task<(object[], bool)> GetFilesData(string userEmail, bool isManager, int uploadId = 0);
        Task<(object, bool)> GetFileById(string userEmail, bool isManager, int uploadId);
    }
    internal class InvestigationService : IInvestigationService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;

        public InvestigationService(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
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
            if (claim.CustomerDetail != null)
            {
                var maskedCustomerContact = new string('*', claim.CustomerDetail.PhoneNumber.ToString().Length - 4) + claim.CustomerDetail.PhoneNumber.ToString().Substring(claim.CustomerDetail.PhoneNumber.ToString().Length - 4);
                claim.CustomerDetail.PhoneNumber = maskedCustomerContact;
            }
            if (claim.BeneficiaryDetail != null)
            {
                var maskedBeneficiaryContact = new string('*', claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + claim.BeneficiaryDetail.PhoneNumber.ToString().Substring(claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
                claim.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            }
            var companyUser = await context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - claim.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";
            //claim.PolicyDetail.DocumentPath = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
            //        Path.Combine(webHostEnvironment.ContentRootPath, claim.PolicyDetail.DocumentPath))));
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
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.InvestigationReport.EnquiryRequest)
                .Include(c => c.InvestigationReport.EnquiryRequests)
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

            if (claim.CustomerDetail != null)
            {
                var maskedCustomerContact = new string('*', claim.CustomerDetail.PhoneNumber.ToString().Length - 4) + claim.CustomerDetail.PhoneNumber.ToString().Substring(claim.CustomerDetail.PhoneNumber.ToString().Length - 4);
                claim.CustomerDetail.PhoneNumber = maskedCustomerContact;
            }
            if (claim.BeneficiaryDetail != null)
            {
                var maskedBeneficiaryContact = new string('*', claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + claim.BeneficiaryDetail.PhoneNumber.ToString().Substring(claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
                claim.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            }
            var companyUser = await context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();
            var endTIme = claim.Status == CONSTANTS.CASE_STATUS.FINISHED ? claim.ProcessedByAssessorTime.GetValueOrDefault() : DateTime.Now;
            var timeTaken = endTIme - claim.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == claim.InvestigationReportId);
            var templates = await context.ReportTemplates
               .Include(r => r.LocationReport)
                  .ThenInclude(l => l.AgentIdReport)
              .Include(r => r.LocationReport)
               .ThenInclude(l => l.MediaReports)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.FaceIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.DocumentIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.Questions)
                  .FirstOrDefaultAsync(q => q.Id == claim.ReportTemplateId);

            claim.InvestigationReport.ReportTemplate = templates;

            var tracker = await context.PdfDownloadTracker
                          .FirstOrDefaultAsync(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
            }

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (claim.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public async Task<CaseTransactionModel> GetClaimPdfReport(string currentUserEmail, long id)
        {
            var claim = await context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
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
            var maskedCustomerContact = new string('*', claim.CustomerDetail.PhoneNumber.ToString().Length - 4) + claim.CustomerDetail.PhoneNumber.ToString().Substring(claim.CustomerDetail.PhoneNumber.ToString().Length - 4);
            claim.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + claim.BeneficiaryDetail.PhoneNumber.ToString().Substring(claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            claim.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            var companyUser = await context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - claim.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == claim.InvestigationReportId);
            var templates = await context.ReportTemplates
               .Include(r => r.LocationReport)
                  .ThenInclude(l => l.AgentIdReport)
              .Include(r => r.LocationReport)
               .ThenInclude(l => l.MediaReports)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.FaceIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.DocumentIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.Questions)
                  .FirstOrDefaultAsync(q => q.Id == claim.ReportTemplateId);

            claim.InvestigationReport.ReportTemplate = templates;

            var tracker = await context.PdfDownloadTracker
                          .FirstOrDefaultAsync(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
                tracker.DownloadCount++;
                tracker.LastDownloaded = DateTime.UtcNow;
                context.PdfDownloadTracker.Update(tracker);
            }
            else
            {
                tracker = new PdfDownloadTracker
                {
                    ReportId = id,
                    UserEmail = currentUserEmail,
                    DownloadCount = 1,
                    LastDownloaded = DateTime.UtcNow
                };
                context.PdfDownloadTracker.Add(tracker);
            }
            context.SaveChanges();
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (claim.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public async Task<CaseTransactionModel> GetPdfReport(long id)
        {
            var claim = await context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
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
            var maskedCustomerContact = new string('*', claim.CustomerDetail.PhoneNumber.ToString().Length - 4) + claim.CustomerDetail.PhoneNumber.ToString().Substring(claim.CustomerDetail.PhoneNumber.ToString().Length - 4);
            claim.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + claim.BeneficiaryDetail.PhoneNumber.ToString().Substring(claim.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            claim.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - claim.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == claim.InvestigationReportId);
            var templates = await context.ReportTemplates
               .Include(r => r.LocationReport)
                  .ThenInclude(l => l.AgentIdReport)
              //.Include(r => r.LocationTemplate)
              // .ThenInclude(l => l.MediaReports)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.FaceIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.DocumentIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.Questions)
                  .FirstOrDefaultAsync(q => q.Id == claim.ReportTemplateId);

            claim.InvestigationReport.ReportTemplate = templates;
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = false,
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

                .Where(a => !a.Deleted &&
                    a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == currentUserEmail &&
                    (
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
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
                    a.CustomerDetail.PhoneNumber.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Code.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Name.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.PhoneNumber.ToLower().Contains(search));
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
                Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.CustomerDetail?.ImagePath)))) : Applicationsettings.NO_USER,
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
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-light\">beneficiary name</span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds,
                BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "?" : a.BeneficiaryDetail.Name,
                CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "?" : a.CustomerDetail.Name,
                PersonMapAddressUrl = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail) != "..." ?
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                        a.CustomerDetail.CustomerLocationMap :
                        a.BeneficiaryDetail.BeneficiaryLocationMap :
                        Applicationsettings.NO_MAP
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
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_IN_PROGRESS,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
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
                    a.CustomerDetail.PhoneNumber.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Code.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Name.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.PhoneNumber.ToLower().Contains(search));
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
                Agent = GetOwner(a),
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwnerImage(a))),
                CaseWithPerson = a.CaseOwner,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.CustomerDetail?.ImagePath)))) : Applicationsettings.NO_USER,
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
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.BeneficiaryDetail.ImagePath)))) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds, // Calculate here
                PersonMapAddressUrl = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ?
                string.Format(a.CustomerDetail.CustomerLocationMap, "400", "400") :
                string.Format(a.BeneficiaryDetail.BeneficiaryLocationMap, "400", "400")
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
                .Where(a => !a.Deleted && a.ClientCompanyId == companyUser.ClientCompanyId &&
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
                    a.CustomerDetail.PhoneNumber.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Code.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Name.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.Addressline.ToLower().Contains(search) ||
                    a.BeneficiaryDetail.PhoneNumber.ToLower().Contains(search));
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
                Document = a.PolicyDetail?.DocumentPath != null ?
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.PolicyDetail.DocumentPath)))) :
                Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.CustomerDetail?.ImagePath))))
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
                BeneficiaryPhoto = a.BeneficiaryDetail?.ImagePath != null
                        ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, a.BeneficiaryDetail?.ImagePath))))
                        : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name)
                        ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>"
                        : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds,
                IsNewAssigned = a.IsNewAssignedToManager,
                PersonMapAddressUrl = string.Format(a.GetMap(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.SubStatus == assignedToAssignerStatus,
                                                      a.SubStatus == submittedToAssessorStatus), "400", "400")
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

                await context.SaveChangesAsync(null, false); // mark as viewed
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
        private string GetOwner(InvestigationTask a)
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
            var subStatuses = new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                };

            var query = context.Investigations
                .Where(a => !a.Deleted &&
                    a.ClientCompanyId == companyUser.ClientCompanyId && subStatuses.Contains(a.SubStatus));

            int totalRecords = query.Count(); // Get total count before pagination
            return totalRecords;
        }

        public async Task<(object[], bool)> GetFilesData(string userEmail, bool isManager, int uploadId = 0)
        {
            var companyUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);

            var totalReadyToAssign = await GetAutoCount(userEmail);
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            if (uploadId > 0)
            {
                var file = await context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
                if (file == null)
                {
                    return (null, false);
                }
                if (file.ClaimsId != null && file.ClaimsId.Count > 0)
                {
                    totalReadyToAssign = totalReadyToAssign + file.ClaimsId.Count;
                }
            }

            var files = await context.FilesOnFileSystem.Where(f => f.CompanyId == companyUser.ClientCompanyId && ((f.UploadedBy == userEmail && !f.Deleted) || isManager && !f.Deleted)).ToListAsync();
            var result = files.OrderBy(o => o.CreatedOn).Select(file => new
            {
                file.Id,
                SequenceNumber = isManager ? file.CompanySequenceNumber : file.UserSequenceNumber,
                file.Name,
                file.Description,
                file.FileType,
                CreatedOn = file.CreatedOn.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm:ss"),
                file.UploadedBy,
                Status = file.Status,
                file.Message,
                //Message = file.Message == "Upload In progress" ? file.Icon : file.Message,
                Icon = file.Icon, // or use some other status representation
                IsManager = isManager,
                file.Completed,
                file.DirectAssign,
                hasError = (file.CompletedOn != null && file.ErrorByteData != null) ? true : false,
                errorLog = (file.CompletedOn != null && file.ErrorByteData != null) ? $"<a href='/Uploads/DownloadErrorLog/{file.Id}' class='btn-xs btn-danger'><i class='fa fa-download'></i> </a>" : "<i class='fas fa-sync fa-spin i-grey'></i>",
                UploadedType = file.DirectAssign ? "<i class='fas fa-random i-assign'></i>" : "<i class='fas fa-upload i-upload'></i>",
                TimeTaken = file.CompletedOn != null ?
                $" {(Math.Round((file.CompletedOn.Value - file.CreatedOn.Value).TotalSeconds) < 1 ?
                1 : Math.Round((file.CompletedOn.Value - file.CreatedOn.Value).TotalSeconds))} sec" : "<i class='fas fa-sync fa-spin i-grey'></i>",
            }).ToList();
            return (result.ToArray(), maxAssignReadyAllowedByCompany >= totalReadyToAssign);
        }

        public async Task<(object, bool)> GetFileById(string userEmail, bool isManager, int uploadId)
        {
            var companyUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
            var file = await context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            if (file == null)
            {
                return (null, false);
            }
            var totalReadyToAssign = await GetAutoCount(userEmail);
            var totalForAssign = totalReadyToAssign + file.ClaimsId?.Count;
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            var result = new
            {
                file.Id,
                SequenceNumber = isManager ? file.CompanySequenceNumber : file.UserSequenceNumber,
                file.Name,
                file.Description,
                file.FileType,
                CreatedOn = file.CreatedOn.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm:ss"),
                file.UploadedBy,
                Status = file.Status,
                file.Completed,
                Message = file.Message,
                Icon = file.Icon, // or use some other status representation
                IsManager = isManager,
                file.DirectAssign,
                hasError = (file.CompletedOn != null && file.ErrorByteData != null) ? true : false,
                errorLog = (file.CompletedOn != null && file.ErrorByteData != null) ? $"<a href='/Uploads/DownloadErrorLog/{file.Id}' class='btn-xs btn-danger'><i class='fa fa-download'></i> </a>" : "<i class='fas fa-sync fa-spin i-grey'></i>",
                UploadedType = file.DirectAssign ? "<i class='fas fa-random i-assign'></i>" : "<i class='fas fa-upload i-upload'></i>",
                TimeTaken = file.CompletedOn != null ? $" {(Math.Round((file.CompletedOn.Value - file.CreatedOn.Value).TotalSeconds) < 1 ? 1 :
                Math.Round((file.CompletedOn.Value - file.CreatedOn.Value).TotalSeconds))} sec" : "<i class='fas fa-sync fa-spin i-grey'></i>",
            };//<i class='fas fa-sync fa-spin'></i>
            return (new { result }, maxAssignReadyAllowedByCompany >= totalForAssign);
        }
    }
}
