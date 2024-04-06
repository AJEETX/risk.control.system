using AspNetCoreHero.ToastNotification.Abstractions;

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
    public class CaseLocationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;
        private HttpClient client = new HttpClient();

        public CaseLocationsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            IToastNotification toastNotification)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.notifyService = notifyService;
            this.toastNotification = toastNotification;
        }

        // GET: CaseLocations
        [Breadcrumb("Location", FromController = typeof(ClaimsInvestigationController), FromAction = "Draft")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.CaseLocation.Include(c => c.District).Include(c => c.State);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CaseLocations/Details/5

        public async Task<IActionResult> AssignerDetails(long? id)
        {
            if (id == null || _context.CaseLocation == null)
            {
                return NotFound();
            }

            var caseLocation = await _context.CaseLocation
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(m => m.CaseLocationId == id);
            if (caseLocation == null)
            {
                return NotFound();
            }

            return View(caseLocation);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null || _context.CaseLocation == null)
            {
                return NotFound();
            }

            var caseLocation = await _context.CaseLocation
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(m => m.CaseLocationId == id);
            if (caseLocation == null)
            {
                return NotFound();
            }

            return View(caseLocation);
        }

        // GET: CaseLocations/Create
        //[Breadcrumb("Create", FromController = typeof(ClaimsInvestigationController), FromAction = "Details")]
        [Breadcrumb("Add Beneficiary", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public IActionResult Create(string id)
        {
            try
            {
                var claim = _context.ClaimsInvestigation
                                .Include(i => i.PolicyDetail)
                                .Include(i => i.CaseLocations)
                                .ThenInclude(c => c.District)
                                                .Include(i => i.CaseLocations)
                                .ThenInclude(c => c.State)
                                .Include(i => i.CaseLocations)
                                .ThenInclude(c => c.Country)
                                                .Include(i => i.CaseLocations)
                                .FirstOrDefault(v => v.ClaimsInvestigationId == id);

                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");

                var beneRelationId = _context.BeneficiaryRelation.FirstOrDefault().BeneficiaryRelationId;
                var pinCode = _context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
                var district = _context.District.Include(d => d.State).FirstOrDefault(d => d.DistrictId == pinCode.District.DistrictId);
                var state = _context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == district.State.StateId);
                var country = _context.Country.FirstOrDefault(c => c.CountryId == state.Country.CountryId);
                var random = new Random();

                var model = new CaseLocation
                {
                    ClaimsInvestigationId = id,
                    ClaimsInvestigation = claim,
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

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string claimId, CaseLocation caseLocation)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(claimId) || caseLocation is null)
                {
                    notifyService.Error("OOPS !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                   i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);

                caseLocation.Updated = DateTime.UtcNow;
                caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;
                caseLocation.InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId;

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
                notifyService.Error("OOPS !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // GET: CaseLocations/Edit/5
        [Breadcrumb("Edit Beneficiary", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> Edit(long? id)
        {
            try
            {

                if (id == null || _context.CaseLocation == null)
                {
                    notifyService.Error("OOPS !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var caseLocation = await _context.CaseLocation.FindAsync(id);
                if (caseLocation == null)
                {
                    notifyService.Error("OOPS !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var services = _context.CaseLocation
                    .Include(v => v.ClaimsInvestigation)
                    .ThenInclude(c => c.PolicyDetail)
                    .Include(v => v.District)
                    .First(v => v.CaseLocationId == id);

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == caseLocation.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == caseLocation.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == caseLocation.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", caseLocation.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", caseLocation.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", caseLocation.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", caseLocation.PinCodeId);

                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);

                return View(services);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: CaseLocations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        public async Task<IActionResult> Edit(long id, CaseLocation ecaseLocation)
        {
            try
            {
                if (id != ecaseLocation.CaseLocationId)
                {
                    notifyService.Error("OOPS !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                   i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
                {
                    var caseLocation = _context.CaseLocation.FirstOrDefault(c => c.CaseLocationId == ecaseLocation.CaseLocationId);
                    caseLocation.Updated = DateTime.UtcNow;
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
                        var existingLocation = _context.CaseLocation.AsNoTracking().Where(c =>
                            c.CaseLocationId == caseLocation.CaseLocationId && c.CaseLocationId == id).FirstOrDefault();
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
                    return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
                }
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", ecaseLocation.DistrictId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", ecaseLocation.BeneficiaryRelationId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", ecaseLocation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", ecaseLocation.StateId);
            return View(ecaseLocation);
        }

        // GET: CaseLocations/Delete/5
        [Breadcrumb("Delete ")]
        public async Task<IActionResult> Delete(long? id)
        {
            try
            {

                if (id == null || _context.CaseLocation == null)
                {
                    notifyService.Error("OOPS !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var caseLocation = await _context.CaseLocation
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .FirstOrDefaultAsync(m => m.CaseLocationId == id);
                if (caseLocation == null)
                {
                    notifyService.Error("OOPS !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(caseLocation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: CaseLocations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                if(1> id)
                {
                    notifyService.Error("OOPS !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var caseLocation = await _context.CaseLocation.FindAsync(id);
                if(caseLocation == null)
                {
                    notifyService.Error("OOPS !!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                caseLocation.Updated = DateTime.UtcNow;
                caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CaseLocation.Remove(caseLocation);

                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage(string.Format("<i class='fas fa-user-tie'></i> Beneficiary {0} deleted successfully !", caseLocation.BeneficiaryName));
                return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = caseLocation.ClaimsInvestigationId });
            }
            catch (Exception)
            {
                notifyService.Error("OOPS !!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}