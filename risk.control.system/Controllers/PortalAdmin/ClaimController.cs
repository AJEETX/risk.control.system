using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    [Breadcrumb("Company Settings ")]
    public class ClaimController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ClaimController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult CreateClaimForm(FormType type = FormType.Claim)
        {
            // Filter out rows to ONLY load the form layouts intended for the active view target
            var existingFields = _context.FormFields
                .Where(f => f.FormType == type)
                .ToList();

            // Track selected target form type to sync the dropdown element selection on page reload
            ViewBag.SelectedFormType = type.ToString();

            return View(existingFields);
        }

        [HttpPost]
        public IActionResult CreateClaimForm(FormType targetFormType, List<FormField> fields)
        {
            // 1. Clear out ONLY the existing layout mapping records for the specific target group
            var oldFields = _context.FormFields.Where(f => f.FormType == targetFormType).ToList();
            _context.FormFields.RemoveRange(oldFields);
            fields = fields.Where(f => !string.IsNullOrWhiteSpace(f.Label)).ToList();
            if (fields != null && fields.Any())
            {
                foreach (var field in fields)
                {
                    // 2. Explicitly bind the target form type category context to each field object
                    field.FormType = targetFormType;
                }

                _context.FormFields.AddRange(fields);
            }

            _context.SaveChanges();
            TempData["SuccessMessage"] = $"{targetFormType} Form structure saved successfully!";

            return RedirectToAction(nameof(CreateClaimForm), new { type = targetFormType });
        }
        [HttpGet]
        public IActionResult FillClaimForm(FormType type = FormType.Claim)
        {
            var model = new FillFormViewModel
            {
                FormType = type,
                Fields = _context.FormFields.Where(f => f.FormType == type).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitForm(FillFormViewModel postModel, IFormCollection form)
        {
            // Access the targeted form type directly from the strongly-typed incoming parameter
            FormType currentFormType = postModel.FormType;

            var fields = _context.FormFields.Where(f => f.FormType == currentFormType).ToList();
            var submission = new SubmittedForm
            {
                SubmittedAt = DateTime.UtcNow,
                FormType = currentFormType
            };

            foreach (var field in fields)
            {
                var valueStr = "";

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

            return RedirectToAction("ViewClaimSubmissions");
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

        // 2. Ensure your submissions JSON returns the FormFieldId
        [HttpGet]
        public IActionResult GetSubmissionsJson()
        {
            var submissions = _context.SubmittedForms
                .Include(sf => sf.Values)
                .ThenInclude(sv => sv.FormField)
                .OrderByDescending(sf => sf.SubmittedAt)
                .Where(f => f.FormType == FormType.Claim) // Filter to only include Claim submissions
                .Select(sf => new
                {
                    Id = sf.Id,
                    SubmittedAt = sf.SubmittedAt.ToString("o"),
                    Fields = sf.Values.Select(v => new
                    {
                        FormFieldId = v.FormFieldId, // Essential for mapping values to the correct column
                        Label = v.FormField.Label,
                        Value = v.Value,
                        Type = v.FormField.FieldType
                    }).ToList()
                })
                .ToList();

            return Json(new { data = submissions });
        }
        // Render the HTML view shell
        [HttpGet]
        public IActionResult ViewClaimSubmissions()
        {
            return View();
        }
        [HttpGet]
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