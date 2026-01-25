namespace risk.control.system.Models.ViewModel
{
    public class LicenseStatus
    {
        public bool CanCreate { get; set; }
        public bool HasClaimsPending { get; set; }
        public int AvailableCount { get; set; }
        public int MaxAllowed { get; set; }

        /// <summary>
        /// Helper to return a 'Success' state for non-trial or paid users
        /// </summary>
        public static LicenseStatus Unlimited() => new LicenseStatus
        {
            CanCreate = true,
            HasClaimsPending = true, // Or based on actual count if needed
            AvailableCount = int.MaxValue,
            MaxAllowed = int.MaxValue
        };
    }
}
