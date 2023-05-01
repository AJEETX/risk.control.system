using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class RolesController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        public RolesController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();

            return View(roles);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationRole role)
        {
            if (role != null)
            {
                await _roleManager.CreateAsync(role);
            }
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(string Id)
        {
            ApplicationRole role = null;
            if (!string.IsNullOrEmpty(Id))
            {
                role = await _roleManager.FindByIdAsync(Id);
            }


            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationRole role)
        {
            if (role is not null)
            {
                var existingRole = await _roleManager.FindByIdAsync(role.Id.ToString());
                existingRole.Name = role.Name;
                await _roleManager.UpdateAsync(existingRole);
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        public async Task<IActionResult> Delete(string Id)
        {
            var role = await _roleManager.FindByIdAsync(Id);

            if (role == null)
            {
                return NotFound();
            }

            return View(role);

        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string Id)
        {
            var role = await _roleManager.FindByIdAsync(Id);
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
