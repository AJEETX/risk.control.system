using System;

using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class ClaimsInvestigationExtension
    {
        public static string GetTimePending(this ClaimsInvestigation a)
        {
            if (a.Created.Subtract(DateTime.UtcNow).Days >= 1)
                return string.Join("", "<span class='badge badge-light'>" + a.Created.Subtract(DateTime.UtcNow).Days + " day</span>");

            if (a.Created.Subtract(DateTime.UtcNow).Hours < 24 && a.Created.Subtract(DateTime.UtcNow).Hours > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Hours + " hr</span>");
            }
            if (a.Created.Subtract(DateTime.UtcNow).Hours == 0 && a.Created.Subtract(DateTime.UtcNow).Minutes > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Minutes + " min</span>");
            }
            if (a.Created.Subtract(DateTime.UtcNow).Minutes == 0 && a.Created.Subtract(DateTime.UtcNow).Seconds > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Seconds + " sec</span>");
            }
            return string.Join("", "<span class='badge badge-light'>...s</span>");
        }
    }
}