using Microsoft.AspNetCore.Http;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;

namespace risk.control.system.Services
{
    public interface IAccountService
    {
        bool ForgotPassword(string useremail, long mobile);
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext context;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AccountService(ApplicationDbContext context,
             IHttpContextAccessor httpContextAccessor
            )
        {
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
        }

        public bool ForgotPassword(string useremail, long mobile)
        {
            //CHECK AND VALIDATE EMAIL PASSWORD
            var user = context.ApplicationUser.FirstOrDefault(u => u.Email == useremail && u.PhoneNumber == mobile.ToString());
            if (user != null)
            {


                var passwordString = $"Your password is: {user.Password}";
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                string message = $"Dear {useremail}";
                message += $"                                          ";
                message += $"{passwordString}";
                message += $"                                          ";
                message += $"Thanks";
                message += $"                                          ";
                message += $"{BaseUrl}"; 
                var response = SmsService.SendSingleMessage(user.PhoneNumber, message, user != null);
            }
            //SEND SMS

            return user != null;
        }
    }
}