namespace risk.control.system.Models.ViewModel
{
    public class DynamicFormDesignerViewModel
    {
        public long? SelectedCompanyId { get; set; }
        public FormType TargetFormType { get; set; } = FormType.Claim;
        public List<FormField> Fields { get; set; } = new List<FormField>();
        public List<CompanySelectItem> Companies { get; set; } = new List<CompanySelectItem>();
    }

    public class CompanySelectItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
    }
    public class FillFormViewModel
    {
        public FormType FormType { get; set; }
        public List<FormField> Fields { get; set; } = new List<FormField>();
        public List<CompanySelectItem> Companies { get; set; } = new List<CompanySelectItem>();
    }
    public class EditSubmissionViewModel
    {
        public int SubmissionId { get; set; }
        public FormType FormType { get; set; }
        public List<EditFieldViewModel> Fields { get; set; } = new List<EditFieldViewModel>();
    }

    public class EditFieldViewModel
    {
        public FormField Field { get; set; } = default!;
        public string CurrentValue { get; set; } = default!;
    }
}
