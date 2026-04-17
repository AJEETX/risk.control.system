using Hangfire;

using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerativeService
    {
        Task<string> Generate(long investigationTaskId, string userEmail);
    }

    internal class PdfGenerativeService(ApplicationDbContext context, IPdfGenerateDetailService pdfGenerate) : IPdfGenerativeService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IPdfGenerateDetailService _pdfGenerate = pdfGenerate;

        [AutomaticRetry(Attempts = 0)]
        public async Task<string> Generate(long investigationTaskId, string userEmail)
        {
            var investigation = _context.Investigations.Include(c => c.CustomerDetail).Include(c => c.BeneficiaryDetail).Include(c => c.ClientCompany)
                    .ThenInclude(c => c!.Country).Include(c => c.PolicyDetail).Include(c => c.InvestigationReport).ThenInclude(c => c!.EnquiryRequest).Include(c => c.InvestigationReport).ThenInclude(c => c!.EnquiryRequests)
                .FirstOrDefault(c => c.Id == investigationTaskId);
            var policy = _context.PolicyDetail.Include(p => p.CaseEnabler).Include(p => p.CostCentre).Include(p => p.InvestigationServiceType)
                .FirstOrDefault(p => p.PolicyDetailId == investigation!.PolicyDetail!.PolicyDetailId);
            var customer = _context.CustomerDetail.Include(c => c.District).Include(c => c.State).Include(c => c.Country).Include(c => c.PinCode)
                .FirstOrDefault(c => c.InvestigationTaskId == investigationTaskId);
            var beneficiary = _context.BeneficiaryDetail.Include(b => b.District).Include(b => b.State).Include(b => b.Country).Include(b => b.PinCode).Include(b => b.BeneficiaryRelation)
                .FirstOrDefault(b => b.InvestigationTaskId == investigationTaskId);
            var investigationReport = await _context.ReportTemplates.Include(r => r.LocationReport).ThenInclude(l => l.AgentIdReport)
               .Include(r => r.LocationReport).ThenInclude(l => l.FaceIds).Include(r => r.LocationReport).ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationReport).ThenInclude(l => l.Questions).FirstOrDefaultAsync(q => q.Id == investigation!.ReportTemplateId);
            var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == investigation!.VendorId);
            var currentUser = _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var investigationServiced = vendor!.VendorInvestigationServiceTypes!.FirstOrDefault(s => s.InvestigationServiceTypeId == policy!.InvestigationServiceTypeId);
            investigationServiced ??= vendor.VendorInvestigationServiceTypes!.FirstOrDefault();
            var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == policy!.InvestigationServiceTypeId);
            var invoice = new VendorInvoice
            {
                ClientCompanyId = currentUser!.ClientCompany!.ClientCompanyId,
                GrandTotal = investigationServiced!.Price + (investigationServiced.Price * (1m / 10m)),
                NoteToRecipient = "Auto generated Invoice",
                Updated = DateTime.UtcNow,
                Vendor = vendor,
                ClientCompany = currentUser.ClientCompany,
                UpdatedBy = userEmail,
                VendorId = vendor.VendorId,
                InvestigationReportId = investigation!.InvestigationReport?.Id,
                SubTotal = investigationServiced.Price,
                TaxAmount = investigationServiced.Price * (1m / 10m),
                InvestigationServiceType = investigatService,
                CaseId = investigationTaskId,
                Currency = CustomExtensions.GetCultureByCountry(investigation.ClientCompany!.Country!.Code.ToUpper()).NumberFormat.CurrencySymbol
            };
            _context.VendorInvoice.Add(invoice);
            await _context.SaveChangesAsync(null, false);
            var reportFilename = await _pdfGenerate.BuildInvestigationPdfReport(investigation, policy!, customer!, beneficiary!, investigationReport!);
            _context.Investigations.Update(investigation);
            await _context.SaveChangesAsync(null, false);
            return policy!.ContractNumber;
        }
    }
}