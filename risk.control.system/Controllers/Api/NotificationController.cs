using System.Runtime.Serialization;

using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

namespace risk.control.system.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly int maxCountReached = 10;
        private readonly INotificationService service;
        private readonly ISmsService smsService;

        public NotificationController(INotificationService service, ISmsService smsService)
        {
            this.service = service;
            this.smsService = smsService;
        }
        [HttpPost("ClearAll")]
        public async Task<IActionResult> ClearAllNotifications()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            await service.ClearAll(userEmail); ;
            return Ok();
        }

        [HttpPost("MarkAsRead")]
        public async Task<IActionResult> MarkAsRead(NotificationRequest request)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            await service.MarkAsRead(request.Id, userEmail);
            return Ok();
        }
        [HttpGet("GetNotifications")]
        public async Task<ActionResult> GetNotifications()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var notifications = await service.GetNotifications(userEmail);
            var activeNotifications = notifications.Select(n => new { Id = n.StatusNotificationId, Symbol = n.Symbol, n.Message, n.Status, CreatedAt = GetTimeAgo(n.CreatedAt), user = n.NotifierUserEmail });
            return Ok(new { Data = activeNotifications?.Take(maxCountReached).ToList(), total = notifications.Count, MaxCountReached = notifications.Count > maxCountReached, MaxCount = maxCountReached });
        }
        //[AllowAnonymous]
        //[HttpGet("GetClientIp")]
        //public async Task<ActionResult> GetClientIp(CancellationToken ct, string url = "", string latlong = "")
        //{
        //    try
        //    {
        //        var decodedUrl = HttpUtility.UrlDecode(url);
        //        var user = HttpContext.User.Identity.Name;
        //        var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
        //        var ipAddress = HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        //        var ipAddressWithoutPort = ipAddress?.Split(':')[0];
        //        var isWhiteListed = service.IsWhiteListIpAddress(HttpContext.Connection.RemoteIpAddress);

        //        var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, decodedUrl, user, isAuthenticated, latlong);
        //        if (ipApiResponse == null)
        //        {
        //            return StatusCode(StatusCodes.Status500InternalServerError, "Error getting IP address");
        //        }
        //        var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={latlong}&zoom=15&size=600x250&maptype=roadmap&markers=color:red%7Clabel:S%7C{latlong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
        //        ipApiResponse.MapUrl = mapUrl;
        //        var response = new
        //        {
        //            IpAddress = string.IsNullOrWhiteSpace(ipAddressWithoutPort) ? ipApiResponse?.query : ipAddressWithoutPort,
        //            Country = ipApiResponse.country,
        //            Region = ipApiResponse?.regionName,
        //            City = ipApiResponse?.city,
        //            District = ipApiResponse?.district ?? ipApiResponse?.city,
        //            PostCode = ipApiResponse?.zip,
        //            Isp = ipApiResponse?.isp,
        //            Longitude = ipApiResponse.lon,
        //            Latitude = ipApiResponse.lat,
        //            mapUrl = ipApiResponse.MapUrl,
        //            whiteListed = false,
        //        };
        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //    }
        //}

        //[HttpPost("schedule")]
        //public async Task<IActionResult> Schedule(ClientSchedulingMessage message)
        //{
        //    string baseUrl = HttpContext.Request.GetDisplayUrl().Replace(HttpContext.Request.Path, "");
        //    message.BaseUrl = baseUrl;
        //    var claim = await service.SendVerifySchedule(message);
        //    if (claim == null)
        //    {
        //        return BadRequest();
        //    }
        //    return Ok(claim);
        //}

        //[HttpGet("ConfirmSchedule")]
        //public async Task<IActionResult> ConfirmSchedule(string id, string confirm = "N")
        //{
        //    var claim = await service.ReplyVerifySchedule(id, confirm);
        //    if (claim == null)
        //    {
        //        return BadRequest();
        //    }
        //    return Redirect("/page/confirm.html");
        //}

        [HttpPost("sms")]
        public async Task<IActionResult> SendSingleSMS(string countryCode = "au", string mobile = "61432854196", string message = "SMS fom iCheckify team")
        {
            string logo = "https://icheckify-demo.azurewebsites.net/img/iCheckifyLogo.png";
            string? attachments = $"<a href='{logo}'>team</a>";
            var finalMessage = $"{message}\n\n Date: {DateTime.Now.ToString("dd-MMM-yyyy HH:mm")}\n\n {logo}";
            await smsService.DoSendSmsAsync(countryCode.ToLower(), "+" + mobile, finalMessage);
            return Ok();
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