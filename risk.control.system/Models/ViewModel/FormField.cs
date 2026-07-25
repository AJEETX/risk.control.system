namespace risk.control.system.Models.ViewModel
{
    public enum FormType
    {
        Claim,
        Underwriting
    }


    public class FormField
    {
        public int Id { get; set; }
        public long CompanyId { get; set; }
        public FormType FormType { get; set; } // Claim or Underwriting
        public string Section { get; set; } = default!;   // "Policy", "Nominee", or "ClaimDetail"
        public string Label { get; set; } = default!;
        public string FieldType { get; set; } = default!;// text, number, date, file, dropdown
        public string? DropdownOptions { get; set; }
        public bool IsRequired { get; set; }
    }
    // Models/SubmittedForm.cs
    public class SubmittedForm
    {
        public int Id { get; set; }
        public DateTime SubmittedAt { get; set; }
        public long CompanyId { get; set; }
        public FormType FormType { get; set; }
        public List<SubmittedValue> Values { get; set; } = new List<SubmittedValue>();
    }

    // Models/SubmittedValue.cs
    public class SubmittedValue
    {
        public int Id { get; set; }
        public int SubmittedFormId { get; set; }
        public int FormFieldId { get; set; }
        public FormField FormField { get; set; } = default!;
        public string Value { get; set; } = default!;// Stores text, dates, or file paths as string
    }

}
