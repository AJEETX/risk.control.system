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
            // Check if the user has given cookie consent
            var cookieConsent = context.Request.Cookies["CookieConsent"];
            context.Items["HasConsent"] = cookieConsent == "Accepted";

            // Call the next middleware in the pipeline
            await _next(context);

            //// Inject analytics scripts after the response is generated
            //if (cookieConsent == "Accepted" && context.Response.ContentType != null &&
            //    context.Response.ContentType.Contains("text/html"))
            //{
            //    context.Response.Body.Seek(0, SeekOrigin.Begin);
            //    var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            //    context.Response.Body.Seek(0, SeekOrigin.Begin);

            //    // Inject analytics scripts at the end of the body
            //    var script = @"
            //    <script async src='https://www.googletagmanager.com/gtag/js?id=YOUR_ANALYTICS_ID'></script>
            //    <script>
            //        window.dataLayer = window.dataLayer || [];
            //        function gtag() { dataLayer.push(arguments); }
            //        gtag('js', new Date());
            //        gtag('config', 'YOUR_ANALYTICS_ID');
            //    </script>";

            //    body = body.Replace("</body>", $"{script}</body>");
            //    await context.Response.WriteAsync(body);
            //}
        }
    }

}
