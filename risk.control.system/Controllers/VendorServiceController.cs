using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class VendorServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VendorServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: VendorService
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.VendorInvestigationServiceType
                .Include(v => v.InvestigationServiceType)
                .Include(v => v.LineOfBusiness)
                .Include(v => v.PincodeServices)
                .Include(v => v.State)
                .Include(v => v.Vendor);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: VendorService/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.VendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                .Include(v => v.InvestigationServiceType)
                .Include(v => v.LineOfBusiness)
                .Include(v => v.PincodeServices)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
            if (vendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            return View(vendorInvestigationServiceType);
        }

        // GET: VendorService/Create
        public IActionResult Create(string id)
        {
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == id);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name");
            ViewBag.VendorName = vendor.Name;
            ViewBag.VendorId = vendor.VendorId;
            return View();
        }

        // POST: VendorService/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType vendorInvestigationServiceType)
        {
            if (vendorInvestigationServiceType is not null)
            {
                _context.Add(vendorInvestigationServiceType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendorInvestigationServiceType.StateId);
            ViewData["VendorId"] = new SelectList(_context.Vendor, "VendorId", "Name", vendorInvestigationServiceType.VendorId);
            return View(vendorInvestigationServiceType);
        }
        [HttpPost, ActionName("GetInvestigationServicesByLineOfBusinessId")]
        public async Task<JsonResult> GetInvestigationServicesByLineOfBusinessId(string LineOfBusinessId)
        {
            string lId;
            var services = new List<InvestigationServiceType>();
            if (!string.IsNullOrEmpty(LineOfBusinessId))
            {
                lId = LineOfBusinessId;
                services = await _context.InvestigationServiceType.Where(s => s.LineOfBusiness.LineOfBusinessId.Equals(lId)).ToListAsync();
            }
            return Json(services);
        }
        // GET: VendorService/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.VendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
            if (vendorInvestigationServiceType == null)
            {
                return NotFound();
            }
            var services = _context.VendorInvestigationServiceType.Include(v => v.Vendor).First(v => v.VendorInvestigationServiceTypeId == id);

            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendorInvestigationServiceType.StateId);
            ViewData["VendorId"] = new SelectList(_context.Vendor, "VendorId", "Name", vendorInvestigationServiceType.VendorId);
            return View(services);
        }

        // POST: VendorService/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, VendorInvestigationServiceType vendorInvestigationServiceType)
        {
            if (id != vendorInvestigationServiceType.VendorInvestigationServiceTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vendorInvestigationServiceType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorInvestigationServiceTypeExists(vendorInvestigationServiceType.VendorInvestigationServiceTypeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendorInvestigationServiceType.StateId);
            ViewData["VendorId"] = new SelectList(_context.Vendor, "VendorId", "Name", vendorInvestigationServiceType.VendorId);
            return View(vendorInvestigationServiceType);
        }

        // GET: VendorService/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.VendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                .Include(v => v.InvestigationServiceType)
                .Include(v => v.LineOfBusiness)
                .Include(v => v.PincodeServices)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
            if (vendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            return View(vendorInvestigationServiceType);
        }

        // POST: VendorService/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.VendorInvestigationServiceType == null)
            {
                return Problem("Entity set 'ApplicationDbContext.VendorInvestigationServiceType'  is null.");
            }
            var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
            if (vendorInvestigationServiceType != null)
            {
                _context.VendorInvestigationServiceType.Remove(vendorInvestigationServiceType);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VendorInvestigationServiceTypeExists(string id)
        {
            return (_context.VendorInvestigationServiceType?.Any(e => e.VendorInvestigationServiceTypeId == id)).GetValueOrDefault();
        }
    }
}
