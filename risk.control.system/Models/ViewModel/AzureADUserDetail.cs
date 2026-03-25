using Newtonsoft.Json;

namespace risk.control.system.Models.ViewModel
{
    public class AzureADUserDetail
    {
        [JsonProperty("@odata.context")]
        public string OdataContext { get; set; } = default!;
        public string Id { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public List<object> BusinessPhones { get; set; } = default!;
        public string GivenName { get; set; } = default!;
        public object JobTitle { get; set; } = default!;
        public object Mail { get; set; } = default!;

        public string StreetAddress { get; set; } = default!;
        public string City { get; set; } = default!;
        public string State { get; set; } = default!;
        public int PostalCode { get; set; }
        public string Country { get; set; } = default!;
        public string MobilePhone { get; set; } = default!;
        public object OfficeLocation { get; set; } = default!;
        public object PreferredLanguage { get; set; } = default!;
        public string Surname { get; set; } = default!;
        public string UserPrincipalName { get; set; } = default!;
    }
}
