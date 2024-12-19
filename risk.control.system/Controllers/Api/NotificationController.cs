using System.Web;

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
        private readonly ISmsService smsService;
        private readonly IHttpClientService httpClientService;

        public NotificationController(INotificationService service, ISmsService smsService, IHttpClientService httpClientService)
        {
            this.service = service;
            this.smsService = smsService;
            this.httpClientService = httpClientService;
        }

        [AllowAnonymous]
        [HttpGet("GetClientIp")]
        public async Task<ActionResult> GetClientIp(CancellationToken ct, string url = "", string latlong = "")
        {
            try
            {
                var decodedUrl = HttpUtility.UrlDecode(url);
                var user = HttpContext.User.Identity.Name;
                var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
                var ipAddress = HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var ipAddressWithoutPort = ipAddress?.Split(':')[0];
                var isWhiteListed = service.IsWhiteListIpAddress(HttpContext.Connection.RemoteIpAddress);
                var lat = latlong.Substring(0, latlong.IndexOf(","));
                var lng = latlong.Substring(latlong.IndexOf(",")+1);
                var address =await httpClientService.GetAddress(lat, lng);

                var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, decodedUrl,user, isAuthenticated);
                var longLatString = ipApiResponse?.lat.GetValueOrDefault().ToString() + "," + ipApiResponse?.lon.GetValueOrDefault().ToString();
                var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={latlong}&zoom=14&size=560x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{latlong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                var response = new
                {
                    IpAddress = string.IsNullOrWhiteSpace(ipAddressWithoutPort) ? ipApiResponse?.query : ipAddressWithoutPort,
                    Country = address?.features[0].properties.country,
                    Region = address?.features[0].properties?.state,
                    City = address?.features[0].properties?.county,
                    District = address?.features[0].properties?.city,
                    PostCode = address?.features[0].properties?.postcode,
                    Isp = ipApiResponse?.isp,
                    Longitude = address?.features[0].properties.lon,
                    Latitude = address?.features[0].properties.lat,
                    mapUrl = mapUrl,
                    whiteListed = false,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("GetClientSystemIp")]
        public async Task<ActionResult> GetClientSystemIp(CancellationToken ct, string url = "")
        {
            try
            {
                var decodedUrl = HttpUtility.UrlDecode(url);
                var user = HttpContext.User.Identity.Name;
                var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
                var ipAddress = HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var ipAddressWithoutPort = ipAddress?.Split(':')[0];
                var isWhiteListed = service.IsWhiteListIpAddress(HttpContext.Connection.RemoteIpAddress);
                var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, decodedUrl, user, isAuthenticated);
                var longLatString = ipApiResponse?.lat.GetValueOrDefault().ToString() + "," + ipApiResponse?.lon.GetValueOrDefault().ToString();
                var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=14&size=560x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
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
                    mapUrl = mapUrl,
                    whiteListed = false,
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
            string logo = "https://icheckify-demo.azurewebsites.net/img/iCheckifyLogo.png";
            string? attachments = $"<a href='{logo}'>team</a>";
            var finalMessage = $"{message} Date: {DateTime.Now.ToString("dd-MMM-yyyy HH:mm")} {logo}";
            await smsService.DoSendSmsAsync("+" + mobile, finalMessage);
            return Ok();
        }
    }
}