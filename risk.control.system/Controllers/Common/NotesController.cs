using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class NotesController : Controller
    {
        private readonly ICaseNotesService _caseNotesService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(ICaseNotesService caseNotesService, ILogger<NotesController> logger)
        {
            _caseNotesService = caseNotesService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNotes(SmsModel model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var (smsSent, count) = await _caseNotesService.SubmitNotes(userEmail, model.CaseId, model.Message!);
                if (smsSent)
                {
                    return Ok(new { message = "Notes added: Success", newCount = count });
                }
                return BadRequest("Notes Add Error !!!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred adding notes for Case {Id}", model.CaseId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}