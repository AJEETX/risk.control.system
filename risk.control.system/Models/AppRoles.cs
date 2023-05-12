using System.ComponentModel.DataAnnotations;

using risk.control.system.AppConstant;

namespace risk.control.system.Models
{
    public enum AppRoles
    {
        [Display(Name = Applicationsettings.PORTAL_ADMIN.DISPLAY_NAME)] PortalAdmin,
        [Display(Name = Applicationsettings.CLIENT_ADMIN.DISPLAY_NAME)] ClientAdmin,
        [Display(Name = Applicationsettings.VENDOR_ADMIN.DISPLAY_NAME)] VendorAdmin,
        [Display(Name = Applicationsettings.CLIENT_CREATOR.DISPLAY_NAME)] ClientCreator,
        [Display(Name = Applicationsettings.CLIENT_ASSIGNER.DISPLAY_NAME)] ClientAssigner,
        [Display(Name = Applicationsettings.CLIENT_ASSESSOR.DISPLAY_NAME)] ClientAssessor,
        [Display(Name = Applicationsettings.VENDOR_SUPERVISOR.DISPLAY_NAME)] VendorSupervisor,
        [Display(Name = Applicationsettings.VENDOR_AGENT.DISPLAY_NAME)] VendorAgent
    }
}
