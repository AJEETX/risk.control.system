﻿using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers
{
    public class BeneficiaryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;
        private HttpClient client = new HttpClient();

        public BeneficiaryController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            IToastNotification toastNotification)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.notifyService = notifyService;
            this.toastNotification = toastNotification;
        }

        [Breadcrumb("Add Beneficiary", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public IActionResult Create(string id)
        {
            try
            {
                var claim = _context.ClaimsInvestigation
                                .Include(i => i.PolicyDetail)
                                .FirstOrDefault(v => v.ClaimsInvestigationId == id);

                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

                var beneRelationId = _context.BeneficiaryRelation.FirstOrDefault().BeneficiaryRelationId;
                var pinCode = _context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
                var district = _context.District.Include(d => d.State).FirstOrDefault(d => d.DistrictId == pinCode.District.DistrictId);
                var state = _context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == district.State.StateId);
                var country = _context.Country.FirstOrDefault(c => c.CountryId == state.Country.CountryId);
                var random = new Random();

                var model = new BeneficiaryDetail
                {
                    ClaimsInvestigation = claim,
                    ClaimsInvestigationId = id,
                    Addressline = random.Next(100, 999) + " GREAT ROAD",
                    BeneficiaryDateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddMonths(3),
                    BeneficiaryIncome = Income.MEDIUUM_INCOME,
                    BeneficiaryName = NameGenerator.GenerateName(),
                    BeneficiaryRelationId = beneRelationId,
                    CountryId = country.CountryId,
                    StateId = state.StateId,
                    DistrictId = district.DistrictId,
                    PinCodeId = pinCode.PinCodeId,
                    BeneficiaryContactNumber = random.NextInt64(5555555555, 9999999999),
                };

                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == country.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == state.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == district.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", model.CountryId);
                ViewData["DistrictId"] = new SelectList(districts.OrderBy(s => s.Code), "DistrictId", "Name", model.DistrictId);
                ViewData["StateId"] = new SelectList(relatedStates.OrderBy(s => s.Code), "StateId", "Name", model.StateId);
                ViewData["PinCodeId"] = new SelectList(pincodes.OrderBy(s => s.Code), "PinCodeId", "Code", model.PinCodeId);
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

                var claimsPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "New & Draft") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Create", "Beneficiary", $"Add beneficiary") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string claimId, BeneficiaryDetail caseLocation)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(claimId) || caseLocation is null)
                {
                    notifyService.Error("NOT FOUND  !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                caseLocation.Updated = DateTime.Now;
                caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;

                IFormFile? customerDocument = Request.Form?.Files?.FirstOrDefault();
                if (customerDocument != null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    caseLocation.ProfilePicture = dataStream.ToArray();
                }

                caseLocation.ClaimsInvestigationId = claimId;
                var pincode = _context.PinCode.FirstOrDefault(p => p.PinCodeId == caseLocation.PinCodeId);

                caseLocation.PinCode = pincode;

                var customerLatLong = caseLocation.PinCode.Latitude + "," + caseLocation.PinCode.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
                caseLocation.BeneficiaryLocationMap = url;
                caseLocation.ClaimReport.ClaimsInvestigationId = claimId;
                _context.Add(caseLocation);
                await _context.SaveChangesAsync();

                var claimsInvestigation = await _context.ClaimsInvestigation
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == claimId);
                claimsInvestigation.IsReady2Assign = true;

                _context.ClaimsInvestigation.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Beneficiary {caseLocation.BeneficiaryName} added successfully", 3, "green", "fas fa-user-tie");

                return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });

                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", caseLocation.CountryId);
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);
                ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", caseLocation.DistrictId);
                ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId", caseLocation.StateId);
                ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Code", caseLocation.PinCodeId);
                return View(caseLocation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Beneficiary", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> Edit(long? id)
        {
            try
            {
                if (id == null || id < 1)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var caseLocation = await _context.BeneficiaryDetail.FindAsync(id);
                if (caseLocation == null)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var services = _context.BeneficiaryDetail
                    .Include(v => v.ClaimsInvestigation)
                    .ThenInclude(c => c.PolicyDetail)
                    .Include(v => v.District)
                    .First(v => v.BeneficiaryDetailId == id);

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == caseLocation.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == caseLocation.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == caseLocation.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", caseLocation.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", caseLocation.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", caseLocation.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", caseLocation.PinCodeId);

                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);


                var claimsPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "New & Draft") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", $"Details") { Parent = agencyPage, RouteValues = new { id = caseLocation.ClaimsInvestigationId } };
                var editPage = new MvcBreadcrumbNode("Edit", "Beneficiary", $"Edit Beneficiary") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(services);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb("Edit Beneficiary", FromAction = "DetailsAuto", FromController = typeof(ClaimsInvestigationController))]
        public async Task<IActionResult> EditAuto(long? id)
        {
            try
            {
                if (id == null)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var caseLocation = await _context.BeneficiaryDetail.FindAsync(id);
                if (caseLocation == null)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var services = _context.BeneficiaryDetail
                    .Include(v => v.ClaimsInvestigation)
                    .ThenInclude(c => c.PolicyDetail)
                    .Include(v => v.District)
                    .First(v => v.BeneficiaryDetailId == id);

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == caseLocation.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == caseLocation.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == caseLocation.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", caseLocation.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", caseLocation.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", caseLocation.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", caseLocation.PinCodeId);

                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);


                var claimsPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Assign(auto)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("DetailsAuto", "ClaimsInvestigation", $"Details") { Parent = agencyPage, RouteValues = new { id = caseLocation.ClaimsInvestigationId } };
                var editPage = new MvcBreadcrumbNode("EditAuto", "ClaimsInvestigation", $"Edit beneficiary") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(services);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Beneficiary", FromAction = "DetailsManual", FromController = typeof(ClaimsInvestigationController))]
        public async Task<IActionResult> EditManual(long? id)
        {
            try
            {
                if (id == null)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var caseLocation = await _context.BeneficiaryDetail.FindAsync(id);
                if (caseLocation == null)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var services = _context.BeneficiaryDetail
                    .Include(v => v.ClaimsInvestigation)
                    .ThenInclude(c => c.PolicyDetail)
                    .Include(v => v.District)
                    .First(v => v.BeneficiaryDetailId == id);

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == caseLocation.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == caseLocation.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == caseLocation.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", caseLocation.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", caseLocation.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", caseLocation.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", caseLocation.PinCodeId);

                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);


                var claimsPage = new MvcBreadcrumbNode("Incomplete", "ClaimsInvestigation", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Assigner", "ClaimsInvestigation", "Assign & Re") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("DetailsManual", "ClaimsInvestigation", $"Details") { Parent = agencyPage, RouteValues = new { id = caseLocation.ClaimsInvestigationId } };
                var editPage = new MvcBreadcrumbNode("EditManual", "ClaimsInvestigation", $"Edit Beneficairy") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(services);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        public async Task<IActionResult> Edit(long id, BeneficiaryDetail ecaseLocation, string claimtype, long beneficiaryDetailId)
        {
            try
            {
                if (id != ecaseLocation.BeneficiaryDetailId && beneficiaryDetailId != ecaseLocation.BeneficiaryDetailId)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if(id == 0)
                {
                    id = beneficiaryDetailId;
                }
                var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                   i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
                var caseLocation = _context.BeneficiaryDetail.FirstOrDefault(c => c.BeneficiaryDetailId == ecaseLocation.BeneficiaryDetailId);
                caseLocation.Updated = DateTime.Now;
                caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;
                caseLocation.Addressline = ecaseLocation.Addressline;
                caseLocation.BeneficiaryContactNumber = ecaseLocation.BeneficiaryContactNumber;
                caseLocation.BeneficiaryDateOfBirth = ecaseLocation.BeneficiaryDateOfBirth;
                caseLocation.BeneficiaryIncome = ecaseLocation.BeneficiaryIncome;
                caseLocation.BeneficiaryName = ecaseLocation.BeneficiaryName;
                caseLocation.BeneficiaryRelation = ecaseLocation.BeneficiaryRelation;
                caseLocation.BeneficiaryRelationId = ecaseLocation.BeneficiaryRelationId;
                caseLocation.ClaimsInvestigationId = ecaseLocation.ClaimsInvestigationId;
                caseLocation.CountryId = ecaseLocation.CountryId;
                caseLocation.DistrictId = ecaseLocation.DistrictId;
                caseLocation.PinCodeId = ecaseLocation.PinCodeId;
                caseLocation.StateId = ecaseLocation.StateId;
                var pincode = _context.PinCode.FirstOrDefault(p => p.PinCodeId == caseLocation.PinCodeId);
                caseLocation.PinCode = pincode;
                var customerLatLong = pincode.Latitude + "," + pincode.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
                caseLocation.BeneficiaryLocationMap = url;

                IFormFile? customerDocument = Request.Form?.Files?.FirstOrDefault();
                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    caseLocation.ProfilePicture = dataStream.ToArray();
                }
                else
                {
                    var existingLocation = _context.BeneficiaryDetail.AsNoTracking().Where(c =>
                        c.BeneficiaryDetailId == caseLocation.BeneficiaryDetailId && c.BeneficiaryDetailId == id).FirstOrDefault();
                    if (existingLocation.ProfilePicture != null || !string.IsNullOrWhiteSpace(existingLocation.ProfilePictureUrl))
                    {
                        caseLocation.ProfilePicture = existingLocation.ProfilePicture;
                        caseLocation.ProfilePictureUrl = existingLocation.ProfilePictureUrl;
                    }
                }

                var pinCode = _context.PinCode.FirstOrDefault(p => p.PinCodeId == caseLocation.PinCodeId);
                caseLocation.PinCode.Latitude = pinCode.Latitude;
                caseLocation.PinCode.Longitude = pinCode.Longitude;

                _context.Update(caseLocation);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Beneficiary {caseLocation.BeneficiaryName} edited successfully", 3, "orange", "fas fa-user-tie");
                if (string.IsNullOrWhiteSpace(claimtype) || claimtype.Equals("draft", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
                }
                else if (claimtype.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(ClaimsInvestigationController.DetailsAuto), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });

                }
                else if (claimtype.Equals("manual", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(ClaimsInvestigationController.DetailsManual), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });

                }
                return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });


            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", ecaseLocation.DistrictId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", ecaseLocation.BeneficiaryRelationId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", ecaseLocation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", ecaseLocation.StateId);
            return View(ecaseLocation);
        }
    }
}