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

        public AccountService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public bool ForgotPassword(string useremail, long mobile)
        {
            //CHECK AND VALIDATE EMAIL PASSWORD
            var user = context.ApplicationUser.FirstOrDefault(u => u.Email == useremail && u.PhoneNumber == mobile.ToString());
            if (user != null)
            {
                var passwordString = $"Your password is: {Applicationsettings.Password}";


                string message = $"Dear {useremail}";
                message += $"                                          ";
                message += $"Uid reset for mobile: {user.PhoneNumber}";
                message += $"                                          ";
                message += $"Thanks";
                message += $"                                          ";
                message += $"https://icheckify.co.in";
                var response = SmsService.SendSingleMessage(user.PhoneNumber, message, user != null);
            }
            //SEND SMS

            return user != null;
        }
    }
}