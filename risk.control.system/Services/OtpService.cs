using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IOtpService
    {
        Task<string> SendOtp(OtpLoginModel model, string BaseUrl);
    }
    internal class OtpService : IOtpService
    {
        private readonly IMemoryCache cache;
        private readonly ISmsService smsService;
        private readonly ApplicationDbContext context;

        public OtpService(IMemoryCache cache, ISmsService smsService, ApplicationDbContext context)
        {
            this.cache = cache;
            this.smsService = smsService;
            this.context = context;
        }
        public async Task<string> SendOtp(OtpLoginModel model, string BaseUrl)
        {
            var otp = new Random().Next(1000, 9999).ToString();
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)).SetSize(1);

            cache.Set($"{model.CountryIsd.TrimStart('+')}{model.MobileNumber.TrimStart('0')}", otp, cacheOptions);

            var country = await context.Country.FirstOrDefaultAsync(c => c.ISDCode.ToString() == model.CountryIsd.TrimStart('+'));
            var message = $"Hi user {model.CountryIsd} {model.MobileNumber.TrimStart('0')}\n" +
                                     $"Your code is {otp}\n" +
                                     $"{BaseUrl}";
            var smsResponse = await smsService.SendSmsAsync(country.Code, model.CountryIsd + model.MobileNumber.TrimStart('0'), message);
            return smsResponse;
        }
    }
}
