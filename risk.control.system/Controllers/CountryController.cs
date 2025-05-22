﻿using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Models;
using SmartBreadcrumbs.Attributes;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class CountryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;

        public CountryController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }

        // GET: RiskCaseStatus
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Country")]
        public IActionResult Profile()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetCountries(int draw, int start, int length, string search, int? orderColumn, string orderDirection)
        {
            // Determine column to sort by
            string sortColumn = orderColumn switch
            {
                0 => "Code",   // First column (index 0)
                1 => "Name",   // Second column (index 1)
                _ => "Code"    // Default to "Code" if no column is specified
            };

            // Determine sort direction
            bool isAscending = orderDirection?.ToLower() == "asc";

            var query = _context.Country.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search) && Regex.IsMatch(search, @"^[a-zA-Z0-9\s]*$"))
            {
                search = search.Trim().Replace("%", "[%]")
                   .Replace("_", "[_]")
                   .Replace("[", "[[]");
                query = query.Where(p =>
                    EF.Functions.Like(p.Code, $"%{search}%") ||
                    EF.Functions.Like(p.Name, $"%{search}%"));
            }

            // Dynamically apply sorting using reflection
            var parameter = Expression.Parameter(typeof(Country), "p");
            var property = Expression.Property(parameter, sortColumn);  // Get the property dynamically
            var lambda = Expression.Lambda<Func<Country, object>>(Expression.Convert(property, typeof(object)), parameter);

            // Apply sorting
            query = isAscending ? query.OrderBy(lambda) : query.OrderByDescending(lambda);

            // Get total records before filtering
            var totalRecords = await query.CountAsync();

            // Apply paging
            var data = await query
                .Skip(start)
                .Take(length)
                .Select(s => new
                {
                    s.CountryId,
                    s.Name,
                    s.Code,
                    s.ISDCode,
                    IsUpdated = s.IsUpdated,
                    lastModified = s.Updated
                })
                .ToListAsync();

            // Prepare DataTables response
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data
            };

            return Json(response);
        }

        // GET: RiskCaseStatus/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id == 0 || _context.Country == null)
            {
                notifyService.Error("country not found!");
                return NotFound();
            }

            var country = await _context.Country
                .FirstOrDefaultAsync(m => m.CountryId == id);
            if (country == null)
            {
                notifyService.Error("country not found!");
                return NotFound();
            }

            return View(country);
        }

        [Breadcrumb("Add New", FromAction = "Profile")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Country country)
        {
            country.IsUpdated = true;
            country.Updated = DateTime.Now;
            country.UpdatedBy = HttpContext.User?.Identity?.Name;
            _context.Add(country);
            await _context.SaveChangesAsync();
            notifyService.Success("country added successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: RiskCaseStatus/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id == 0 || _context.Country == null)
            {
                notifyService.Error("country not found!");
                return NotFound();
            }

            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == id);
            if (country == null)
            {
                notifyService.Error("country not found!");
                return NotFound();
            }
            return View(country);
        }

        // POST: RiskCaseStatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Country country)
        {
            if (id != country.CountryId)
            {
                notifyService.Error("country not found!");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    country.Updated = DateTime.Now;
                    country.IsUpdated = true;
                    country.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Update(country);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CountryExists(country.CountryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                notifyService.Success("country edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            notifyService.Error("Error to edit country!");
            return View(country);
        }

        // GET: RiskCaseStatus/Delete/5
        [Breadcrumb("Delete ", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id == 0 || _context.Country == null)
            {
                notifyService.Error("country not found!");
                return NotFound();
            }

            var country = await _context.Country
                .FirstOrDefaultAsync(m => m.CountryId == id);
            if (country == null)
            {
                notifyService.Error("country not found!");
                return NotFound();
            }

            return View(country);
        }

        // POST: RiskCaseStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.Country == null)
            {
                notifyService.Error("country not found!");
                return Problem("Entity set 'ApplicationDbContext.Country'  is null.");
            }
            var country = await _context.Country.FindAsync(id);
            if (country != null)
            {
                country.Updated = DateTime.Now;
                country.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Country.Remove(country);
            }

            await _context.SaveChangesAsync();
            notifyService.Success("country deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool CountryExists(long id)
        {
            return (_context.Country?.Any(e => e.CountryId == id)).GetValueOrDefault();
        }
    }
}