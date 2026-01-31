using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace risk.control.system.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("AcceptCookies")]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptCookies()
        {
            Response.Cookies.Append("cookieConsent", "Accepted", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(265),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            Response.Cookies.Append("analyticsCookies", true.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            Response.Cookies.Append("perfomanceCookies", true.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            return Ok(new { success = true, message = "Accept-All-Cookie consent saved" });
        }

        [AllowAnonymous]
        [HttpPost("RevokeCookies")]
        [ValidateAntiForgeryToken]
        public IActionResult RevokeCookies()
        {
            Response.Cookies.Append("cookieConsent", "Accepted", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365), // Persistent for 1 year
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            Response.Cookies.Append("analyticsCookies", false.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            Response.Cookies.Append("perfomanceCookies", false.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            return Ok(new { success = true, message = "Accept-only-required Cookie consent saved" });
        }

        [AllowAnonymous]
        [HttpPost("SavePreferences")]
        [ValidateAntiForgeryToken]
        public IActionResult SavePreferences([FromBody] CookiePreferences preferences)
        {
            if (preferences == null)
            {
                return BadRequest(new { success = false, message = "Invalid data received." });
            }

            Response.Cookies.Append("cookieConsent", "Accepted", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            Response.Cookies.Append("analyticsCookies", preferences.AnalyticsCookies.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            Response.Cookies.Append("perfomanceCookies", preferences.PerfomanceCookies.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(new { success = true, message = "Cookie Preferences saved" });
        }
    }

    public class CookiePreferences
    {
        public bool AnalyticsCookies { get; set; }
        public bool PerfomanceCookies { get; set; }
    }
}