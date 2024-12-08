using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class GlobalSettings : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long GlobalSettingsId { get; set; }

        public bool EnableMailbox { get; set; } = true;

        public bool SendSMS { get; set; } = false;
        public bool CanChangePassword { get; set; } = false;
        public bool ShowTimer { get; set; } = false;
        public bool ShowDetailFooter { get; set; } = false;
        public bool EnableClaim { get; set; } = false;
        public bool EnableUnderwriting { get; set; } = false;

        public string FtpUri { get; set; } = "ftp://ftp.drivehq.com/holosync/";
        public string FtpUser { get; set; } = "its.aby@email.com";
        public string FtpData { get; set; } = "C0##ect10n";

        public string AddressUri { get; set; } = "https://api.geoapify.com/v1/geocode/reverse";
        public string AddressUriData { get; set; } = "f2a54c0ec9ba4dfdbd450116509c6313";

        public string WeatherUri { get; set; } = "https://api.open-meteo.com/v1/forecast";

    }
}
