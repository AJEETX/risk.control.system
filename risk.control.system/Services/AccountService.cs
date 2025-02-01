using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;

namespace risk.control.system.Services
{
    public interface IAccountService
    {
        Task<bool> ForgotPassword(string useremail, string mobile, string countryCode);
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext context;
        private readonly ISmsService smsService;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AccountService(ApplicationDbContext context,
            ISmsService SmsService,
             IHttpContextAccessor httpContextAccessor
            )
        {
            this.context = context;
            smsService = SmsService;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> ForgotPassword(string useremail, string mobile, string countryCode)
        {
            //CHECK AND VALIDATE EMAIL PASSWORD
            var resetPhone = countryCode.TrimStart('+') + mobile.Trim().ToString();
            var user = context.ApplicationUser.Include(a=>a.Country).FirstOrDefault(u => !u.Deleted && u.Email == useremail && string.Concat(u.Country.ISDCode.ToString(), u.PhoneNumber) == resetPhone);
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
                if(user != null)
                {
                    await smsService.DoSendSmsAsync(user.Country.ISDCode+ user.PhoneNumber, message);
                }
            }
            //SEND SMS

            return user != null;
        }
    }
}