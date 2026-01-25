using System.Globalization;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
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
        Task<CaseTransactionModel> GetCaseDetails(string currentUserEmail, long id);
        List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors);
        Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id);
        Task<CaseTransactionModel> GetClaimPdfReport(string currentUserEmail, long id);
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

        public async Task<CaseTransactionModel> GetCaseDetails(string currentUserEmail, long id)
        {
            var caseTask = await context.Investigations
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
            if (caseTask.CustomerDetail != null)
            {
                var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
                caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            }
            if (caseTask.BeneficiaryDetail != null)
            {
                var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
                caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            }
            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Location = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                TimeTaken = totalTimeTaken,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public async Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id)
        {
            var caseTask = await context.Investigations
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

            if (caseTask.CustomerDetail != null)
            {
                var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
                caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            }
            if (caseTask.BeneficiaryDetail != null)
            {
                var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
                caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            }
            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();
            var endTIme = caseTask.Status == CONSTANTS.CASE_STATUS.FINISHED ? caseTask.ProcessedByAssessorTime.GetValueOrDefault() : DateTime.Now;
            var timeTaken = endTIme - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == caseTask.InvestigationReportId);
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
                  .FirstOrDefaultAsync(q => q.Id == caseTask.ReportTemplateId);

            caseTask.InvestigationReport.ReportTemplate = templates;

            var tracker = await context.PdfDownloadTracker
                          .FirstOrDefaultAsync(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
            }

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Location = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public async Task<CaseTransactionModel> GetClaimPdfReport(string currentUserEmail, long id)
        {
            var caseTask = await context.Investigations
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
            var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
            caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.Now - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == caseTask.InvestigationReportId);
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
                  .FirstOrDefaultAsync(q => q.Id == caseTask.ReportTemplateId);

            caseTask.InvestigationReport.ReportTemplate = templates;

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
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Location = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
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
            // 1. Minimal User Context Fetch
            var companyUser = await context.ApplicationUser.Include(c=>c.Country).Include(c=>c.ClientCompany)
                .Where(c => c.Email == currentUserEmail)
                .FirstOrDefaultAsync();

            // 2. Base Query (Keep as IQueryable)
            var query = context.Investigations.Where(a => !a.Deleted && a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == currentUserEmail);
            
            int totalRecords = await query.CountAsync();

            query = query.Where(a => CONSTANTS.CreatedAndDraftStatuses.Contains(a.SubStatus));


            // Search filtering
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower(CultureInfo.InvariantCulture);
                query = query.Where(a =>
                    a.PolicyDetail.ContractNumber.ToLower().Contains(search) ||
                    a.PolicyDetail.CauseOfLoss.ToLower().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.ToLower().Contains(search) ||
                    a.PolicyDetail.InvestigationServiceType.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.DateOfBirth.ToString().ToLower().Contains(search) ||
                    a.CustomerDetail.Name.ToLower().Contains(search) ||
                    a.CustomerDetail.PhoneNumber.ToLower().Contains(search) ||
                    a.CustomerDetail.PinCode.Code.ToString().Contains(search) ||
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

            // 4. Apply Sorting (In SQL)
            bool isAsc = string.Equals(orderDir, "asc", StringComparison.OrdinalIgnoreCase);
            query = orderColumn switch
            {
                1 => isAsc ? query.OrderBy(a => a.PolicyDetail.ContractNumber) : query.OrderByDescending(a => a.PolicyDetail.ContractNumber),
                2 => isAsc ? query.OrderBy(a => (double)a.PolicyDetail.SumAssuredValue) : query.OrderByDescending(a => (double)a.PolicyDetail.SumAssuredValue),
                3 => isAsc
                    ? query.OrderBy(a =>
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                            ? a.CustomerDetail.PinCode.Code
                            : a.BeneficiaryDetail.PinCode.Code)
                    : query.OrderByDescending(a =>
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                            ? a.CustomerDetail.PinCode.Code
                            : a.BeneficiaryDetail.PinCode.Code),
                4 => isAsc ? query.OrderBy(a => a.ClientCompany.Name) : query.OrderByDescending(a => a.ClientCompany.Name),
                5 => isAsc ? query.OrderBy(a => a.PolicyDetail.InsuranceType) : query.OrderByDescending(a => a.PolicyDetail.InsuranceType),
                6 => isAsc ? query.OrderBy(a => a.CustomerDetail.Name) : query.OrderByDescending(a => a.CustomerDetail.Name),
                7 => isAsc ? query.OrderBy(a => a.Status) : query.OrderByDescending(a => a.Status),
                8 => isAsc ? query.OrderBy(a => a.BeneficiaryDetail.Name) : query.OrderByDescending(a => a.BeneficiaryDetail.Name),
                9 => isAsc ? query.OrderBy(a => a.PolicyDetail.InvestigationServiceType.Name) : query.OrderByDescending(a => a.PolicyDetail.InvestigationServiceType.Name),
                10 => isAsc ? query.OrderBy(a => a.ORIGIN) : query.OrderByDescending(a => a.ORIGIN),
                11 => isAsc ? query.OrderBy(a => a.Created) : query.OrderByDescending(a => a.Created),
                12 => isAsc ? query.OrderBy(a => a.Updated) : query.OrderByDescending(a => a.Updated),
                // Default fallback (Usually ID or Created Date)
                _ => isAsc ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id)
            };
            // 5. Paginate and Project (Only fetch what you need)
            var pagedRawData = await query
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
                .Skip(start)
                .Take(length)
                .ToListAsync();
            int recordsFiltered = await query.CountAsync();
            

            var finalDataTasks = pagedRawData.Select(async a =>
            {
                var isUnderwriting = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;
                var culture = Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper());

                // Run file operations in parallel for this specific row
                var documentTask = GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var customerTask = GetBase64FileAsync(a.CustomerDetail?.ImagePath, Applicationsettings.NO_USER);
                var beneficiaryTask = GetBase64FileAsync(a.BeneficiaryDetail?.ImagePath, Applicationsettings.NO_USER);

                await Task.WhenAll(documentTask, customerTask, beneficiaryTask);
                var policyId = a.PolicyDetail.ContractNumber;
                var amount = string.Format(culture, "{0:C}", a.PolicyDetail.SumAssuredValue);
                var pincodeCode = ClaimsInvestigationExtension.GetPincodeCode(isUnderwriting, a.CustomerDetail, a.BeneficiaryDetail);
                var customerName = a.CustomerDetail?.Name ?? "<span class=\"badge badge-light\">customer name</span>";
                var policyName = a.PolicyDetail.InsuranceType.GetEnumDisplayName();
                var Origin = a.ORIGIN.GetEnumDisplayName();
                var ready2Assign = a.IsValidCaseData();
                var investigationService = a.PolicyDetail.InvestigationServiceType.Name;
                var serviceType = $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({investigationService})";
                var timePending = GetDraftedTimePending(a);
                var policyNumber = a.GetPolicyNum();
                var beneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-light\">beneficiary name</span>" : a.BeneficiaryDetail.Name;
                var timeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds;
                var personMapAddressUrl = GetMapUrl(a);
                return new CaseAutoAllocationResponse
                {
                    Id = a.Id,
                    IsNew = a.IsNew,
                    Amount = amount,
                    PolicyId = policyId,
                    AssignedToAgency = a.IsNew,
                    PincodeCode = pincodeCode,
                    Document = await documentTask,
                    Customer = await customerTask,
                    Name = customerName,
                    Policy = policyName,
                    IsUploaded = a.IsUploaded,
                    Origin = Origin,
                    SubStatus = a.SubStatus,
                    Ready2Assign = ready2Assign,
                    Service = investigationService,
                    Location = a.ORIGIN.GetEnumDisplayName(),
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    ServiceType = serviceType,
                    TimePending = timePending,
                    PolicyNum = policyNumber,
                    BeneficiaryPhoto = await beneficiaryTask,
                    BeneficiaryName = beneficiaryName,
                    TimeElapsed = timeElapsed,
                    BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? "?",
                    CustomerFullName = a.CustomerDetail?.Name ?? "?",
                    PersonMapAddressUrl = personMapAddressUrl
                };
            });

            // 2. Await all rows
            List<CaseAutoAllocationResponse> finalData = (await Task.WhenAll(finalDataTasks)).ToList();

            // 7. Bulk Update "IsNew" (Fastest way)
            var idsToUpdate = finalData.Where(x => x.IsNew).Select(x => x.Id).ToList();
            if (idsToUpdate.Any())
            {
                await context.Investigations
                    .Where(x => idsToUpdate.Contains(x.Id))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.IsNew, false));
            }

            return new
            {
                draw,
                AutoAllocatopn = companyUser.ClientCompany.AutoAllocation,
                recordsTotal = totalRecords,
                recordsFiltered,
                data = finalData
            };
        }
        private async Task<string> GetBase64FileAsync(string relativePath, string fallback = "")
        {
            if (string.IsNullOrEmpty(relativePath)) return fallback;

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, relativePath);
            if (!File.Exists(fullPath)) return fallback;

            // Use the async version of file reading
            byte[] bytes = await File.ReadAllBytesAsync(fullPath);
            return $"data:image/*;base64,{Convert.ToBase64String(bytes)}";
        }
        private static string GetMapUrl(InvestigationTask a)
        {
            var pinName = ClaimsInvestigationExtension.GetPincodeName(
                a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail);

            if (pinName == "...") return Applicationsettings.NO_MAP;

            return a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                ? a.CustomerDetail.CustomerLocationMap
                : a.BeneficiaryDetail.BeneficiaryLocationMap;
        }
        private static string GetDraftedTimePending(InvestigationTask a)
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
            var companyUser = await context.ApplicationUser
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
                .Where(a => !a.Deleted && a.CreatedUser == currentUserEmail);

            query = query.Where(q => q.Status == CONSTANTS.CASE_STATUS.INPROGRESS && !subStatus.Contains(q.SubStatus));
            
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
                    a.CustomerDetail.PinCode.Code.ToString().Contains(search) ||
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

            int recordsFiltered = query.Count();

            bool isAsc = orderDir == "asc";
            query = orderColumn switch
            {
                0 => isAsc ? query.OrderBy(a => a.PolicyDetail.ContractNumber) : query.OrderByDescending(a => a.PolicyDetail.ContractNumber),
                1 => isAsc ? query.OrderBy(a => (double)a.PolicyDetail.SumAssuredValue) : query.OrderByDescending(a => (double)a.PolicyDetail.SumAssuredValue),
                3 => isAsc
                    ? query.OrderBy(a =>
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                            ? a.CustomerDetail.PinCode.Code
                            : a.BeneficiaryDetail.PinCode.Code)
                    : query.OrderByDescending(a =>
                        a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                            ? a.CustomerDetail.PinCode.Code
                            : a.BeneficiaryDetail.PinCode.Code),
                6 => isAsc ? query.OrderBy(a => a.CustomerDetail.Name) : query.OrderByDescending(a => a.CustomerDetail.Name),
                8 => isAsc ? query.OrderBy(a => a.BeneficiaryDetail.Name) : query.OrderByDescending(a => a.BeneficiaryDetail.Name),
                9 => isAsc ? query.OrderBy(a => $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})") : query.OrderByDescending(a => $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})"),
                10 => isAsc ? query.OrderBy(a => a.SubStatus) : query.OrderByDescending(a => a.SubStatus),
                11 => isAsc ? query.OrderBy(a => a.Created) : query.OrderByDescending(a => a.Created),
                _ =>  isAsc ? query.OrderBy(a => a.Updated) : query.OrderByDescending(a => a.Updated)
                };

            var pagedList = await query.Skip(start).Take(length).ToListAsync();

            // 6. Transform & Async File I/O
            var culture = Extensions.GetCultureByCountry(companyUser.Country.Code);

            var finalTasks = pagedList.Select(async a => {
                var isUW = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;

                // Fetch files in parallel for this row
                var docTask = GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                var custTask = GetBase64FileAsync(a.CustomerDetail.ImagePath, Applicationsettings.NO_USER);
                var beneTask = GetBase64FileAsync(a.BeneficiaryDetail.ImagePath, Applicationsettings.NO_USER);
                var ownerImageTask = GetOwnerImage(a.Id);
                var ownerDetailTask = GetOwner(a.Id);
                var policyNumber = a.GetPolicyNum();
                var investigationService = a.PolicyDetail.InvestigationServiceType.Name;
                await Task.WhenAll(docTask, custTask, beneTask, ownerImageTask, ownerDetailTask);
                return new ActiveCaseResponse
                {
                    PolicyNum = policyNumber,
                    AutoAllocated = a.IsAutoAllocated,
                    Id = a.Id,
                    IsNew = a.IsNew,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                    CustomerFullName = a.CustomerDetail.Name ?? "",
                    BeneficiaryFullName = a.BeneficiaryDetail.Name?? "",
                    Document = await docTask,
                    Customer = await custTask,
                    AssignedToAgency = a.AssignedToAgency,
                    Agent = (await ownerDetailTask),
                    OwnerDetail = (await ownerImageTask),
                    CaseWithPerson = a.CaseOwner,
                    BeneficiaryPhoto = await beneTask,
                    SubStatus = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    Location = a.SubStatus,
                    TimePending = a.GetCreatorTimePending(),
                    TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds,
                    Service = investigationService,
                    ServiceType = $"{a.PolicyDetail.InsuranceType.GetEnumDisplayName()} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    PersonMapAddressUrl = isUW ? string.Format(a.CustomerDetail.CustomerLocationMap, "400", "400") : string.Format(a.BeneficiaryDetail.BeneficiaryLocationMap, "400", "400"),
                    Pincode = ClaimsInvestigationExtension.GetPincode(isUW, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(isUW, a.CustomerDetail, a.BeneficiaryDetail),
                    Name = a.CustomerDetail.Name?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>",
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Status = a.ORIGIN.GetEnumDisplayName(),
                };
            });

            var data = await Task.WhenAll(finalTasks);

            // 7. Bulk Update Viewed Status
            var idsToUpdate = data.Where(x => x.IsNew).Select(x => x.Id).ToList();
            if (idsToUpdate.Any())
            {
                await context.Investigations
                    .Where(x => idsToUpdate.Contains(x.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsNew, false));
            }

            return new 
            { 
                draw, 
                recordsTotal= totalRecords, 
                recordsFiltered, 
                data 
            };
        }
        private async Task<string> GetOwnerImage(long id)
        {
            string base64StringImage;
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
            var caseTask =await  context.Investigations.FirstOrDefaultAsync(c => c.Id == id);
            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR || caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                var agencyUser =await context.Vendor.FirstOrDefaultAsync(u => u.VendorId == caseTask.VendorId);
                if (agencyUser != null && !string.IsNullOrWhiteSpace(agencyUser.DocumentUrl))
                {
                    var agentImagePath = Path.Combine(webHostEnvironment.ContentRootPath, agencyUser.DocumentUrl);
                    var agentImageByte =await System.IO.File.ReadAllBytesAsync(agentImagePath);
                    return string.Format("data:image/*;base64,{0}", Convert.ToBase64String(agentImageByte));
                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                var agent =await context.ApplicationUser.FirstOrDefaultAsync(v => v.Email == caseTask.TaskedAgentEmail);
                if (agent != null && !string.IsNullOrWhiteSpace(agent.ProfilePictureUrl))
                {
                    var agentImagePath = Path.Combine(webHostEnvironment.ContentRootPath, agent.ProfilePictureUrl);
                    var agentImageByte =  await File.ReadAllBytesAsync(agentImagePath);
                    return string.Format("data:image/*;base64,{0}", Convert.ToBase64String(agentImageByte));

                }
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                )
            {
                var company =await context.ClientCompany.FirstOrDefaultAsync(v => v.ClientCompanyId == caseTask.ClientCompanyId);
                if (company != null && !string.IsNullOrWhiteSpace(company.DocumentUrl))
                {
                    var companyImagePath = Path.Combine(webHostEnvironment.ContentRootPath, company.DocumentUrl);
                    var companyImageByte = await File.ReadAllBytesAsync(companyImagePath);
                    return string.Format("data:image/*;base64,{0}", Convert.ToBase64String(companyImageByte));
                }
            }
            return noDataImagefilePath;
        }
        private async Task< string> GetOwner(long caseId)
        {
            var caseTask = await context.Investigations.FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                return caseTask.Vendor.Email;
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                return caseTask.TaskedAgentEmail;
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                )
            {
                var company =await context.ClientCompany.FirstOrDefaultAsync(v => v.ClientCompanyId == caseTask.ClientCompanyId);
                if (company != null)
                {
                    return company.Email;
                }
            }
            return string.Empty;
        }
        public async Task<int> GetAutoCount(string currentUserEmail)
        {
            var companyUser = await context.ApplicationUser.Include(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);

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

            var query = context.Investigations.Where(a => !a.Deleted &&a.ClientCompanyId == companyUser.ClientCompanyId && subStatuses.Contains(a.SubStatus));

            return await query.CountAsync(); // Get total count before pagination
        }

        public async Task<(object[], bool)> GetFilesData(string userEmail, bool isManager, int uploadId = 0)
        {
            var companyUser = await context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);

            var totalReadyToAssign = await GetAutoCount(userEmail);
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            if (uploadId > 0)
            {
                var file = await context.FilesOnFileSystem.Include(c => c.CaseIds).FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
                if (file == null)
                {
                    return (null, false);
                }
                if (file.CaseIds != null && file.CaseIds.Count > 0)
                {
                    totalReadyToAssign += file.CaseIds.Count;
                }
            }

            var files = await context.FilesOnFileSystem.Where(f => f.CompanyId == companyUser.ClientCompanyId && ((f.UploadedBy == userEmail && !f.Deleted) || (isManager && !f.Deleted))).ToListAsync();
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
                file.RecordCount,
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
            var companyUser = await context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
            var file = await context.FilesOnFileSystem.Include(c=>c.CaseIds).FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            if (file == null)
            {
                return (null!, false);
            }
            var totalReadyToAssign = await GetAutoCount(userEmail);
            var totalForAssign = totalReadyToAssign + file.CaseIds?.Count;
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
                file.RecordCount,
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
