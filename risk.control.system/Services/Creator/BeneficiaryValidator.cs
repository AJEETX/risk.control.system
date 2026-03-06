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
            if (string.IsNullOrWhiteSpace(uc.BeneficiaryName.Trim()) || uc.BeneficiaryName.Length < 2)
                AddError("BeneficiaryName", uc.BeneficiaryName.Trim(), errs, sums);

            if (string.IsNullOrWhiteSpace(uc.BeneficiaryAddressLine.Trim()) || uc.BeneficiaryAddressLine.Length < 3)
                AddError("AddressLine", uc.BeneficiaryAddressLine.Trim(), errs, sums);
        }

        public (DateTime Dob, Income Income) ValidateDetails(UploadCase uc, List<UploadError> errs, List<string> sums)
        {
            var dob = dateParserService.ParseDate(uc.BeneficiaryDob.Trim(), errs, sums, "Beneficiary");
            var income = ValidateIncome(uc, errs, sums);
            return (dob, income);
        }

        private Income ValidateIncome(UploadCase uc, List<UploadError> errs, List<string> sums)
        {
            if (Enum.TryParse<Income>(uc.BeneficiaryIncome.Trim(), true, out var result)) return result;
            AddError("Income", uc.BeneficiaryIncome.Trim(), errs, sums);
            return default;
        }

        private void AddError(string field, string value, List<UploadError> errs, List<string> sums)
        {
            errs.Add(new UploadError { UploadData = $"[{field}: {value}]", Error = "Invalid/Null" });
            sums.Add($"[{field}={value} is invalid]");
        }
    }
}