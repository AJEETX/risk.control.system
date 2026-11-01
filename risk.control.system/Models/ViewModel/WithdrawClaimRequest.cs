namespace risk.control.system.Models.ViewModel
{
    public class WithdrawCaseRequest
    {
        public int ClaimId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
