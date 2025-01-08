using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using risk.control.system.AppConstant;

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

        public string SmsUri { get; set; } = "https://api.sms-gate.app/3rdparty/v1/message";
        public string SmsUser { get; set; } = "YXNGBE";
        public string SmsData { get; set; } = "rfi-gbbukll7-6";

        public string FtpUri { get; set; } = "ftp://ftp.drivehq.com/holosync/";
        public string FtpUser { get; set; } = "its.aby@email.com";
        public string FtpData { get; set; } = "C0##ect10n";

        public string AddressUri { get; set; } = "https://api.geoapify.com/v1/geocode/reverse";
        public string AddressUriData { get; set; } = "f2a54c0ec9ba4dfdbd450116509c6313";

        public string WeatherUri { get; set; } = "https://api.open-meteo.com/v1/forecast";

        public string PanIdfyUrl { get; set; } = "https://pan-card-verification-at-lowest-price.p.rapidapi.com/verification/marketing/pan";
        public string RapidAPIKey { get; set; } = "df0893831fmsh54225589d7b9ad1p15ac51jsnb4f768feed6f";
        public string RapidAPIHost { get; set; } = "pan-card-verification-at-lowest-price.p.rapidapi.com";

        public string? PassportApiUrl { get; set; } = "https://document-ocr1.p.rapidapi.com/idr";
        public string? PassportApiKey { get; set; } = "327fd8beb9msh8a441504790e80fp142ea8jsnf74b9208776a";
        public string? PassportApiHost { get; set; } = "document-ocr1.p.rapidapi.com";

        public string? MobileAppUrl { get; set; } = Applicationsettings.APP_URL;
        public string WhitelistIpAddress { get; set; } = "::1;202.7.251.53";

        public string AiApiUrl { get; set; } = "https://api-inference.huggingface.co/models/facebook/bart-large-cnn"; // HUGGING FACE
        public string AiApiData { get; set; } = Environment.GetEnvironmentVariable("HUGING_FACE") ?? "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"; // HUGGING FACE
    }
}
