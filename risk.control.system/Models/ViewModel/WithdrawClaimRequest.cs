namespace risk.control.system.Models.ViewModel
{
    public class WithdrawClaimRequest
    {
        public int ClaimId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
