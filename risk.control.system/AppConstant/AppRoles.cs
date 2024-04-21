﻿using System.ComponentModel.DataAnnotations;

namespace risk.control.system.AppConstant
{
    public enum AppRoles
    {
        [Display(Name = Applicationsettings.PORTAL_ADMIN.DISPLAY_NAME)] PORTAL_ADMIN,
        [Display(Name = Applicationsettings.ADMIN.DISPLAY_NAME)] ADMIN,
        [Display(Name = Applicationsettings.AGENCY_ADMIN.DISPLAY_NAME)] AGENCY_ADMIN,
        [Display(Name = Applicationsettings.CREATOR.DISPLAY_NAME)] CREATOR,
        [Display(Name = Applicationsettings.ASSESSOR.DISPLAY_NAME)] ASSESSOR,
        [Display(Name = Applicationsettings.MANAGER.DISPLAY_NAME)] MANAGER,
        [Display(Name = Applicationsettings.SUPERVISOR.DISPLAY_NAME)] SUPERVISOR,
        [Display(Name = Applicationsettings.AGENT.DISPLAY_NAME)] AGENT
    }
    public enum CompanyRole
    {
        [Display(Name = Applicationsettings.ADMIN.DISPLAY_NAME)] ADMIN,
        [Display(Name = Applicationsettings.CREATOR.DISPLAY_NAME)] CREATOR,
        [Display(Name = Applicationsettings.ASSESSOR.DISPLAY_NAME)] ASSESSOR,
        [Display(Name = Applicationsettings.MANAGER.DISPLAY_NAME)] MANAGER,
    }
    public enum AgencyRole
    {
        [Display(Name = Applicationsettings.AGENCY_ADMIN.DISPLAY_NAME)] AGENCY_ADMIN,
        [Display(Name = Applicationsettings.SUPERVISOR.DISPLAY_NAME)] SUPERVISOR,
        [Display(Name = Applicationsettings.AGENT.DISPLAY_NAME)] AGENT
    }
}