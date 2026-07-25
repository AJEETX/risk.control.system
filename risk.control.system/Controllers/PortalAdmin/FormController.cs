using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME}")]
    [Breadcrumb("Company Settings ")]
    public class FormController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public FormController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }
        public IActionResult Index()
        {
            return View();
        }
        [Breadcrumb("Create Form")]
        [HttpGet]
        public IActionResult CreateClaimForm(long? companyId, FormType type = FormType.Claim)
        {
            var model = new DynamicFormDesignerViewModel
            {
                SelectedCompanyId = companyId,
                TargetFormType = type,
                Companies = _context.ClientCompany
                    .Where(c => !c.Deleted && c.Status == CompanyStatus.ACTIVE)
                    .Select(c => new CompanySelectItem
                    {
                        Id = c.ClientCompanyId,
                        Name = c.Name
                    })
                    .ToList()
            };

            // Filter fields by FormType (and CompanyId if provided)
            var query = _context.FormFields.Where(f => f.FormType == type);

            if (companyId.HasValue)
            {
                query = query.Where(f => f.CompanyId == companyId.Value);
            }

            model.Fields = query.ToList();

            return View(model);
        }

        [HttpPost]
        public IActionResult CreateClaimForm(long? selectedCompanyId, FormType targetFormType, List<FormField> fields)
        {
            // Filter out rows without labels
            fields = fields?.Where(f => !string.IsNullOrWhiteSpace(f.Label)).ToList() ?? new List<FormField>();

            // 1. Fetch and remove only existing layout records matching BOTH CompanyId and FormType
            var oldFields = _context.FormFields
                .Where(f => f.FormType == targetFormType && f.CompanyId == selectedCompanyId)
                .ToList();

            if (oldFields.Any())
            {
                _context.FormFields.RemoveRange(oldFields);
            }

            // 2. Bind CompanyId and FormType to each incoming field
            if (fields.Any())
            {
                foreach (var field in fields)
                {
                    field.CompanyId = selectedCompanyId ?? 0;
                    field.FormType = targetFormType;
                }

                _context.FormFields.AddRange(fields);
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"{targetFormType} Form structure saved successfully!";

            // 3. Redirect preserving both filter parameters
            return RedirectToAction(nameof(CreateClaimForm), new { companyId = selectedCompanyId, type = targetFormType });
        }
        [Breadcrumb("Submit Claim")]
        [HttpGet]
        public IActionResult FillClaimForm(FormType type = FormType.Claim)
        {
            var userEmail = User.Identity?.Name;

            var userCompany = _context.ApplicationUser
                .FirstOrDefault(c => c.Email == userEmail);
            var model = new FillFormViewModel
            {
                FormType = type,
                Fields = _context.FormFields.Where(f => f.FormType == type && f.CompanyId == userCompany!.ClientCompanyId).ToList()
            };

            return View(model);
        }

        [Breadcrumb("Submit Underwriting")]
        [HttpGet]
        public IActionResult FillUnderwritingForm(FormType type = FormType.Underwriting)
        {
            var userEmail = User.Identity?.Name;

            var userCompany = _context.ApplicationUser
                .FirstOrDefault(c => c.Email == userEmail);

            var model = new FillFormViewModel
            {
                FormType = type,
                Fields = _context.FormFields.Where(f => f.FormType == type && f.CompanyId == userCompany!.ClientCompanyId).ToList()
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitForm(FillFormViewModel postModel, IFormCollection form)
        {
            FormType currentFormType = postModel.FormType;

            // 1. Get the current logged-in user's Company ID securely
            var userEmail = User.Identity?.Name;
            var userCompany = await _context.ApplicationUser
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (userCompany == null)
            {
                return Unauthorized();
            }

            long companyId = userCompany.ClientCompanyId!.Value;

            // 2. Query fields specific to BOTH FormType and CompanyId
            var fields = await _context.FormFields
                .Where(f => f.FormType == currentFormType && f.CompanyId == companyId)
                .ToListAsync();

            var submission = new SubmittedForm
            {
                CompanyId = companyId,
                SubmittedAt = DateTime.UtcNow,
                FormType = currentFormType
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
                            valueStr = parsedDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            valueStr = rawDate;
                        }
                    }
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
            await _context.SaveChangesAsync();
            if (currentFormType == FormType.Claim)
            {
                return RedirectToAction(nameof(ViewClaimSubmissions));
            }
            else
            {
                return RedirectToAction(nameof(ViewUnderwritingSubmissions));
            }
        }
        // Controllers/UserController.cs
        public IActionResult SubmissionSuccess()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetFormFields()
        {
            var fields = _context.FormFields
                .Select(f => new { f.Id, f.Label })
                .ToList();
            return Json(fields);
        }

        [HttpGet]
        public IActionResult GetClaimSubmissionsJson()
        {
            var userEmail = User.Identity?.Name;
            var userCompany = _context.ApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            if (userCompany == null)
                return Unauthorized();

            // 1. Fetch form fields for dynamic column headers
            var formFields = _context.FormFields
                .Where(f => f.FormType == FormType.Claim && f.CompanyId == userCompany.ClientCompanyId)
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
                .Where(f => f.FormType == FormType.Claim && f.CompanyId == userCompany.ClientCompanyId)
                .OrderByDescending(sf => sf.SubmittedAt)
                .Select(sf => new
                {
                    Id = sf.Id,
                    SubmittedAt = sf.SubmittedAt,
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
                Values = sf.Values.ToDictionary(
                    v => v.FormFieldId.ToString(),
                    v => v
                )
            }).ToList();

            return Json(new { fields = formFields, data = submissions });
        }
        [Breadcrumb("Claims")]
        [HttpGet]
        public IActionResult ViewClaimSubmissions()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetUnderwritingSubmissionsJson()
        {
            var userEmail = User.Identity?.Name;
            var userCompany = _context.ApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            if (userCompany == null)
                return Unauthorized();

            // 1. Fetch form fields for dynamic column headers
            var formFields = _context.FormFields
                .Where(f => f.FormType == FormType.Underwriting && f.CompanyId == userCompany.ClientCompanyId)
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
                .Where(f => f.FormType == FormType.Underwriting && f.CompanyId == userCompany.ClientCompanyId)
                .OrderByDescending(sf => sf.SubmittedAt)
                .Select(sf => new
                {
                    Id = sf.Id,
                    SubmittedAt = sf.SubmittedAt,
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
                Values = sf.Values.ToDictionary(
                    v => v.FormFieldId.ToString(),
                    v => v
                )
            }).ToList();

            return Json(new { fields = formFields, data = submissions });
        }
        [Breadcrumb("Underwritings")]
        [HttpGet]
        public IActionResult ViewUnderwritingSubmissions()
        {
            return View();
        }
        [HttpGet]
        [Breadcrumb("Edit Claims", FromAction = nameof(ViewClaimSubmissions))]
        public IActionResult EditClaimForm(int id)
        {
            var submission = _context.SubmittedForms
                .Include(sf => sf.Values)
                .FirstOrDefault(sf => sf.Id == id);

            if (submission == null) return NotFound();

            // 1. Fetch only dynamic fields belonging to this submission's form type context
            var fields = _context.FormFields
                .Where(f => f.FormType == submission.FormType)
                .ToList();

            // 2. Map everything to the strongly-typed view model
            var viewModel = new EditSubmissionViewModel
            {
                SubmissionId = id,
                FormType = submission.FormType,
                Fields = fields.Select(f => new EditFieldViewModel
                {
                    Field = f,
                    CurrentValue = submission.Values.FirstOrDefault(v => v.FormFieldId == f.Id)?.Value!
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditClaimForm(int id, EditSubmissionViewModel postModel, IFormCollection form)
        {
            var submission = _context.SubmittedForms
                .Include(sf => sf.Values)
                .FirstOrDefault(sf => sf.Id == id);

            if (submission == null) return NotFound();

            // Pull fields specifically bound to this layout type 
            var fields = _context.FormFields.Where(f => f.FormType == submission.FormType).ToList();

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

            await _context.SaveChangesAsync();
            return RedirectToAction("ViewSubmissions");
        }
        // POST: /Claim/DeleteSubmission/5
        [HttpPost]
        public IActionResult DeleteSubmission(int id)
        {
            var submission = _context.SubmittedForms
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

            _context.SubmittedValues.RemoveRange(submission.Values);
            _context.SubmittedForms.Remove(submission);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}