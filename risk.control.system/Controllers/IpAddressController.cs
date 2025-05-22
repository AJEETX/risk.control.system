using Microsoft.AspNetCore.Mvc;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Admin Settings ")]
    public class IpAddressController : Controller
    {
        // GET: IpAddressController
        public ActionResult Index()
        {
            return RedirectToAction("Active");
        }
        [Breadcrumb("Ip Tracking")]
        public ActionResult Active()
        {
            return View();
        }

        // GET: IpAddressController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: IpAddressController/Create
        public ActionResult Create()
        {
            return View();
        }

        // GET: IpAddressController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: IpAddressController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: IpAddressController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: IpAddressController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
