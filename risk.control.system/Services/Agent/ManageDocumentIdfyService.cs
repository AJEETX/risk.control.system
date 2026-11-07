using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent
{
    public interface IManageDocumentIdfyService
    {
        Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data);
    }
    internal class ManageDocumentIdfyService(
        ApplicationDbContext context,
        IDocumentIdfyService documentIdfyService,
        IKycDocumentIdfyService kycDocumentIdfyService,
    IFileStorageService fileStorageService,
    ILogger<ManageDocumentIdfyService> logger,
    IAgentCaseDetailService caseService) : IManageDocumentIdfyService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IAgentCaseDetailService _caseService = caseService;
        private readonly ILogger<ManageDocumentIdfyService> _logger = logger;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly IDocumentIdfyService _documentIdfyService = documentIdfyService;
        private readonly IKycDocumentIdfyService _kycDocumentIdfyService = kycDocumentIdfyService;

        public async Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data)
        {
            try
            {
                var caseDetail = await _caseService.GetCaseById(data.CaseId);
                if (caseDetail?.InvestigationReport == null)
                {
                    _logger.LogError("Case not found for CaseId {CaseId}", data.CaseId);
                    return new AppiCheckifyResponse
                    {
                        BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                        Valid = false,
                        LocationLongLat = "No Data",
                        LocationTime = DateTime.UtcNow
                    };
                }

                var locationRecord = caseDetail.InvestigationReport.ReportTemplate!.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);
                if (locationRecord == null)
                {
                    _logger.LogError("Location not found for Case {CaseId}, and LocationName {LocationName}", data.CaseId, data.LocationName);
                    return new AppiCheckifyResponse
                    {
                        BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                        Valid = false,
                        LocationLongLat = "No Data",
                        LocationTime = DateTime.UtcNow
                    };
                }

                var locationTemplate = await _context.LocationReport.Include(l => l.DocumentIds)!.ThenInclude(d => d.DocumentFiles).FirstOrDefaultAsync(l => l.Id == locationRecord!.Id);
                if (locationTemplate == null)
                {
                    _logger.LogError("Location template not found for Case {CaseId}, and LocationName {LocationName}", data.CaseId, data.LocationName);
                    return new AppiCheckifyResponse
                    {
                        BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                        Valid = false,
                        LocationLongLat = "No Data",
                        LocationTime = DateTime.UtcNow
                    };
                }

                var documentReport = locationTemplate!.DocumentIds!.FirstOrDefault(c => c.ReportName == data.ReportName);
                if (documentReport == null)
                {
                    _logger.LogError("Document report not found for Case {CaseId}, LocationName {LocationName}, and ReportName {ReportName}", data.CaseId, data.LocationName, data.ReportName);
                    return new AppiCheckifyResponse
                    {
                        BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                        Valid = false,
                        LocationLongLat = "No Data",
                        LocationTime = DateTime.UtcNow
                    };
                }
                if (documentReport.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName() ||
                    documentReport.ReportName == DocumentIdReportType.ADHAAR.GetEnumDisplayName() ||
                    documentReport.ReportName == DocumentIdReportType.PASSPORT.GetEnumDisplayName() ||
                    documentReport.ReportName == DocumentIdReportType.DRIVING_LICENSE.GetEnumDisplayName() ||
                    documentReport.ReportName == DocumentIdReportType.VOTER_ID.GetEnumDisplayName())
                {
                    var response = await _kycDocumentIdfyService.CaptureDocumentId(data, caseDetail, documentReport, locationTemplate);
                    return response;
                }
                else
                {
                    var response = await _documentIdfyService.CaptureDocumentId(data, caseDetail, documentReport, locationTemplate);
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
                throw;
            }
        }
    }
}
