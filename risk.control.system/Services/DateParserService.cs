using System.Globalization;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IDateParserService
    {
        DateTime ParseDate(string value, List<UploadError> errs, List<string> sums, string dateType);

        (DateTime IssueDate, DateTime IncidentDate) ValidateDates(UploadCase uc, List<UploadError> errs, List<string> sums);
    }

    internal class DateParserService : IDateParserService
    {
        private static string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy" };

        public DateTime ParseDate(string value, List<UploadError> errs, List<string> sums, string dateType)
        {
            bool isValid = DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dob
            );
            if (!isValid || dob > DateTime.Now || dob < DateTime.Now.AddYears(-120))
            {
                errs.Add(new UploadError { UploadData = $"[{dateType} Date of Birth: {value}]", Error = "Invalid/Null" });
                sums.Add($"[{dateType} Date of Birth={value} is invalid]");
                return DateTime.MinValue;
            }
            return dob;
        }

        public (DateTime IssueDate, DateTime IncidentDate) ValidateDates(UploadCase uc, List<UploadError> errs, List<string> sums)
        {
            bool isIssueValid = DateTime.TryParseExact(uc.IssueDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var issueDate);
            bool isIncidentValid = DateTime.TryParseExact(uc.IncidentDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var incidentDate);

            if (!isIssueValid || issueDate > DateTime.Today)
            {
                AddError("Issue Date", uc.IssueDate, errs, sums);
                issueDate = DateTime.Now;
            }

            if (!isIncidentValid || incidentDate > DateTime.Today)
            {
                AddError("Incident Date", uc.IncidentDate, errs, sums);
                incidentDate = DateTime.Now;
            }

            if (isIssueValid && isIncidentValid && issueDate > incidentDate)
            {
                errs.Add(new UploadError { UploadData = "Date Comparison", Error = "Issue date must be before incident date" });
                sums.Add("[Chronology Error: Issue date is after Incident date]");
            }

            return (issueDate, incidentDate);
        }

        private static void AddError(string field, string value, List<UploadError> errs, List<string> sums)
        {
            errs.Add(new UploadError { UploadData = $"[{field}: {value}]", Error = "Invalid/Null" });
            sums.Add($"[{field}={value} is invalid]");
        }
    }
}