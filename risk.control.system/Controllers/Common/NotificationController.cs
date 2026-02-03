using System.Runtime.Serialization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Common
{
    [ApiExplorerSettings(IgnoreApi = true)]

    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly int maxCountReached = 10;
        private readonly INotificationService service;
        private readonly ISmsService smsService;
        private readonly ILogger<NotificationController> logger;

        public NotificationController(INotificationService service, ISmsService smsService, ILogger<NotificationController> logger)
        {
            this.service = service;
            this.smsService = smsService;
            this.logger = logger;
        }
        [HttpPost("ClearAll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllNotifications()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                await service.ClearAll(userEmail); ;
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while clear notifications for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("MarkAsRead")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(NotificationRequest request)
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                await service.MarkAsRead(request.Id, userEmail);
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while marking notifications for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetNotifications")]
        public async Task<ActionResult> GetNotifications()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var notifications = await service.GetNotifications(userEmail);
                var activeNotifications = notifications.Select(n => new { Id = n.StatusNotificationId, Symbol = n.Symbol, n.Message, n.Status, CreatedAt = GetTimeAgo(n.CreatedAt), user = n.NotifierUserEmail });
                return Ok(new
                {
                    Data = activeNotifications?.Take(maxCountReached).ToList(),
                    total = notifications.Count,
                    MaxCountReached = notifications.Count > maxCountReached,
                    MaxCount = maxCountReached
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting notifications for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        private static string GetTimeAgo(DateTime createdAt)
        {
            var timeSpan = DateTime.Now - createdAt;

            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.Seconds} seconds ago";
            if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.Minutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{timeSpan.Hours} hours ago";
            if (timeSpan.TotalHours < 48)
                return "Yesterday";
            if (timeSpan.TotalDays < 7)
                return createdAt.DayOfWeek.ToString(); // Returns 'Wednesday', 'Thursday', etc.

            return $"{(int)timeSpan.TotalDays} days ago";
        }
    }

    public enum CountryCode
    {
        [EnumMember(Value = "au")]
        au,

        [EnumMember(Value = "in")]
        In // capitalized, avoids keyword issue
    }
    public class NotificationRequest
    {
        public int Id { get; set; }
    }
}