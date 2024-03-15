using risk.control.system.AppConstant;
using risk.control.system.Data;

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
                SMS.API.SendSingleMessage(user.PhoneNumber, passwordString);
                return true;
            }
            //SEND SMS

            return false;
        }
    }
}