using System.Text;

namespace risk.control.system.Middleware
{
    public class CookieConsentMiddleware
    {
        private readonly RequestDelegate _next;

        public CookieConsentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the "CookieConsent" cookie exists
            var cookieConsent = context.Request.Cookies["CookieConsent"];
            context.Items["HasCookieConsent"] = cookieConsent == "Accepted";

            await _next(context);

            // Inject analytics scripts after the response is generated
            //if (cookieConsent != "Accepted" && context.Response.ContentType != null && context.Response.ContentType.Contains("text/html"))
            //{
            //    // Inject a script that can trigger the popup on the client side
            //    var script = @"
            //    <script>
            //        window.onload = function() {
            //            if (!localStorage.getItem('cookieConsent')) {
            //                // Show your popup here
            //                alert('Please accept cookies to continue using this site.');
            //                // Store the consent in localStorage to avoid showing the popup again
            //                localStorage.setItem('cookieConsent', 'true');
            //                document.cookie = 'CookieConsent=Accepted; path=/; max-age=31536000'; // Set the cookie for 1 year
            //            }
            //        }
            //    </script>";

            //    context.Response.Body.Seek(0, SeekOrigin.Begin); // Move to the beginning of the response
            //    var responseBody = new StreamReader(context.Response.Body).ReadToEnd();
            //    responseBody = responseBody.Replace("</body>", $"{script}</body>"); // Append the script just before closing </body>

            //    var modifiedBytes = Encoding.UTF8.GetBytes(responseBody);
            //    context.Response.ContentLength = modifiedBytes.Length;
            //    await context.Response.Body.WriteAsync(modifiedBytes, 0, modifiedBytes.Length);
            //}
        }
    }
}