using Hangfire;

using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;


namespace risk.control.system.Services
{
    public interface IPdfGenerativeService
    {
        Task<string> Generate(long investigationTaskId, string userEmail = "assessor@insurer.com");
    }
    internal class PdfGenerativeService : IPdfGenerativeService
    {

        private readonly ApplicationDbContext context;
        private readonly IPdfGenerateDetailService pdfGenerate;

        public PdfGenerativeService(ApplicationDbContext context, IPdfGenerateDetailService pdfGenerate)
        {
            this.context = context;
            this.pdfGenerate = pdfGenerate;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task<string> Generate(long investigationTaskId, string userEmail = "assessor@insurer.com")
        {
            var investigation = context.Investigations
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .Include(c => c.ClientCompany)
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.InvestigationReport)
                    .ThenInclude(c => c.EnquiryRequests)
                .FirstOrDefault(c => c.Id == investigationTaskId);

            var policy = context.PolicyDetail
                .Include(p => p.CaseEnabler)
                .Include(p => p.CostCentre)
                .Include(p => p.InvestigationServiceType)
                .FirstOrDefault(p => p.PolicyDetailId == investigation.PolicyDetail.PolicyDetailId);

            var customer = context.CustomerDetail
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .FirstOrDefault(c => c.InvestigationTaskId == investigationTaskId);

            var beneficiary = context.BeneficiaryDetail
                .Include(b => b.District)
                .Include(b => b.State)
                .Include(b => b.Country)
                .Include(b => b.PinCode)
                .Include(b => b.BeneficiaryRelation)
                .FirstOrDefault(b => b.InvestigationTaskId == investigationTaskId);

            var investigationReport = await context.ReportTemplates
                .Include(r => r.LocationReport)
                   .ThenInclude(l => l.AgentIdReport)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.Questions)
                   .FirstOrDefaultAsync(q => q.Id == investigation.ReportTemplateId);
            var vendor = context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == investigation.VendorId);
            var currentUser = context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == policy.InvestigationServiceTypeId);
            if (investigationServiced == null)
            {
                investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
            }
            var investigatService = context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == policy.InvestigationServiceTypeId);

            var invoice = new VendorInvoice
            {
                ClientCompanyId = currentUser.ClientCompany.ClientCompanyId,
                GrandTotal = investigationServiced.Price + investigationServiced.Price * (1 / 10),
                NoteToRecipient = "Auto generated Invoice",
                Updated = DateTime.Now,
                Vendor = vendor,
                ClientCompany = currentUser.ClientCompany,
                UpdatedBy = userEmail,
                VendorId = vendor.VendorId,
                InvestigationReportId = investigation.InvestigationReport?.Id,
                SubTotal = investigationServiced.Price,
                TaxAmount = investigationServiced.Price * (1m / 10m),
                InvestigationServiceType = investigatService,
                ClaimId = investigationTaskId
            };

            context.VendorInvoice.Add(invoice);
            await context.SaveChangesAsync(null, false);
            //var reportFilename = await pdfGenerate.BuildInvestigationPdfReport(investigation, policy, customer, beneficiary, investigationReport);

            return "reportFilename";
        }
    }
}
