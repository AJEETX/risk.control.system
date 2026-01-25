using System.Globalization;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICustomerValidator
    {
        void ValidateRequiredFields(UploadCase uc, List<UploadError> errs, List<string> sums);
        (DateTime Dob, Gender Gen, Education Edu, Occupation Occ, Income Inc) ValidateDetails(UploadCase uc, List<UploadError> errs, List<string> sums);
    }

    internal class CustomerValidator : ICustomerValidator
    {
        public void ValidateRequiredFields(UploadCase uc, List<UploadError> errs, List<string> sums)
        {
            if (string.IsNullOrWhiteSpace(uc.CaseId)) AddError("CaseId", "Empty", errs, sums);
            if (string.IsNullOrWhiteSpace(uc.CustomerName) || uc.CustomerName.Length < 2) AddError("CustomerName", uc.CustomerName, errs, sums);
            if (string.IsNullOrWhiteSpace(uc.CustomerAddressLine)) AddError("AddressLine", "Empty", errs, sums);
        }

        public (DateTime Dob, Gender Gen, Education Edu, Occupation Occ, Income Inc) ValidateDetails(UploadCase uc, List<UploadError> errs, List<string> sums)
        {
            var dob = ParseDate(uc.CustomerDob, errs, sums);
            var gender = ParseEnum<Gender>(uc.Gender, "Gender", errs, sums);
            var edu = ParseEnum<Education>(uc.Education, "Education", errs, sums);
            var occ = ParseEnum<Occupation>(uc.Occupation, "Occupation", errs, sums);
            var inc = ParseEnum<Income>(uc.Income, "Income", errs, sums);

            return (dob, gender, edu, occ, inc);
        }

        private T ParseEnum<T>(string value, string field, List<UploadError> errs, List<string> sums) where T : struct
        {
            if (Enum.TryParse<T>(value, true, out var result)) return result;
            AddError(field, value, errs, sums);
            return default;
        }

        private DateTime ParseDate(string value, List<UploadError> errs, List<string> sums)
        {
            bool isValid = DateTime.TryParseExact(value, CONSTANTS.ValidDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob);
            if (!isValid || dob > DateTime.Now || dob < DateTime.Now.AddYears(-120))
            {
                AddError("Date of Birth", value, errs, sums);
                return DateTime.MinValue;
            }
            return dob;
        }

        private void AddError(string field, string value, List<UploadError> errs, List<string> sums)
        {
            errs.Add(new UploadError { UploadData = $"[{field}: {value}]", Error = "Invalid/Null" });
            sums.Add($"[{field}={value} invalid]");
        }
    }
}
