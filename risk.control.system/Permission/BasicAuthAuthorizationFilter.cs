using Hangfire.Dashboard;
using System.Text;

namespace risk.control.system.Permission
{
    public class BasicAuthAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            var request = httpContext.Request;
            var response = httpContext.Response;
            var authorization = request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                // Send 401 response with WWW-Authenticate to trigger login popup
                response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
                response.StatusCode = StatusCodes.Status401Unauthorized;
                return false;
            }

            try
            {
                // Decode Authorization header (Base64 username:password)
                var encodedCredentials = authorization.Substring(6); // Remove "Basic "
                var decodedAuthHeader = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var credentials = decodedAuthHeader.Split(':');

                if (credentials.Length != 2)
                    return false;

                string username = "admin", password = "admin";

#if !DEBUG
            username = Environment.GetEnvironmentVariable("SMS_User");
            password = Environment.GetEnvironmentVariable("SMS_Pwd");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Environment variables not set properly!");
                return false;
            }
#endif

                // Check if credentials match
                return credentials[0] == username && credentials[1] == password;
            }
            catch
            {
                return false; // Handle malformed authorization header
            }
        }
    }
}
