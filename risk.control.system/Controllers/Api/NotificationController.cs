using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService service;

        public NotificationController(INotificationService service)
        {
            this.service = service;
        }

        [AllowAnonymous]
        [HttpGet("GetClientIp")]
        public async Task<ActionResult> GetClientIp(CancellationToken ct)
        {
            try
            {
                var user = HttpContext.User.Identity.Name;
                var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
                var ipAddress = HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var ipAddressWithoutPort = ipAddress?.Split(':')[0];

                var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, user, isAuthenticated);
                var longLatString = ipApiResponse?.lat.GetValueOrDefault().ToString() + "," + ipApiResponse?.lon.GetValueOrDefault().ToString();
                var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=12&size=560x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Applicationsettings.GMAPData}";
                var response = new
                {
                    IpAddress = string.IsNullOrWhiteSpace(ipAddressWithoutPort) ? ipApiResponse?.query : ipAddressWithoutPort,
                    Country = ipApiResponse?.country,
                    Region = ipApiResponse?.regionName,
                    City = ipApiResponse?.city,
                    District = ipApiResponse?.district,
                    PostCode = ipApiResponse?.zip,
                    Isp = ipApiResponse?.isp,
                    Longitude = ipApiResponse?.lon.GetValueOrDefault(),
                    Latitude = ipApiResponse?.lat.GetValueOrDefault(),
                    mapUrl = mapUrl
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("schedule")]
        public async Task<IActionResult> Schedule(ClientSchedulingMessage message)
        {
            string baseUrl = HttpContext.Request.GetDisplayUrl().Replace(HttpContext.Request.Path, "");
            message.BaseUrl = baseUrl;
            var claim = await service.SendVerifySchedule(message);
            if (claim == null)
            {
                return BadRequest();
            }
            return Ok(claim);
        }

        [AllowAnonymous]
        [HttpGet("ConfirmSchedule")]
        public async Task<IActionResult> ConfirmSchedule(string id, string confirm = "N")
        {
            var claim = await service.ReplyVerifySchedule(id, confirm);
            if (claim == null)
            {
                return BadRequest();
            }
            return Redirect("/page/confirm.html");
        }

        [AllowAnonymous]
        [HttpPost("sms")]
        public async Task<IActionResult> SendSingleSMS(string mobile = "61432854196", string message = "SMS fom iCheckify team")
        {
            string device = "0";
            long? timestamp = null;
            bool isMMS = false;
            string logo = "https://icheckify-demo.azurewebsites.net/img/iCheckifyLogo.png";
            string? attachments = $"<a href='{logo}'>team</a>";
            var finalMessage = $"{message} Date: {DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm")} {logo}";
            bool priority = true;
            var response = SMS.API.SendSingleMessage("+" + mobile, finalMessage, device, timestamp, isMMS, attachments, priority);
            return Ok(response);
        }
    }
}