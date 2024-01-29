using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("sms")]
        public async Task<IActionResult> SendSMS(string mobile = "61432854196", string message = "SMS fom iCheckify team")
        {
            string device = "0";
            long? timestamp = null;
            bool isMMS = false;
            string logo = "https://icheckify-demo.azurewebsites.net/img/iCheckifyLogo.png";

            Uri address = new Uri("http://tinyurl.com/api-create.php?url=" + logo);
            System.Net.WebClient client = new System.Net.WebClient();
            string tinyUrl = client.DownloadString(address);

            string? attachments = $"<a href='{logo}'>team</a>";
            var finalMessage = $"{message} Date: {DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm")} {logo}";
            bool priority = true;
            var response = SMS.API.SendSingleMessage("+" + mobile, finalMessage, device, timestamp, isMMS, null, priority);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("mms")]
        public async Task<IActionResult> SendMMS(string mobile = "+61432854196", string message = "Testing fom iCheckify")
        {
            string attachments = "https://example.com/images/footer-logo.png,https://example.com/downloads/sms-gateway/images/section/create-chat-bot.png";
            Dictionary<string, object> response = SMS.API.SendSingleMessage(mobile, message, "0", null, true, attachments, true);
            return Ok(response);
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