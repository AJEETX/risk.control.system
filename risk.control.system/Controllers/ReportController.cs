﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using SmartBreadcrumbs.Attributes;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using System.Text.RegularExpressions;
using risk.control.system.Helpers;
using risk.control.system.Services;
using NToastNotify;
using AspNetCoreHero.ToastNotification.Notyf;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;
using SmartBreadcrumbs.Nodes;
using risk.control.system.Controllers.Company;
using risk.control.system.Controllers.Api.Claims;

namespace risk.control.system.Controllers
{
    public class ReportController : Controller
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly INotyfService notifyService;
        private readonly IClaimsService claimsService;

        public ReportController(IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            IClaimsService claimsService)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.notifyService = notifyService;
            this.claimsService = claimsService;
        }


        [HttpGet]
        public async Task<IActionResult> PrintReport(string id)
        {
            try
            {
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var claim = claimsService.GetClaims()
                    .FirstOrDefault(c => c.ClaimsInvestigationId == id);

                var policy = claim.PolicyDetail;
                var customer = claim.CustomerDetail;
                var beneficiary = claim.BeneficiaryDetail;

                string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var filename = "report" + id + ".pdf";

                var filePath = Path.Combine(webHostEnvironment.WebRootPath, "report", filename);

                ReportRunner.Run(webHostEnvironment.WebRootPath).Build(filePath); ;
                var memory = new MemoryStream();
                using var stream = new FileStream(filePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                return File(memory, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintPdfReport(string id)
        {
            try
            {
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var claim = claimsService.GetClaims()
                    .Include(r => r.AgencyReport)
                    //.Include(r=>r.AgencyReport.DigitalIdReport)
                    //.Include(r=>r.AgencyReport.PanIdReport)
                    .FirstOrDefault(c => c.ClaimsInvestigationId == id);
                var fileName = Path.GetFileName(claim.AgencyReport.PdfReportFilePath);
                var memory = new MemoryStream();
                using var stream = new FileStream(claim.AgencyReport.PdfReportFilePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                //notifyService.Success($"Policy {claim.PolicyDetail.ContractNumber} Report download success !!!");
                return File(memory, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}