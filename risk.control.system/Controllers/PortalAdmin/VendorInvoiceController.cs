using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers.PortalAdmin
{

    public class VendorInvoiceController : Controller
    {
        private readonly ApplicationDbContext _context; // Replace with your actual DbContext name

        public VendorInvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            // Build the strongly-typed ViewModel instead of using ViewBag
            var viewModel = new VendorInvoiceIndexViewModel
            {
                VendorList = await _context.Vendor // Assuming your DbSet is called Vendor
                    .AsNoTracking()
                    .OrderBy(v => v.Name)
                    .Select(v => new SelectListItem
                    {
                        Value = v.VendorId.ToString(),
                        Text = v.Name
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> GetInvoicesJson()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            // EXTRACTION: Fetch the custom dynamic filter value from DataTables payload
            var selectedVendorIdRaw = Request.Form["selectedVendorId"].FirstOrDefault();
            long? filterVendorId = !string.IsNullOrEmpty(selectedVendorIdRaw) ? Convert.ToInt64(selectedVendorIdRaw) : null;

            var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
            var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var sortColumn = Request.Form[$"columns[{sortColumnIndex}][name]"].FirstOrDefault() ?? "VendorInvoiceId";

            int pageSize = length != null ? Convert.ToInt32(length) : 10;
            int skip = start != null ? Convert.ToInt32(start) : 0;

            // Base Query
            var query = _context.VendorInvoice
                .Include(v => v.Vendor)
                .Include(v => v.ClientCompany)
                .AsNoTracking();

            // 2b. Apply Strongly-Typed Structural Dropdown Filters BEFORE total/filtered counts
            if (filterVendorId.HasValue)
            {
                query = query.Where(v => v.VendorId == filterVendorId.Value);
            }

            int totalRecords = await query.CountAsync();

            // Global Search/Filtering
            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(v => v.InvoiceNumber.Contains(searchValue) ||
                                         (v.Vendor != null && v.Vendor.Name.Contains(searchValue)) ||
                                         (v.ClientCompany != null && v.ClientCompany.Name.Contains(searchValue)));
            }

            int filteredRecords = await query.CountAsync();

            // Apply Sorting (Map with your existing Switch logic)
            if (sortDirection == "desc")
            {
                query = sortColumn switch
                {
                    "InvoiceNumber" => query.OrderByDescending(v => v.InvoiceNumber),
                    "InvoiceDate" => query.OrderByDescending(v => v.InvoiceDate),
                    "DueDate" => query.OrderByDescending(v => v.DueDate),
                    "GrandTotal" => query.OrderByDescending(v => v.GrandTotal),
                    "VendorName" => query.OrderByDescending(v => v.Vendor != null ? v.Vendor.Name : ""),
                    "ClientName" => query.OrderByDescending(v => v.ClientCompany != null ? v.ClientCompany.Name : ""),
                    _ => query.OrderByDescending(v => v.VendorInvoiceId)
                };
            }
            else
            {
                query = sortColumn switch
                {
                    "InvoiceNumber" => query.OrderBy(v => v.InvoiceNumber),
                    "InvoiceDate" => query.OrderBy(v => v.InvoiceDate),
                    "DueDate" => query.OrderBy(v => v.DueDate),
                    "GrandTotal" => query.OrderBy(v => v.GrandTotal),
                    "VendorName" => query.OrderBy(v => v.Vendor != null ? v.Vendor.Name : ""),
                    "ClientName" => query.OrderBy(v => v.ClientCompany != null ? v.ClientCompany.Name : ""),
                    _ => query.OrderBy(v => v.VendorInvoiceId)
                };
            }

            // Apply Paging
            var data = await query.Skip(skip).Take(pageSize)
                .Select(v => new
                {
                    v.VendorInvoiceId,
                    v.InvoiceNumber,
                    InvoiceDate = v.InvoiceDate.ToString("yyyy-MM-dd"),
                    DueDate = v.DueDate.ToString("yyyy-MM-dd"),
                    ClientName = v.ClientCompany != null ? v.ClientCompany.Name : "--",
                    VendorName = v.Vendor != null ? v.Vendor.Name : "--",
                    v.GrandTotal,
                    v.Currency
                })
                .ToListAsync();

            return Json(new { draw = draw, recordsFiltered = filteredRecords, recordsTotal = totalRecords, data = data });
        }
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendorInvoice = await _context.VendorInvoice
                .Include(v => v.Vendor)
                .Include(v => v.ClientCompany)
                .Include(v => v.InvestigationReport)
                .Include(v => v.InvestigationReport!.ReportTemplate)
                .Include(v => v.InvestigationServiceType)
                .FirstOrDefaultAsync(m => m.VendorInvoiceId == id);
            if (vendorInvoice == null)
            {
                return NotFound();
            }
            var caseTask = await _context.Investigations.Include(i => i.PolicyDetail).FirstOrDefaultAsync(c => c.Id == vendorInvoice!.CaseId);
            var model = new InvoiceDetail
            {
                VendorInvoice = vendorInvoice,
                ContractNumber = caseTask!.PolicyDetail!.ContractNumber
            };
            return View(model);
        }
    }
}
