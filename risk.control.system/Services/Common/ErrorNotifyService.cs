using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace risk.control.system.Services.Common
{
    public interface IErrorNotifyService
    {
        void ShowErrorNotification(ModelStateDictionary ModelState, string message = "An error occurred. Please try again.");
    }

    internal class ErrorNotifyService : IErrorNotifyService
    {
        private readonly INotyfService _notifyService;

        public ErrorNotifyService(INotyfService notifyService)
        {
            _notifyService = notifyService;
        }

        public void ShowErrorNotification(ModelStateDictionary ModelState, string message = "An error occurred. Please try again.")
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct();
            _notifyService.Error($"<b>Please fix:</b><br/>{string.Join("<br/>", errors)}");
        }
    }
}