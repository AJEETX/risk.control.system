using Google.Api;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

using SkiaSharp;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Report Templates")]
    public class ReportTemplateController : Controller
    {
        private readonly ApplicationDbContext context;

        public ReportTemplateController(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task<IActionResult> Index()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var companyUser = context.ClientCompanyApplicationUser
                .Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);

            var templates = await context.ReportTemplates
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.DocumentIds)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.Questions)
                    .Where(q => q.ClientCompanyId == companyUser.ClientCompanyId && q.Basetemplate && q.OriginalTemplateId == null)
                .ToListAsync();

            return View(templates);
        }

        [Breadcrumb(" Detail", FromAction = "Index")]
        public async Task<IActionResult> Details(long id)
        {
            var template = await context.ReportTemplates
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.DocumentIds)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.Questions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            return View(template);
        }

        // Controller method for adding FaceId
        [HttpGet]
        public async Task<IActionResult> GetFaceIdDetails(long faceId)
        {
            var face = await context.DigitalIdReport
                .Where(f => f.Id == faceId)
                .Select(f => new
                {
                    f.Id,
                    f.ReportName,
                    ReportType = f.ReportType.ToString() // Convert the ReportType enum to string
                })
                .FirstOrDefaultAsync();

            if (face == null)
            {
                return Json(new { success = false, message = "FaceId not found." });
            }

            return Json(new
            {
                success = true,
                faceId = face
            });
        }
        [HttpPost]
        public IActionResult AddFaceId(long locationId, string IdIName, DigitalIdReportType ReportType)
        {
            var location = context.LocationTemplate.Include(l => l.FaceIds).FirstOrDefault(l => l.Id == locationId);
            var faceId = new DigitalIdReport
            {
                IdName = IdIName,
                ReportType = ReportType
            };
            location.FaceIds.Add(faceId);
            
            context.LocationTemplate.Update(location);
           context.SaveChanges();

            return Json(new { locationId = locationId, newFaceId = faceId.Id });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateFaceId(long id, string newName, string newReportType)
        {
            // Find the FaceId in the database
            var faceId = await context.DigitalIdReport.FindAsync(id);
            if (faceId == null)
            {
                return NotFound();
            }

            // Update the properties
            faceId.IdName = newName;
            faceId.ReportType = Enum.TryParse(newReportType, out DigitalIdReportType reportType) ? reportType : faceId.ReportType;

            // Save the changes
            await context.SaveChangesAsync();

            // Return the updated FaceId back to the client
            return Json(new
            {
                success = true,
                message = "FaceId updated successfully",
                updatedFaceId = new
                {
                    Id = faceId.Id,
                    Name = faceId.IdName,
                    ReportType = faceId.ReportType.ToString()
                }
            });
        }

        // Controller method for adding FaceId

        [HttpGet]
        public async Task<IActionResult> GetDocumentIdDetails(long docId)
        {
            var doc = await context.DocumentIdReport
                .Where(d => d.Id == docId)
                .Select(d => new
                {
                    d.Id,
                    d.IdName,
                    DocumentType = d.ReportType.ToString() // Convert the DocumentIdReportType enum to string
                })
                .FirstOrDefaultAsync();

            if (doc == null)
            {
                return Json(new { success = false, message = "DocumentId not found." });
            }

            return Json(new
            {
                success = true,
                documentId = doc
            });
        }
        [HttpPost]
        public IActionResult AddDocId(long locationId, string IdIName, DocumentIdReportType ReportType)
        {
            var location = context.LocationTemplate.Include(l => l.DocumentIds).FirstOrDefault(l => l.Id == locationId);
            var faceId = new DocumentIdReport
            {
                IdName = IdIName,
                 ReportType = ReportType
            };
            location.DocumentIds.Add(faceId);

            context.LocationTemplate.Update(location);
            context.SaveChanges();

            return Json(new { locationId = locationId, newFaceId = faceId.Id });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateDocumentId(long id, string newName, string newDocumentType)
        {
            // Find the DocumentId in the database
            var documentId = await context.DocumentIdReport.FindAsync(id);
            if (documentId == null)
            {
                return NotFound();
            }

            // Update the properties
            documentId.IdName = newName;
            documentId.ReportType = Enum.TryParse(newDocumentType, out DocumentIdReportType documentType) ? documentType : documentId.ReportType;

            // Save the changes
            await context.SaveChangesAsync();

            // Return the updated DocumentId back to the client
            return Json(new
            {
                success = true,
                message = "DocumentId updated successfully",
                updatedDocumentId = new
                {
                    Id = documentId.Id,
                    Name = documentId.IdName,
                    DocumentType = documentId.ReportType.ToString()
                }
            });
        }

        [HttpGet]
        public IActionResult GetQuestionDetails(long questionId)
        {
            // Retrieve the question from the database using the questionId
            var question = context.Questions
                .Where(q => q.Id == questionId)
                .FirstOrDefault();

            if (question == null)
            {
                // Return a failure response if the question was not found
                return Json(new { success = false, message = "Question not found." });
            }

            // Return the question details as JSON
            return Json(new { success = true, question = question });
        }

        [HttpPost]
        public IActionResult UpdateQuestion(long id, string newQuestionText, string newQuestionType)
        {
            var question = context.Questions.FirstOrDefault(q => q.Id == id);
            if (question == null)
            {
                return Json(new { success = false, message = "Question not found." });
            }

            question.QuestionText = newQuestionText;
            question.QuestionType = newQuestionType;

            context.SaveChanges();

            return Json(new { success = true, updatedQuestion = question });
        }

    }
}
