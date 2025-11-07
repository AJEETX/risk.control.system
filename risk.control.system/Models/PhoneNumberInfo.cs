namespace risk.control.system.Models
{
    public class PhoneNumberInfo
    {
        public string? PhoneNumberEntered { get; set; }
        public string? DefaultCountryEntered { get; set; }
        public string? LanguageEntered { get; set; }
        public string? CountryCode { get; set; }
        public string? NationalNumber { get; set; }
        public string? Extension { get; set; }
        public string? CountryCodeSource { get; set; }
        public bool ItalianLeadingZero { get; set; }
        public string? RawInput { get; set; }
        public bool IsPossibleNumber { get; set; }
        public bool IsValidNumber { get; set; }
        public bool IsValidNumberForRegion { get; set; }
        public string? PhoneNumberRegion { get; set; }
        public string? NumberType { get; set; }
        public string? E164Format { get; set; }
        public string? OriginalFormat { get; set; }
        public string? NationalFormat { get; set; }
        public string? InternationalFormat { get; set; }
        public string? OutOfCountryFormatFromUS { get; set; }
        public string? OutOfCountryFormatFromCH { get; set; }
        public string? Location { get; set; }
        public string? TimeZone_s { get; set; }
        public string? Carrier { get; set; }
    }
}
