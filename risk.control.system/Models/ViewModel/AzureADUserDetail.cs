using Newtonsoft.Json;

namespace risk.control.system.Models.ViewModel
{
    public class AzureADUserDetail
    {
        [JsonProperty("@odata.context")]
        public string OdataContext { get; set; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public List<object> BusinessPhones { get; set; }
        public string GivenName { get; set; }
        public object JobTitle { get; set; }
        public object Mail { get; set; }

        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int PostalCode { get; set; }
        public string Country { get; set; }
        public string MobilePhone { get; set; }
        public object OfficeLocation { get; set; }
        public object PreferredLanguage { get; set; }
        public string Surname { get; set; }
        public string UserPrincipalName { get; set; }
    }
}
