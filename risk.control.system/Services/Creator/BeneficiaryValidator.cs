using System.Globalization;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Creator;

namespace risk.control.system.Services
{
    public interface IBeneficiaryValidator
    {
        (DateTime Dob, Income Income) ValidateDetails(UploadCase uc, List<UploadError> errs, List<string> sums);

        void ValidateRequiredFields(UploadCase uc, List<UploadError> errs, List<string> sums);
    }

    internal class BeneficiaryValidator : IBeneficiaryValidator
    {
        private readonly IDateParserService dateParserService;

        public BeneficiaryValidator(IDateParserService dateParserService)
        {
            this.dateParserService = dateParserService;
        }

        public void ValidateRequiredFields(UploadCase uc, List<UploadError> errs, List<string> sums)
        {
            if (string.IsNullOrWhiteSpace(uc.BeneficiaryName) || uc.BeneficiaryName.Length < 2)
                AddError("BeneficiaryName", uc.BeneficiaryName, errs, sums);

            if (string.IsNullOrWhiteSpace(uc.BeneficiaryAddressLine) || uc.BeneficiaryAddressLine.Length < 3)
                AddError("AddressLine", uc.BeneficiaryAddressLine, errs, sums);
        }

        public (DateTime Dob, Income Income) ValidateDetails(UploadCase uc, List<UploadError> errs, List<string> sums)
        {
            var dob = dateParserService.ParseDate(uc.BeneficiaryDob, errs, sums, "Beneficiary");
            var income = ValidateIncome(uc, errs, sums);
            return (dob, income);
        }

        private Income ValidateIncome(UploadCase uc, List<UploadError> errs, List<string> sums)
        {
            if (Enum.TryParse<Income>(uc.BeneficiaryIncome, true, out var result)) return result;
            AddError("Income", uc.BeneficiaryIncome, errs, sums);
            return default;
        }

        private void AddError(string field, string value, List<UploadError> errs, List<string> sums)
        {
            errs.Add(new UploadError { UploadData = $"[{field}: {value}]", Error = "Invalid/Null" });
            sums.Add($"[{field}={value} is invalid]");
        }
    }
}