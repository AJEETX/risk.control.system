using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IAccountService
    {
        Task<ForgotPasswordResult> ForgotPassword(string useremail, string mobile, string countryCode);
    }

    internal class AccountService : IAccountService
    {
        private readonly ApplicationDbContext context;
        private readonly ISmsService smsService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AccountService(ApplicationDbContext context,
            ISmsService SmsService,
            IWebHostEnvironment webHostEnvironment,
             IHttpContextAccessor httpContextAccessor
            )
        {
            this.context = context;
            smsService = SmsService;
            this.webHostEnvironment = webHostEnvironment;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<ForgotPasswordResult> ForgotPassword(string useremail, string mobile, string countryCode)
        {
            //CHECK AND VALIDATE EMAIL PASSWORD
            var resetPhone = countryCode.TrimStart('+') + mobile.Trim().ToString();
            var user = await context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => !u.Deleted && u.Email == useremail && string.Concat(u.Country.ISDCode.ToString(), u.PhoneNumber) == resetPhone);
            if (user == null)
            {
                return null;
            }
            var passwordString = $"Your password is: {user.Password}";
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

            string message = $"Dear {useremail}\n";
            message += $"{passwordString}\n";
            message += $"{BaseUrl}";
            await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, message);
            return new ForgotPasswordResult
            {
                CountryCode = user.Country.Code,
                ProfilePicture = System.IO.File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, user.ProfilePictureUrl)) ?? new byte[] { }
            };
        }
    }
}