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
    public class VendorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VendorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Vendor
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Vendor.Include(v => v.Country).Include(v => v.District).Include(v => v.PinCode).Include(v => v.State);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Vendor/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.Vendor == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // GET: Vendor/Create
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "CountryId");
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "DistrictId");
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "PinCodeId");
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId");
            return View();
        }

        // POST: Vendor/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VendorId,Name,Code,Description,PhoneNumber,Email,Branch,Addressline,City,StateId,CountryId,PinCodeId,DistrictId,BankName,BankAccountNumber,IFSCCode,AgreementDate,ActivatedDate,DeListedDate,Status,DelistReason,Document,Created,Updated,UpdatedBy")] Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vendor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "CountryId", vendor.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "DistrictId", vendor.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "PinCodeId", vendor.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId", vendor.StateId);
            return View(vendor);
        }

        // GET: Vendor/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.Vendor == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendor.FindAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "CountryId", vendor.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "DistrictId", vendor.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "PinCodeId", vendor.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId", vendor.StateId);
            return View(vendor);
        }

        // POST: Vendor/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("VendorId,Name,Code,Description,PhoneNumber,Email,Branch,Addressline,City,StateId,CountryId,PinCodeId,DistrictId,BankName,BankAccountNumber,IFSCCode,AgreementDate,ActivatedDate,DeListedDate,Status,DelistReason,Document,Created,Updated,UpdatedBy")] Vendor vendor)
        {
            if (id != vendor.VendorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vendor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorExists(vendor.VendorId))
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
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "CountryId", vendor.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "DistrictId", vendor.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "PinCodeId", vendor.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId", vendor.StateId);
            return View(vendor);
        }

        // GET: Vendor/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.Vendor == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // POST: Vendor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.Vendor == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Vendor'  is null.");
            }
            var vendor = await _context.Vendor.FindAsync(id);
            if (vendor != null)
            {
                _context.Vendor.Remove(vendor);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VendorExists(string id)
        {
          return (_context.Vendor?.Any(e => e.VendorId == id)).GetValueOrDefault();
        }
    }
}
