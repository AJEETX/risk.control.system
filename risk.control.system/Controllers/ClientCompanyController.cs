using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
        [Breadcrumb("Company")]
    public class ClientCompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public ClientCompanyController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: ClientCompanies
        [Breadcrumb("Home")]
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? currentPage, int pageSize = 10)
        {
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.CodeSortParm = string.IsNullOrEmpty(sortOrder) ? "code_desc" : "";
            if (searchString != null)
            {
                currentPage = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var applicationDbContext = _context.ClientCompany.Include(c => c.Country).Include(c => c.PinCode).Include(c => c.State).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                applicationDbContext = applicationDbContext.Where(a =>
                a.Name.ToLower().Contains(searchString.Trim().ToLower()) ||
                a.Code.ToLower().Contains(searchString.Trim().ToLower()));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.Name);
                    break;
                case "code_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.Code);
                    break;
                default:
                    applicationDbContext.OrderByDescending(s => s.Name);
                    break;
            }
            int pageNumber = (currentPage ?? 1);
            ViewBag.TotalPages = (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.ShowPrevious = pageNumber > 1;
            ViewBag.ShowNext = pageNumber < (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));
            ViewBag.ShowFirst = pageNumber != 1;
            ViewBag.ShowLast = pageNumber != (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));

            var applicationDbContextResult = await applicationDbContext.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return View(applicationDbContextResult);
        }

        // GET: ClientCompanies/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ClientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }

            var clientCompany = await _context.ClientCompany
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClientCompanyId == id);
            if (clientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }

            return View(clientCompany);
        }

        // GET: ClientCompanies/Create
        [Breadcrumb("Create")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        // POST: ClientCompanies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCompany clientCompany)
        {
            if (clientCompany is not null)
            {
                _context.Add(clientCompany);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("client company created successfully!");
                return RedirectToAction(nameof(Index));
            }
                toastNotification.AddErrorToastMessage("client company not found!");
            return Problem();
        }

        // GET: ClientCompanies/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ClientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }

            var clientCompany = await _context.ClientCompany.FindAsync(id);
            if (clientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", clientCompany.CountryId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", clientCompany.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", clientCompany.StateId);
            return View(clientCompany);
        }

        // POST: ClientCompanies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ClientCompany clientCompany)
        {
            if (id != clientCompany.ClientCompanyId)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }

            if (clientCompany is not null)
            {
                try
                {
                    _context.Update(clientCompany);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientCompanyExists(clientCompany.ClientCompanyId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("client company edited successfully!");
                return RedirectToAction(nameof(Index));
            }
                toastNotification.AddErrorToastMessage("Error to edit client company!");
            return Problem();
        }

        // GET: ClientCompanies/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ClientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }

            var clientCompany = await _context.ClientCompany
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClientCompanyId == id);
            if (clientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }

            return View(clientCompany);
        }

        // POST: ClientCompanies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.ClientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return Problem("Entity set 'ApplicationDbContext.ClientCompany'  is null.");
            }
            var clientCompany = await _context.ClientCompany.FindAsync(id);
            if (clientCompany != null)
            {
                _context.ClientCompany.Remove(clientCompany);
            }

            await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("client company deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool ClientCompanyExists(string id)
        {
            return (_context.ClientCompany?.Any(e => e.ClientCompanyId == id)).GetValueOrDefault();
        }
    }
}
