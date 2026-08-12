using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Seeds
{
    public static class ClaimFormSeed
    {
        public static async Task Init(ApplicationDbContext context, long companyId)
        {
            var claimFormData = new List<FormField>
            {
                new FormField { Id = 1, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "Policy #", FieldType = "policyNumber", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 2, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "Policy Document", FieldType = "file", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 3, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "Date of Issue", FieldType = "date", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 4, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "Policy Plan", FieldType = "text", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 5, CompanyId =companyId , InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "Annual Premium", FieldType = "number", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 6, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "Premium Paying Term (years)", FieldType = "number", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 7, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "Assured Amount", FieldType = "number", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 8, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "LA name", FieldType = "text", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 9, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "LA DOB", FieldType = "date", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 10, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "LA Gender", FieldType = "dropdown", DropdownOptions = "MALE, FEMALE, OTHER", LocationData = null, IsRequired = true },
                new FormField { Id = 11, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Policy", Label = "LA address", FieldType = "address", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 12, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Nominee", Label = "Name", FieldType = "text", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 13, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Nominee", Label = "Photo", FieldType = "file", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 14, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Nominee", Label = "DOB", FieldType = "date", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 15, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Nominee", Label = "Gender", FieldType = "dropdown", DropdownOptions = "MALE, FEMALE, OTHER", LocationData = null, IsRequired = true },
                new FormField { Id = 16, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Nominee", Label = "Address", FieldType = "address", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 17, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "Nominee", Label = "Relationship", FieldType = "dropdown", DropdownOptions = "PARENT, SIBLING, UNCLE, AUNT, COUSIN", LocationData = null, IsRequired = true },
                new FormField { Id = 18, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "ClaimDetail", Label = "Claim form", FieldType = "file", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 19, CompanyId =companyId, InsuranceType = InsuranceType.CLAIM, Section = "ClaimDetail", Label = "Date of Incident", FieldType = "date", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 20, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "ClaimDetail", Label = "Cause of Incident", FieldType = "text", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 21, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "ClaimDetail", Label = "Reason to verify", FieldType = "text", DropdownOptions = null, LocationData = null, IsRequired = true },
                new FormField { Id = 22, CompanyId = companyId, InsuranceType = InsuranceType.CLAIM, Section = "ClaimDetail", Label = "Comment", FieldType = "text", DropdownOptions = null, LocationData = null, IsRequired = true }
            };
            await context.FormFields.AddRangeAsync(claimFormData);
            await context.SaveChangesAsync(null, false);
        }
    }
}
