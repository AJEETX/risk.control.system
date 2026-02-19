namespace risk.control.system.Models.ViewModel
{
    public class ClaimCreationState
    {
        public bool UserCanCreate { get; set; }
        public bool HasClaims { get; set; }
        public string FileSampleIdentifier { get; set; }
        public int AvailableCount { get; set; }
        public int MaxAllowed { get; set; }
        public bool IsTrial { get; set; }
    }
}