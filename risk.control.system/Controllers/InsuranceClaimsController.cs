//using Microsoft.AspNetCore.Mvc;
//using risk.control.system.Models.ViewModel;
//using risk.control.system.Models;

//using SmartBreadcrumbs.Attributes;
//using risk.control.system.Data;
//using AspNetCoreHero.ToastNotification.Notyf;
//using risk.control.system.Services;
//using AspNetCoreHero.ToastNotification.Abstractions;
//using Microsoft.EntityFrameworkCore;
//using SkiaSharp;
//using Microsoft.AspNetCore.Authorization;
//using static risk.control.system.AppConstant.Applicationsettings;

//namespace risk.control.system.Controllers
//{
//    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
//    public class InsuranceClaimsController : Controller
//    {
//        private readonly ApplicationDbContext context;
//        private readonly IClaimsVendorService vendorService;
//        private readonly INotyfService notifyService;

//        public InsuranceClaimsController(ApplicationDbContext context,
//            IClaimsVendorService vendorService,
//            INotyfService notifyService
//            )
//        {
//            this.context = context;
//            this.vendorService = vendorService;
//            this.notifyService = notifyService;
//        }

//        public async Task<IActionResult> Summary4Insurer(string id)
//        {
//            try
//            {
//                var currentUserEmail = HttpContext.User?.Identity?.Name;
//                if (string.IsNullOrWhiteSpace(currentUserEmail))
//                {
//                    notifyService.Error("OOPs !!!..Contact Admin");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                if (id == null)
//                {
//                    notifyService.Error("NOT FOUND !!!..");
//                    return RedirectToAction(nameof(Index));
//                }

//                var model = await claimPolicyService.GetClaimSummary(currentUserEmail, id);

//                return View(model);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Error("OOPs !!!..Contact Admin !!!..");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }
//        public async Task<IActionResult> Summary4Agency(string id)
//        {
//            try
//            {
//                if (id == null)
//                {
//                    notifyService.Error("NOT FOUND !!!..");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                var currentUserEmail = HttpContext.User?.Identity?.Name;

//                if (string.IsNullOrWhiteSpace(currentUserEmail))
//                {
//                    notifyService.Error("OOPs !!!..Contact Admin");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
//                if(model == null)
//                {
//                    notifyService.Error("OOPs !!!..Claim no longer exist");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                return View(model);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Error("OOPs !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }
//    }
//}