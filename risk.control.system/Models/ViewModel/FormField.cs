namespace risk.control.system.Models.ViewModel
{
    public class FormField
    {
        public int Id { get; set; }
        public string Label { get; set; } = default!;     // e.g., "First Name", "Profile Image"
        public string FieldType { get; set; } = default!;  // e.g., "text", "date", "number", "file", "dropdown"
        public string? DropdownOptions { get; set; } = default!;// Comma-separated if FieldType is "dropdown" (e.g., "Red,Blue,Green")
        public bool IsRequired { get; set; }
    }

    // Models/SubmittedForm.cs
    public class SubmittedForm
    {
        public int Id { get; set; }
        public DateTime SubmittedAt { get; set; }
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
    public class EditSubmissionViewModel
    {
        public int SubmissionId { get; set; }
        public List<EditFieldViewModel> Fields { get; set; } = new List<EditFieldViewModel>();
    }

    public class EditFieldViewModel
    {
        public FormField Field { get; set; } = default!;
        public string CurrentValue { get; set; } = default!;
    }
}
