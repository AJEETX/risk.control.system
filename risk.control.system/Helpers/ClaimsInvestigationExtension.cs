using System;

using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class ClaimsInvestigationExtension
    {
        public static string GetTimePending(this ClaimsInvestigation a)
        {
            if (DateTime.UtcNow.Subtract(a.Created).Days >= 1)
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Days + " day</span>");

            if (DateTime.UtcNow.Subtract(a.Created).Hours < 24 && DateTime.UtcNow.Subtract(a.Created).Hours > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Hours + " hr</span>");
            }
            if (DateTime.UtcNow.Subtract(a.Created).Hours == 0 && DateTime.UtcNow.Subtract(a.Created).Minutes > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Minutes + " min</span>");
            }
            if (DateTime.UtcNow.Subtract(a.Created).Minutes == 0 && DateTime.UtcNow.Subtract(a.Created).Seconds > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Seconds + " sec</span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
    }
}