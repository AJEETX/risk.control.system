using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Report
{
    public interface IInvoiceService
    {
        Task<VendorInvoice> GetInvoice(long id);
    }

    internal class InvoiceService(ApplicationDbContext context) : IInvoiceService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<VendorInvoice> GetInvoice(long id)
        {
            var invoice = await _context.VendorInvoice
              .Where(x => x.VendorInvoiceId.Equals(id))
              .Include(x => x.ClientCompany)
              .ThenInclude(c => c!.District)
              .Include(c => c.ClientCompany)
              .ThenInclude(c => c!.State)
              .Include(c => c.ClientCompany)
              .ThenInclude(c => c!.Country)
              .Include(c => c.ClientCompany)
              .ThenInclude(c => c!.PinCode)
              .Include(x => x.Vendor)
              .ThenInclude(v => v!.State)
              .Include(v => v.Vendor)
              .ThenInclude(v => v!.District)
              .Include(v => v.Vendor)
              .ThenInclude(v => v!.Country)
              .Include(i => i.InvestigationServiceType)
              .FirstOrDefaultAsync();

            return invoice!;
        }
    }
}