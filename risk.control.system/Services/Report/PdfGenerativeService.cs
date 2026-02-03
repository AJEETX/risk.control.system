using System.Composition;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;


namespace risk.control.system.Services.Report
{
    public interface IPdfGenerativeService
    {
        Task<string> Generate(long investigationTaskId, string userEmail);
        //Task<InvestigationTask> GeneratePdf(long investigationTaskId, string userEmail);

    }
    internal class PdfGenerativeService : IPdfGenerativeService
    {

        private readonly ApplicationDbContext context;
        private readonly IPdfGenerateDetailService pdfGenerate;
        private readonly IInvestigationReportPdfService generateReport;

        public PdfGenerativeService(ApplicationDbContext context, IPdfGenerateDetailService pdfGenerate, IInvestigationReportPdfService generateReport)
        {
            this.context = context;
            this.pdfGenerate = pdfGenerate;
            this.generateReport = generateReport;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task<string> Generate(long investigationTaskId, string userEmail)
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

            var reportFilename = await pdfGenerate.BuildInvestigationPdfReport(investigation, policy, customer, beneficiary, investigationReport);

            return reportFilename;
        }

        //QUESTPDF
        //[AutomaticRetry(Attempts = 0)]
        //public async Task<InvestigationTask> GeneratePdf(long taskId, string userEmail)
        //{
        //    try
        //    {
        //        var task = context.Investigations
        //           .Include(x => x.PolicyDetail)
        //           .Include(x => x.CustomerDetail)
        //           .Include(x => x.BeneficiaryDetail)
        //           .Include(x => x.ClientCompany)
        //           .Include(x => x.Vendor)
        //           .Include(x => x.InvestigationReport)
        //           .First(x => x.Id == taskId);

        //        var investigationReport = await context.ReportTemplates
        //           .Include(r => r.LocationReport)
        //              .ThenInclude(l => l.AgentIdReport)
        //          .Include(r => r.LocationReport)
        //              .ThenInclude(l => l.FaceIds)
        //          .Include(r => r.LocationReport)
        //              .ThenInclude(l => l.DocumentIds)
        //          .Include(r => r.LocationReport)
        //              .ThenInclude(l => l.Questions)
        //              .FirstOrDefaultAsync(q => q.Id == task.ReportTemplateId);

        //        var savedTask = generateReport.SaveReport(task, investigationReport);
        //        context.Investigations.Update(savedTask);
        //        await context.SaveChangesAsync();
        //        return savedTask;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error generating PDF for Task ID {taskId}: {ex.Message}");
        //        throw;
        //    }
           
        //}
    }
}
