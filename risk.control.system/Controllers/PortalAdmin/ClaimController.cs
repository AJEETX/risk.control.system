using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
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
        public IActionResult FillForm()
        {
            // Load all the dynamic fields configured by the Admin
            var fields = _context.FormFields.ToList();
            return View(fields);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitForm(IFormCollection form, List<IFormFile> files)
        {
            var fields = _context.FormFields.ToList();
            var submission = new SubmittedForm { SubmittedAt = DateTime.UtcNow };

            foreach (var field in fields)
            {
                var valueStr = "";

                if (field.FieldType == "file")
                {
                    // Find file associated with this dynamic field ID
                    var uploadedFile = Request.Form.Files.FirstOrDefault(f => f.Name == $"field_{field.Id}");
                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                        Directory.CreateDirectory(uploadsFolder); // Ensure directory exists
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(fileStream);
                        }
                        valueStr = "/uploads/" + uniqueFileName; // Saved file path
                    }
                }
                else if (field.FieldType == "date")
                {
                    // 1. Get the raw string submitted by the jQuery Datepicker (e.g., "17-07-2026")
                    string rawDate = form[$"field_{field.Id}"].ToString();

                    if (!string.IsNullOrWhiteSpace(rawDate))
                    {
                        // 2. Parse the custom "dd-mm-yyyy" format safely
                        if (DateTime.TryParseExact(rawDate, "dd-MM-yyyy",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out DateTime parsedDate))
                        {
                            // 3. Convert it to ISO standard "yyyy-MM-dd" for consistent DB storage
                            valueStr = parsedDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            // Fallback in case parsing fails (saves raw input)
                            valueStr = rawDate;
                        }
                    }
                }
                else
                {
                    // Grab the value from the form data using the field ID as the key
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

            return RedirectToAction("SubmissionSuccess");
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
                .Select(sf => new
                {
                    Id = sf.Id,
                    SubmittedAt = sf.SubmittedAt.ToString("g"),
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
        public IActionResult ViewSubmissions()
        {
            return View();
        }
        [HttpGet]
        public IActionResult EditForm(int id)
        {
            var submission = _context.SubmittedForms
                .Include(sf => sf.Values)
                .FirstOrDefault(sf => sf.Id == id);

            if (submission == null) return NotFound();

            var fields = _context.FormFields.ToList();

            // Map to our unified, strongly-typed model
            var viewModel = new EditSubmissionViewModel
            {
                SubmissionId = id,
                Fields = fields.Select(f => new EditFieldViewModel
                {
                    Field = f,
                    CurrentValue = submission.Values.FirstOrDefault(v => v.FormFieldId == f.Id)?.Value
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: /Claim/EditForm/5
        [HttpPost]
        public async Task<IActionResult> EditForm(int id, IFormCollection form, List<IFormFile> files)
        {
            var submission = _context.SubmittedForms
                .Include(sf => sf.Values)
                .FirstOrDefault(sf => sf.Id == id);

            if (submission == null) return NotFound();

            var fields = _context.FormFields.ToList();

            foreach (var field in fields)
            {
                var existingValue = submission.Values.FirstOrDefault(v => v.FormFieldId == field.Id);
                var newValueStr = "";

                if (field.FieldType == "file")
                {
                    var uploadedFile = Request.Form.Files.FirstOrDefault(f => f.Name == $"field_{field.Id}");
                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        // Process and save new file
                        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                        Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(fileStream);
                        }
                        newValueStr = "/uploads/" + uniqueFileName;

                        // Optionally delete the old physical file from disk
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
                        // No new file uploaded; retain the existing path
                        newValueStr = existingValue?.Value;
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
                    existingValue.Value = newValueStr;
                }
                else
                {
                    submission.Values.Add(new SubmittedValue { FormFieldId = field.Id, Value = newValueStr });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ViewSubmissions"); // Redirects back to your DataTable list
        }

        // POST: /Claim/DeleteSubmission/5
        [HttpPost]
        public IActionResult DeleteSubmission(int id)
        {
            var submission = _context.SubmittedForms
                .Include(sf => sf.Values)
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