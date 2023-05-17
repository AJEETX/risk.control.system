using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class VendorApplicationUsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public VendorApplicationUsersController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: VendorApplicationUsers
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.District).Include(v => v.PinCode).Include(v => v.State).Include(v => v.Vendor);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: VendorApplicationUsers/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null || _context.VendorApplicationUser == null)
            {
                return NotFound();
            }

            var vendorApplicationUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vendorApplicationUser == null)
            {
                return NotFound();
            }

            return View(vendorApplicationUser);
        }

        // GET: VendorApplicationUsers/Create
        public IActionResult Create(string id)
        {
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == id);
            var model = new VendorApplicationUser { Vendor = vendor };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View(model);
        }

        // POST: VendorApplicationUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorApplicationUser vendorApplicationUser)
        {

            if (vendorApplicationUser is not null)
            {
                vendorApplicationUser.Mailbox.Name = vendorApplicationUser.Email;
                IFormFile? vendorUserProfile = Request.Form?.Files?.FirstOrDefault();
                if (vendorUserProfile is not null)
                {
                    vendorApplicationUser.ProfileImage = vendorUserProfile;
                    using var dataStream = new MemoryStream();
                    await vendorApplicationUser.ProfileImage.CopyToAsync(dataStream);
                    vendorApplicationUser.ProfilePicture = dataStream.ToArray();
                }

                _context.Add(vendorApplicationUser);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("vendor user created successfully!");
                return RedirectToAction(nameof(VendorUserController.Index), "VendorUser", new { id = vendorApplicationUser.VendorId });
            }
            toastNotification.AddErrorToastMessage("Error to create vendor user!");
            return Problem();
        }

        // GET: VendorApplicationUsers/Edit/5
        public async Task<IActionResult> Edit(long? userId)
        {
            if (userId == null || _context.VendorApplicationUser == null)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return NotFound();
            }

            var vendorApplicationUser = await _context.VendorApplicationUser.FindAsync(userId);
            if (vendorApplicationUser == null)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return NotFound();
            }
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorApplicationUser.VendorId);

            if (vendor == null)
            {
                toastNotification.AddErrorToastMessage("vendor not found");
                return NotFound();
            }
            vendorApplicationUser.Vendor = vendor;
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", vendor.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name");
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", vendor.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendor.StateId);
            return View(vendorApplicationUser);
        }

        // POST: VendorApplicationUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, VendorApplicationUser vendorApplicationUser)
        {
            if (id != vendorApplicationUser.Id)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return NotFound();
            }

            if (vendorApplicationUser is not null)
            {
                try
                {
                    vendorApplicationUser.Mailbox.Name = vendorApplicationUser.Email;
                    IFormFile? vendorUserProfile = Request.Form?.Files?.FirstOrDefault();
                    if (vendorUserProfile is not null)
                    {
                        vendorApplicationUser.ProfileImage = vendorUserProfile;
                        using var dataStream = new MemoryStream();
                        await vendorApplicationUser.ProfileImage.CopyToAsync(dataStream);
                        vendorApplicationUser.ProfilePicture = dataStream.ToArray();
                    }

                    _context.Update(vendorApplicationUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorApplicationUserExists(vendorApplicationUser.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("vendor user edited successfully!");
                return RedirectToAction(nameof(VendorUserController.Index), "VendorUser", new { id = vendorApplicationUser.VendorId });
            }

            toastNotification.AddErrorToastMessage("Error to create vendor user!");
            return RedirectToAction(nameof(VendorUserController.Index), "VendorUser", new { id = vendorApplicationUser.VendorId });
        }

        // GET: VendorApplicationUsers/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null || _context.VendorApplicationUser == null)
            {
                return NotFound();
            }

            var vendorApplicationUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vendorApplicationUser == null)
            {
                return NotFound();
            }

            return View(vendorApplicationUser);
        }

        // POST: VendorApplicationUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.VendorApplicationUser == null)
            {
                return Problem("Entity set 'ApplicationDbContext.VendorApplicationUser'  is null.");
            }
            var vendorApplicationUser = await _context.VendorApplicationUser.FindAsync(id);
            if (vendorApplicationUser != null)
            {
                _context.VendorApplicationUser.Remove(vendorApplicationUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VendorApplicationUserExists(long id)
        {
            return (_context.VendorApplicationUser?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
