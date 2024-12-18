﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class PinCodesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public PinCodesController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: PinCodes
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Pincode")]
        public async Task<IActionResult> Profile()
        {
            var applicationDbContext = _context.PinCode.
                Include(p => p.Country)
                .Include(p => p.District)
                .Include(p => p.State).AsQueryable();

            var applicationDbContextResult = await applicationDbContext.ToListAsync();
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            return View(applicationDbContextResult);
        }

        // GET: PinCodes/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1 || _context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            var pinCode = await _context.PinCode
                .Include(p => p.Country)
                .Include(p => p.State)
                .Include(p => p.District)
                .FirstOrDefaultAsync(m => m.PinCodeId == id);
            if (pinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            return View(pinCode);
        }

        // GET: PinCodes/Create
        [Breadcrumb("Add New", FromAction = "Profile")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        // POST: PinCodes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PinCode pinCode)
        {
            pinCode.Updated = DateTime.Now;
            pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;

            _context.Add(pinCode);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("pincode created successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: PinCodes/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id == null || _context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            var pinCode = await _context.PinCode.FindAsync(id);
            if (pinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", pinCode.CountryId);
            ViewData["StateId"] = new SelectList(_context.State.Where(s => s.CountryId == pinCode.CountryId), "StateId", "Name", pinCode?.StateId);
            ViewData["DistrictId"] = new SelectList(_context.District.Where(s => s.StateId == pinCode.StateId), "DistrictId", "Name", pinCode?.DistrictId);
            return View(pinCode);
        }

        // POST: PinCodes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, PinCode pinCode)
        {
            if (id != pinCode.PinCodeId)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }
            try
            {
                pinCode.Updated = DateTime.Now;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(pinCode);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PinCodeExists(pinCode.PinCodeId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            toastNotification.AddSuccessToastMessage("pincode edited successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: PinCodes/Delete/5
        [Breadcrumb("Delete ", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            var pinCode = await _context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District)
                .FirstOrDefaultAsync(m => m.PinCodeId == id);
            if (pinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            return View(pinCode);
        }

        // POST: PinCodes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return Problem("Entity set 'ApplicationDbContext.PinCode'  is null.");
            }
            var pinCode = await _context.PinCode.FindAsync(id);
            if (pinCode != null)
            {
                pinCode.Updated = DateTime.Now;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.PinCode.Remove(pinCode);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("pincode deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool PinCodeExists(long id)
        {
            return (_context.PinCode?.Any(e => e.PinCodeId == id)).GetValueOrDefault();
        }
    }
}