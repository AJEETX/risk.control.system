using System.IO.Compression;
using System.Text;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Seeds;
using risk.control.system.Services;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb("Claims")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class ClaimController : Controller
    {
        private readonly ILogger<ClaimController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ITimelineService _timelineService;
        private readonly IAgencyDetailService _agencyDetailService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly ICaseCreateEditService _caseCreateEditService;
        private readonly INavigationService _navigationService;
        private readonly INotyfService _notifyService;
        private readonly IEmpanelledAgencyService _empanelledAgencyService;

        public ClaimController(ILogger<ClaimController> logger,
            ApplicationDbContext context,
            IAgencyDetailService agencyDetailService,
            ITimelineService timelineService,
            IWebHostEnvironment hostingEnvironment,
            IErrorNotifyService errorNotifyService,
            ICaseCreateEditService createCreateEditService,
            INavigationService navigationService,
            IEmpanelledAgencyService empanelledAgencyService,
            INotyfService notifyService)
        {
            _agencyDetailService = agencyDetailService;
            _logger = logger;
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _errorNotifyService = errorNotifyService;
            _caseCreateEditService = createCreateEditService;
            _timelineService = timelineService;
            _navigationService = navigationService;
            _notifyService = notifyService;
            _empanelledAgencyService = empanelledAgencyService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Add));
        }

        [Breadcrumb("Add Claim")]
        [HttpGet]
        public IActionResult Add(InsuranceType type = InsuranceType.CLAIM)
        {
            var userEmail = User.Identity?.Name;

            var companyUser = _context.ApplicationUser
                .FirstOrDefault(c => c.Email == userEmail);
            var model = new FillFormViewModel
            {
                FormType = type,
                Fields = [.. _context.FormFields.Where(f => f.InsuranceType == type && f.CompanyId == companyUser!.ClientCompanyId)],
                SelectedCompanyId = companyUser!.ClientCompanyId
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Add(FillFormViewModel postModel, IFormCollection form)
        {
            InsuranceType currentFormType = postModel.FormType;

            // 1. Get the current logged-in user's Company ID securely
            var userEmail = User.Identity?.Name;
            var companyUser = await _context.ApplicationUser
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null)
            {
                return Unauthorized();
            }

            long companyId = companyUser.ClientCompanyId!.Value;

            // 2. Query fields specific to BOTH FormType and CompanyId
            var fields = await _context.FormFields
                .Where(f => f.InsuranceType == currentFormType && f.CompanyId == companyId)
                .ToListAsync();

            var submission = new SubmittedForm
            {
                CompanyId = companyId,
                SubmittedAt = DateTime.UtcNow,
                InsuranceType = currentFormType,
                CaseOwner = companyUser.Email,
            };

            foreach (var field in fields)
            {
                string valueStr = string.Empty;

                if (field.FieldType == "file")
                {
                    var uploadedFile = Request.Form.Files.FirstOrDefault(f => f.Name == $"field_{field.Id}");
                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(fileStream);
                        }
                        valueStr = "/uploads/" + uniqueFileName;
                    }
                }
                else if (field.FieldType == "date")
                {
                    string rawDate = form[$"field_{field.Id}"].ToString();
                    if (!string.IsNullOrWhiteSpace(rawDate))
                    {
                        if (DateTime.TryParseExact(rawDate, "dd-MM-yyyy",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out DateTime parsedDate))
                        {
                            valueStr = parsedDate.ToString("dd-MM-yyyy");
                        }
                        else
                        {
                            valueStr = rawDate;
                        }
                    }
                }
                else if (field.FieldType == "address")
                {
                    valueStr = form[$"field_{field.Id}"].ToString().Trim();
                }
                else
                {
                    valueStr = form[$"field_{field.Id}"].ToString();
                }

                submission.Values.Add(new SubmittedValue
                {
                    FormFieldId = field.Id,
                    Value = valueStr
                });
            }

            _context.SubmittedForms.Add(submission);
            await _context.SaveChangesAsync(null, false);
            await _timelineService.UpdateCaseStatus(submission.Id, companyUser.Email!);
            return RedirectToAction(nameof(Assign));
        }

        [HttpGet]
        public IActionResult Get()
        {
            var userEmail = User.Identity?.Name;
            var companyUser = _context.ApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            if (companyUser == null)
                return Unauthorized();

            // 1. Fetch form fields for dynamic column headers
            var formFields = _context.FormFields
                .Where(f => f.InsuranceType == InsuranceType.CLAIM && f.CompanyId == companyUser.ClientCompanyId)
                .OrderBy(f => f.Id)
                .Select(f => new
                {
                    f.Id,
                    f.Label,
                    f.FieldType,
                    f.Section
                })
                .ToList();

            // 2. Query database for raw submissions first (Translates to SQL)
            var rawSubmissions = _context.SubmittedForms
                .Include(sf => sf.Values)
                .ThenInclude(sv => sv.FormField)
                .Where(f => f.InsuranceType == InsuranceType.CLAIM && f.CompanyId == companyUser.ClientCompanyId &&
                (f.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR || f.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY || f.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY))
                .OrderByDescending(sf => sf.SubmittedAt)
                .Select(sf => new
                {
                    Id = sf.Id,
                    SubmittedAt = sf.SubmittedAt,
                    Status = sf.Status,
                    WithdrawlComments = sf.WithdrawlComments,
                    Values = sf.Values.Select(v => new
                    {
                        v.FormFieldId,
                        Label = v.FormField.Label,
                        Value = v.Value,
                        Type = v.FormField.FieldType,
                        Section = v.FormField.Section
                    }).ToList()
                })
                .ToList(); // Execution happens here (DB query finishes)

            // 3. Project to Dictionary in memory (Client-side evaluation)
            var submissions = rawSubmissions.Select(sf => new
            {
                Id = sf.Id,
                SubmittedAt = sf.SubmittedAt.ToString("o"),
                Status = sf.Status, // Include Status property
                WithDrawable = sf.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY || sf.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                WithdrawlComments = sf.WithdrawlComments,
                Values = sf.Values.ToDictionary(
                    v => v.FormFieldId.ToString(),
                    v => v
                )
            }).ToList();

            return Json(new { fields = formFields, data = submissions });
        }
        [Breadcrumb("Assign")]
        [HttpGet]
        public IActionResult Assign()
        {
            return View();
        }

        [HttpGet]
        [Breadcrumb("Edit Claim", FromAction = nameof(Assign))]
        public IActionResult Edit(int id)
        {
            var submission = _context.SubmittedForms
                .Include(sf => sf.Values)
                .FirstOrDefault(sf => sf.Id == id);

            if (submission == null) return NotFound();

            // 1. Fetch only dynamic fields belonging to this submission's form type context
            var fields = _context.FormFields
                .Where(f => f.InsuranceType == submission.InsuranceType)
                .ToList();

            // 2. Map everything to the strongly-typed view model
            var viewModel = new EditSubmissionViewModel
            {
                SubmissionId = id,
                FormType = submission.InsuranceType,
                Fields = fields.Select(f => new EditFieldViewModel
                {
                    Field = f,
                    CurrentValue = submission.Values.FirstOrDefault(v => v.FormFieldId == f.Id)?.Value!
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditSubmissionViewModel postModel, IFormCollection form)
        {
            var submission = _context.SubmittedForms
                .Include(sf => sf.Values)
                .FirstOrDefault(sf => sf.Id == id);

            if (submission == null) return NotFound();

            // Pull fields specifically bound to this layout type 
            var fields = _context.FormFields.Where(f => f.InsuranceType == submission.InsuranceType).ToList();

            foreach (var field in fields)
            {
                var existingValue = submission.Values.FirstOrDefault(v => v.FormFieldId == field.Id);
                var newValueStr = "";

                if (field.FieldType == "file")
                {
                    var uploadedFile = Request.Form.Files.FirstOrDefault(f => f.Name == $"field_{field.Id}");
                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                        Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(fileStream);
                        }
                        newValueStr = "/uploads/" + uniqueFileName;

                        if (existingValue != null && !string.IsNullOrEmpty(existingValue.Value))
                        {
                            var oldFilePath = Path.Combine(_hostingEnvironment.WebRootPath, existingValue.Value.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                    }
                    else
                    {
                        newValueStr = existingValue?.Value ?? "";
                    }
                }
                else if (field.FieldType == "date")
                {
                    string rawDate = form[$"field_{field.Id}"].ToString();
                    if (!string.IsNullOrWhiteSpace(rawDate))
                    {
                        if (DateTime.TryParseExact(rawDate, "dd-MM-yyyy",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out DateTime parsedDate))
                        {
                            newValueStr = parsedDate.ToString("yyyy-MM-dd");
                        }
                        else { newValueStr = rawDate; }
                    }
                }
                else
                {
                    newValueStr = form[$"field_{field.Id}"].ToString();
                }

                if (existingValue != null)
                {
                    existingValue.Value = newValueStr!;
                }
                else
                {
                    submission.Values.Add(new SubmittedValue { FormFieldId = field.Id, Value = newValueStr! });
                }
            }

            await _context.SaveChangesAsync(null, false);
            return RedirectToAction(nameof(Assign));
        }

        [Breadcrumb("Agencies", FromAction = nameof(Assign))]
        [HttpGet]
        public async Task<IActionResult> EmpanelledAgencies(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("No Case selected!!!. Please select Case to allocate.");
                    return RedirectToAction(nameof(Assign));
                }
                var model = await _empanelledAgencyService.GetEmpanelledAgencies(id, userEmail);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Empanelled Agencies of case {CaseId}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting Agencies. Try again.");
                return RedirectToAction(nameof(Assign));
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var submission = _context.SubmittedForms
                .Include(sf => sf.CaseTimelines)
                .Include(sf => sf.Values)
                .ThenInclude(sv => sv.FormField)
                .FirstOrDefault(sf => sf.Id == id);

            if (submission == null)
            {
                return Json(new { success = false, message = "Submission not found." });
            }

            // Delete associated physical media uploads
            foreach (var val in submission.Values)
            {
                if (val.FormField.FieldType == "file" && !string.IsNullOrEmpty(val.Value))
                {
                    var filePath = Path.Combine(_hostingEnvironment.WebRootPath, val.Value.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            foreach (var tl in submission.CaseTimelines)
            {
                _context.CaseTimeline.Remove(tl);
            }
            _context.SubmittedValues.RemoveRange(submission.Values);
            _context.SubmittedForms.Remove(submission);
            _context.SaveChanges();

            return Json(new { success = true, message = "Case deleted successfully." });
        }
        [Breadcrumb(title: "Active")]
        public async Task<IActionResult> Active()
        {
            var userEmail = HttpContext.User.Identity?.Name!;
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting active cases. {UserEmail}", userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetCaseTimelinesPartial(int claimId)
        {
            var timelines = await _context.SubmittedForms.Include(f => f.CaseTimelines)
                .Where(t => t.Id == claimId)
                .Select(f => f.CaseTimelines)
                .FirstOrDefaultAsync();
            var timeTaken = DateTime.UtcNow - timelines!.FirstOrDefault()!.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";
            return PartialView("Investigation/_CaseTimelines", timelines);
        }
        [Breadcrumb("Agency Profile", FromAction = nameof(EmpanelledAgencies))]
        [HttpGet]
        public async Task<IActionResult> AgencyProfile(long id, long selectedcase)
        {
            if (id <= 0 || selectedcase <= 0)
            {
                _notifyService.Error("Invalid request.");
                return RedirectToAction(nameof(Assign));
            }

            var userEmail = User.Identity?.Name;

            try
            {
                var vendor = await _agencyDetailService.GetVendorDetailAsync(id, selectedcase);

                if (vendor == null)
                {
                    _notifyService.Error("Agency not found.");
                    return RedirectToAction(nameof(EmpanelledAgencies));
                }

                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agency details. VendorId: {VendorId}, User: {UserEmail}", id, userEmail ?? "Anonymous");
                _notifyService.Error("Error getting agency details. Try again.");
                return RedirectToAction(nameof(Assign));
            }
        }

        [HttpGet]
        public IActionResult DownloadSamplePackage(long companyId, InsuranceType type)
        {
            var fields = _context.FormFields
                .Where(f => f.CompanyId == companyId && f.InsuranceType == type)
                .OrderBy(f => f.Id)
                .ToList();

            if (!fields.Any())
            {
                _notifyService.Warning("No form fields found for the selected company and form type.");
                return RedirectToAction(nameof(Add), new { type });
            }

            var samplePolicyNumber = $"{type}-1001";
            byte[] zipBytes;

            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var csvHeader = string.Join(",", fields.Select(f => EscapeCsvField(f.Label)));
                    var csvRow = string.Join(",", fields.Select(f => GetSampleFieldValue(f, samplePolicyNumber)));
                    var csvContent = $"{csvHeader}\n{csvRow}";
                    var csvData = Encoding.UTF8.GetBytes(csvContent);

                    var csvEntry = archive.CreateEntry($"Sample_{type}_Data.csv", CompressionLevel.Optimal);
                    using (var entryStream = csvEntry.Open())
                    {
                        entryStream.Write(csvData, 0, csvData.Length);
                    }

                    var pdfEntry = archive.CreateEntry($"{samplePolicyNumber}/PolicyDocument.pdf", CompressionLevel.Optimal);
                    using (var entryStream = pdfEntry.Open())
                    {
                        byte[] policyPdfData = SampleFiles.GetValidPolicyPdfBytes(samplePolicyNumber);
                        entryStream.Write(policyPdfData, 0, policyPdfData.Length);
                    }

                    var imgEntry = archive.CreateEntry($"{samplePolicyNumber}/NomineePhoto.jpg", CompressionLevel.Optimal);
                    using (var entryStream = imgEntry.Open())
                    {
                        byte[] jpgData = SampleFiles.GetValidJpgBytes();
                        entryStream.Write(jpgData, 0, jpgData.Length);
                    }

                    var claimPdfEntry = archive.CreateEntry($"{samplePolicyNumber}/ClaimDocument.pdf", CompressionLevel.Optimal);
                    using (var entryStream = claimPdfEntry.Open())
                    {
                        byte[] claimPdfData = SampleFiles.GetValidClaimPdfBytes(samplePolicyNumber);
                        entryStream.Write(claimPdfData, 0, claimPdfData.Length);
                    }

                }

                zipBytes = zipStream.ToArray();
            }

            var zipFileName = $"{type}_Sample.zip";
            return File(zipBytes, "application/zip", zipFileName);
        }
        private static string GetSampleFieldValue(FormField field, string policyNumber)
        {
            var type = field.FieldType?.ToLower() ?? "";
            var label = field.Label?.ToLower() ?? "";

            if (type.Contains("policy #"))
                return EscapeCsvField(policyNumber);

            if (type.Contains("file") || label.Contains("photo") || label.Contains("document"))
            {
                if (label.Contains("photo") || label.Contains("image"))
                    return EscapeCsvField($"{policyNumber}/Nominee Photo.jpg");
                return EscapeCsvField($"{policyNumber}/Policy Document.pdf");
            }

            if (type.Contains("date"))
                return EscapeCsvField(DateTime.Now.ToString("yyyy-MM-dd"));

            if (type.Contains("number") || type.Contains("amount"))
                return EscapeCsvField("50000");

            if (label.Contains("email"))
                return EscapeCsvField("john.doe@example.com");

            if (label.Contains("phone") || label.Contains("mobile"))
                return EscapeCsvField("9876543210");

            return EscapeCsvField($"Sample {field.Label}");
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}