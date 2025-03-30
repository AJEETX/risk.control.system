using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

using risk.control.system.Models.ViewModel;

using System.Threading.Tasks;

namespace risk.control.system.Components
{
    public class MailboxViewComponent : ViewComponent
    {
        private readonly IFeatureManager _featureManager;

        public MailboxViewComponent(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var mailBoxEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_MAILBOX_FOR_PORTAL_ADMIN);
            return mailBoxEnabled ? View("AdminLTE/_CompanySidebarMailbox") : Content("");
        }
    }
}
