namespace risk.control.system.Models
{
    public class ClientCompanyApplicationUser : ApplicationUser
    {
        public string? ClientCompanyId { get; set; } = default!;
        public ClientCompany? ClientCompany { get; set; } = default!;
        public string? Comments { get; set; } = default!;
    }
}
