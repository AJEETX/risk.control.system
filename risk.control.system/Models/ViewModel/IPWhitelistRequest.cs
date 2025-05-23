namespace risk.control.system.Models.ViewModel
{
    public class IPWhitelistRequest
    {
        public string Url { get; set; } = "https://icheckify-edelweiss.azurewebsites.net/";
        public string Domain { get; set; }
        public string IpAddress { get; set; }
    }
}
