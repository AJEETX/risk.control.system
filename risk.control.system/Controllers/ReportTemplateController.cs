using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;
namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class ReportTemplateController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly ICloneReportService cloneService;
        private readonly INotyfService notifyService;
        private readonly ILogger<ReportTemplateController> logger;

        public ReportTemplateController(ApplicationDbContext context, ICloneReportService cloneService, INotyfService notifyService, ILogger<ReportTemplateController> logger)
        {
            this.context = context;
            this.cloneService = cloneService;
            this.notifyService = notifyService;
            this.logger = logger;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb(" Report Template", FromAction = "Index")]
        public async Task<IActionResult> Profile()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var companyUser = context.ClientCompanyApplicationUser
                .Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);

            var templates = await context.ReportTemplates
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.DocumentIds)
                     .Include(r => r.LocationReport)
                    .ThenInclude(l => l.MediaReports)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.Questions)
                    .Where(q => q.ClientCompanyId == companyUser.ClientCompanyId && !q.IsDeleted && q.UpdatedBy != "system")
                .ToListAsync();

            return View(templates);
        }

        [Breadcrumb(" Detail", FromAction = "Profile")]
        public async Task<IActionResult> Details(long id)
        {
            var template = await context.ReportTemplates
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.AgentIdReport)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.MediaReports)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.DocumentIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.Questions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (template == null)
            {
                return NotFound();
            }
            return View(template);
        }

        [Breadcrumb("Clone Detail", FromAction = "Profile")]
        public async Task<IActionResult> CloneDetails(long templateId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var newTemplate = await cloneService.CreateCloneReportTemplate(templateId, currentUserEmail);
                notifyService.Success($"Report cloned successfully");
                return View(newTemplate);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                logger.LogError(ex.StackTrace);
                notifyService.Error($"Issue cloning Report!!!");
                return RedirectToAction(nameof(Profile));
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var activated = await cloneService.Activate(id);
                if (activated)
                {
                    return Json(new { success = true, message = "Report activated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Report activation failed!" });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                return Json(new { success = false, message = "Report activation failed! Try again" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTemplate(long id)
        {
            try
            {
                var template = await context.ReportTemplates.FindAsync(id);
                if (template == null)
                {
                    return Json(new { success = false, message = "Template not found." });
                }

                if (template.IsActive)
                {
                    return Json(new { success = false, message = "Active templates cannot be deleted." });
                }

                template.IsDeleted = true;
                context.ReportTemplates.Update(template);
                var affected = await context.SaveChangesAsync() > 0;
                if (affected)
                {
                    return Json(new { success = true, message = "Template deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Error to delete Template" });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                return Json(new { success = false, message = "Exception to delete Template" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionDetails(long questionId)
        {
            // Retrieve the question from the database using the questionId
            var question = await context.Questions.FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                // Return a failure response if the question was not found
                return Json(new { success = false, message = "Question not found." });
            }

            // Return the question details as JSON
            return Json(new { success = true, question = question });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(long locationId, string? optionsInput, bool isRequired, string newQuestionText, string newQuestionType)
        {
            try
            {
                var location = await context.LocationReport.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == locationId);
                if (location == null)
                {
                    return Json(new { success = false, message = "Location not found." });
                }
                var question = new Question
                {
                    QuestionText = newQuestionText,
                    QuestionType = newQuestionType,
                    Options = optionsInput,
                    IsRequired = isRequired
                };
                location.Questions.Add(question);

                await context.SaveChangesAsync();

                return Json(new { success = true, updatedQuestion = question });

            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return Json(new { success = false, message = "Add Question Error." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(long id, long locationId)
        {
            try
            {
                var question = await context.Questions.FirstOrDefaultAsync(q => q.Id == id);
                if (question == null)
                {
                    return Json(new { success = false, message = "Question not found." });
                }
                var location = await context.LocationReport
                    .Include(l => l.Questions)
                    .FirstOrDefaultAsync(l => l.Id == locationId);

                if (location.Questions.Count > 1)
                {
                    context.Questions.Remove(question);
                    await context.SaveChangesAsync();
                    return Json(new { success = true, Id = id });
                }
                return Json(new { success = false, message = "Single Question not deleted." });

            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return Json(new { success = false, message = "Error Question Delete." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation(long id, bool locationDeletable = true)
        {
            try
            {
                if (!locationDeletable)
                {
                    return Json(new { success = false, message = "Single Location not DELETED." });
                }
                var location = await context.LocationReport
                    .Include(l => l.Questions)
                    .Include(l => l.AgentIdReport)
                    .Include(l => l.FaceIds)
                    .Include(l => l.DocumentIds)
                    .Include(l => l.MediaReports)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (location == null)
                {
                    return Json(new { success = false, message = "Location not found." });
                }

                context.Questions.RemoveRange(location.Questions);
                context.AgentIdReport.Remove(location.AgentIdReport);
                context.DigitalIdReport.RemoveRange(location.FaceIds);
                context.DocumentIdReport.RemoveRange(location.DocumentIds);
                context.MediaReport.RemoveRange(location.MediaReports);

                context.LocationReport.Remove(location);
                await context.SaveChangesAsync();

                return Json(new { success = true, Id = id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return Json(new { success = false, message = "Error Location Delete." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLocation([FromBody] SaveLocationDto model)
        {
            try
            {
                var location = await context.LocationReport
                    .Include(l => l.AgentIdReport)
                    .Include(l => l.FaceIds)
                    .Include(l => l.DocumentIds)
                    .Include(l => l.MediaReports)
                    .FirstOrDefaultAsync(l => l.Id == model.LocationId);

                if (location == null)
                    return Json(new { success = false, message = "Location not found." });

                location.LocationName = model.LocationName;

                // Update AgentId
                var agent = location.AgentIdReport;
                if (agent != null)
                    agent.Selected = model.AgentId.Selected;

                // Update FaceIds
                foreach (var f in model.FaceIds)
                {
                    var face = location.FaceIds.FirstOrDefault(x => x.Id == f.Id);
                    if (face != null)
                        face.Selected = f.Selected;
                }

                // Update DocumentIds
                foreach (var d in model.DocumentIds)
                {
                    var doc = location.DocumentIds.FirstOrDefault(x => x.Id == d.Id);
                    if (doc != null)
                        doc.Selected = d.Selected;
                }

                // Update MediaReports
                foreach (var m in model.MediaReports)
                {
                    var media = location.MediaReports.FirstOrDefault(x => x.Id == m.Id);
                    if (media != null)
                        media.Selected = m.Selected;
                }

                await context.SaveChangesAsync();

                return Json(new { success = true, message = "Location saved successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return Json(new { success = false, message = "Error Location Save." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloneLocation(long locationId, long reportTemplateId)
        {
            try
            {
                var original = await context.LocationReport
                    .Include(l => l.AgentIdReport)
                    .Include(l => l.FaceIds)
                    .Include(l => l.DocumentIds)
                    .Include(l => l.MediaReports)
                    .Include(l => l.Questions)
                    .FirstOrDefaultAsync(l => l.Id == locationId);

                if (original == null)
                    return Json(new { success = false, message = "Original location not found." });

                var locationName = original.LocationName + " (Copy)";
                var reportTemplate = await context.ReportTemplates.Include(r => r.LocationReport).FirstOrDefaultAsync(r => r.Id == reportTemplateId);

                if (reportTemplate == null)
                {
                    return Json(new { success = false, message = "Report Template not found." });
                }

                var hasAnyLocationName = reportTemplate.LocationReport.Any(l => l.LocationName.ToLower() == locationName.ToLower());

                if (hasAnyLocationName)
                {
                    return Json(new { success = false, message = $"Location name {locationName} exists." });
                }
                // Create clone (deep copy)
                var clone = new LocationReport
                {
                    LocationName = original.LocationName + " (Copy)",
                    ReportTemplateId = reportTemplateId,
                    Created = DateTime.Now,
                    AgentIdReport = new AgentIdReport
                    {
                        Selected = original.AgentIdReport.Selected,
                        IsRequired = original.AgentIdReport.IsRequired,
                        ReportName = original.AgentIdReport.ReportName,                                          // You can set other properties of Agent here if needed
                        ReportType = original.AgentIdReport.ReportType,  // Default agent
                    },
                    FaceIds = original.FaceIds.Select(f => new FaceIdReport
                    {
                        IsRequired = f.IsRequired,
                        Selected = f.Selected,
                        ReportName = f.ReportName,
                        Has2Face = f.Has2Face,
                        ReportType = f.ReportType
                    }).ToList(),
                    DocumentIds = original.DocumentIds.Select(d => new DocumentIdReport
                    {
                        IsRequired = d.IsRequired,
                        Selected = d.Selected,
                        HasBackImage = d.HasBackImage,
                        ReportName = d.ReportName,
                        ReportType = d.ReportType
                    }).ToList(),
                    MediaReports = original.MediaReports.Select(m => new MediaReport
                    {
                        IsRequired = m.IsRequired,
                        Selected = m.Selected,
                        ReportName = m.ReportName,
                        MediaType = m.MediaType,
                        MediaExtension = m.MediaExtension
                    }).ToList(),
                    Questions = original.Questions.Select(q => new Question
                    {
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        IsRequired = q.IsRequired
                    }).ToList()
                };

                context.LocationReport.Add(clone);
                await context.SaveChangesAsync();

                // Render partial view for new location card
                var html = await this.RenderViewAsync("_LocationCardPartial", clone, true);
                return Json(new { success = true, html });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
