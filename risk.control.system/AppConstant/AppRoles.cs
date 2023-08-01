using System.ComponentModel.DataAnnotations;

namespace risk.control.system.AppConstant
{
    public enum AppRoles
    {
        [Display(Name = Applicationsettings.PORTAL_ADMIN.DISPLAY_NAME)] PortalAdmin,
        [Display(Name = Applicationsettings.CLIENT_ADMIN.DISPLAY_NAME)] CompanyAdmin,
        [Display(Name = Applicationsettings.VENDOR_ADMIN.DISPLAY_NAME)] AgencyAdmin,
        [Display(Name = Applicationsettings.CLIENT_CREATOR.DISPLAY_NAME)] Creator,
        [Display(Name = Applicationsettings.CLIENT_ASSIGNER.DISPLAY_NAME)] Assigner,
        [Display(Name = Applicationsettings.CLIENT_ASSESSOR.DISPLAY_NAME)] Assessor,
        [Display(Name = Applicationsettings.VENDOR_SUPERVISOR.DISPLAY_NAME)] Supervisor,
        [Display(Name = Applicationsettings.VENDOR_AGENT.DISPLAY_NAME)] Agent
    }
}