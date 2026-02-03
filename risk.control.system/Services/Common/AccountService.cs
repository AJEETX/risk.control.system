using System.Net;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface IAccountService
    {
        Task<ForgotPasswordResult> ForgotPassword(string useremail, string mobile, string countryCode);
        Task<ServiceResult> ChangePasswordAsync(ChangePasswordViewModel model, ClaimsPrincipal userPrincipal, bool isAuthenticated, string portal_base_url);
        Task<ForgotPassword> CreateDefaultForgotPasswordModel(string email);
    }

    internal class AccountService : IAccountService
    {
        private readonly ApplicationDbContext context;
        private readonly ISmsService smsService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public AccountService(ApplicationDbContext context,
            ISmsService SmsService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
             IHttpContextAccessor httpContextAccessor
            )
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.context = context;
            this.smsService = SmsService;
            this.webHostEnvironment = webHostEnvironment;
            this.httpContextAccessor = httpContextAccessor;
        }
        public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordViewModel model, ClaimsPrincipal userPrincipal, bool isAuthenticated, string portal_base_url)
        {
            if (userPrincipal == null) 
            {
                throw new ArgumentNullException(nameof(userPrincipal));
            }
            var result = new ServiceResult();

            if (!isAuthenticated)
                return Error(result, string.Empty, "User not authenticated");

            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null)
                return Error(result, string.Empty, "User not found");

            var admin = await context.ApplicationUser.Include(u => u.Country).FirstOrDefaultAsync(u => u.IsSuperAdmin);

            if (admin == null)
                return Error(result, string.Empty, "Admin configuration missing");

            var changeResult = await userManager.ChangePasswordAsync(user, WebUtility.HtmlEncode(model.CurrentPassword), model.NewPassword);

            if (!changeResult.Succeeded)
            {
                await NotifyAdminAsync(admin, user, model.NewPassword, portal_base_url, failed: true);

                foreach (var error in changeResult.Errors)
                    result.Errors.TryAdd(string.Empty, error.Description);

                result.Message = "OOPS !!!..Error occurred. Contact Admin";
                return result;
            }

            user.IsPasswordChangeRequired = false;
            context.ApplicationUser.Update(user);
            await context.SaveChangesAsync();

            await signInManager.RefreshSignInAsync(user);

            await NotifyAdminAsync(admin, user, model.NewPassword, portal_base_url, false);
            await NotifyUserAsync(admin, user, model.NewPassword, portal_base_url);

            result.Success = true;
            return result;
        }

        private async Task NotifyAdminAsync(ApplicationUser admin, ApplicationUser user, string newPassword, string portal_base_url, bool failed = false)
        {
            var message =
                $"Dear {admin.Email}\n" +
                $"User {user.Email} {(failed ? "attempted" : "changed")} password.\n" +
                $"New password: {newPassword}\n" +
                $"{portal_base_url}";

            await smsService.DoSendSmsAsync(admin.Country.Code, "+" + admin.Country.ISDCode + admin.PhoneNumber, message);
        }

        private async Task NotifyUserAsync(ApplicationUser admin, ApplicationUser user, string newPassword, string portal_base_url)
        {
            var message =
                $"Dear {user.Email}\n" +
                $"Your changed password: {newPassword}\n" +
                $"{portal_base_url}";

            await smsService.DoSendSmsAsync(
                admin.Country.Code,
                "+" + admin.Country.ISDCode + user.PhoneNumber,
                message);
        }

        private static ServiceResult Error(ServiceResult result, string key, string message)
        {
            result.Errors.TryAdd(key, message);
            result.Message = message;
            return result;
        }

        public async Task<ForgotPasswordResult> ForgotPassword(string useremail, string mobile, string countryCode)
        {
            if(string.IsNullOrEmpty(useremail) || string.IsNullOrEmpty(mobile) || string.IsNullOrEmpty(countryCode))
            {
                return null!;
            }
            //CHECK AND VALIDATE EMAIL PASSWORD
            var resetPhone = countryCode.TrimStart('+') + mobile.Trim().ToString();
            var user = await context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => !u.Deleted && u.Email == useremail && string.Concat(u.Country.ISDCode.ToString(), u.PhoneNumber) == resetPhone);
            if (user == null)
            {
                return null!;
            }
            var passwordString = $"Your password is: {user.Password}";
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

            string message = $"Dear {useremail}\n";
            message += $"{passwordString}\n";
            message += $"{BaseUrl}";
            await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, message);
            var profileImageByte =await File.ReadAllBytesAsync(Path.Combine(webHostEnvironment.ContentRootPath, user.ProfilePictureUrl));

            return new ForgotPasswordResult
            {
                Id = user.Id,
                CountryCode = user.Country.Code,
                ProfileImage = $"data:image/*;base64,{Convert.ToBase64String(profileImageByte)}",
                ProfilePicture = profileImageByte ?? new byte[] { }
            };
        }
        public async Task<ForgotPassword> CreateDefaultForgotPasswordModel(string email)
        {
            var imagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-user.png");

            byte[] profilePicture = Array.Empty<byte>();

            if (File.Exists(imagePath))
            {
                profilePicture = await File.ReadAllBytesAsync(imagePath);
            }

            return new ForgotPassword
            {
                Message = "Incorrect details. Try Again",
                Reset = false,
                Flag = "/img/no-map.jpeg",
                ProfilePicture = profilePicture,
                Email = email
            };
        }
    }
}