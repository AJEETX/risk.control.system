namespace risk.control.system.Models
{
    public class ClientApplicationUser : ApplicationUser
    {
        public string? ClientCompanyId { get; set; }
        public ClientCompany? ClientCompany { get; set; }
        public string? Comments { get; set; }
    }
}
