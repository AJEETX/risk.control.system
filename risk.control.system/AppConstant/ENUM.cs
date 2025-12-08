using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public enum LicenseType
    {
        Trial,
        Standard
    }
    public enum Income
    {
        [Display(Name = "UNKNOWN")]
        [Description("Income status is unknown")]
        UNKNOWN = 0,

        [Display(Name = "0.0 Lac")]
        [Description("No income or negligible income")]
        NO_INCOME = 1,

        [Display(Name = "0 - 2.5 Lac")]
        [Description("Income falls within the tax-free slab")]
        TAXFREE_SLOT = 2,

        [Display(Name = "2.5 - 5 Lac")]
        [Description("Basic income level")]
        BASIC_INCOME = 3,

        [Display(Name = "5 - 8 Lac")]
        [Description("Medium income level")]
        MEDIUM_INCOME = 4,

        [Display(Name = "8 - 15 Lac")]
        [Description("Upper-middle income level")]
        UPPER_INCOME = 5,

        [Display(Name = "15 - 30 Lac")]
        [Description("Higher income level")]
        HIGHER_INCOMEv = 6,

        [Display(Name = "30 - 50 Lac")]
        [Description("Top higher-income bracket")]
        TOP_HIGHER_INCOME = 7,

        [Display(Name = "50 + Lac")]
        [Description("Premium income category")]
        PREMIUM_INCOME = 8,
    }

    public enum Occupation
    {
        [Display(Name = "UNKNOWN")]
        [Description("Occupation is not specified or unknown")]
        UNKNOWN,

        [Display(Name = "UNEMPLOYED")]
        [Description("Currently not employed")]
        UNEMPLOYED,

        [Display(Name = "DOCTOR")]
        [Description("Medical professional or healthcare provider")]
        DOCTOR,

        [Display(Name = "ENGINEER")]
        [Description("Professional in engineering or technical field")]
        ENGINEER,

        [Display(Name = "ACCOUNTANT")]
        [Description("Professional in accounting or finance")]
        ACCOUNTANT,

        [Display(Name = "SELF EMPLOYED")]
        [Description("Runs own business or works independently")]
        SELF_EMPLOYED,

        [Display(Name = "OTHER")]
        [Description("Any other occupation not listed")]
        OTHER
    }

    public enum Education
    {
        [Display(Name = "UNKNOWN")]
        [Description("Education is not specified or unknown")]
        UNKNOWN,

        [Display(Name = "PRIMARY SCHOOL")]
        [Description("Completed primary school education")]
        PRIMARY_SCHOOL,

        [Display(Name = "HIGH SCHOOL")]
        [Description("Completed high school education")]
        HIGH_SCHOOL,

        [Display(Name = "12th Class")]
        [Description("Completed 12th-grade or equivalent education")]
        CLASS_12,

        [Display(Name = "GRADUATE")]
        [Description("Holds an undergraduate or bachelor's degree")]
        GRADUATE,

        [Display(Name = "POST GRADUATE")]
        [Description("Holds a postgraduate or master's degree")]
        POST_GRADUATE,

        [Display(Name = "PROFESSIONAL")]
        [Description("Holds a professional degree or qualification")]
        PROFESSIONAL,
    }

    public enum UploadType
    {
        [Display(Name = "File Upload")]
        [Description("Data upload type through file")]
        FILE
        //    ,

        //[Display(Name = "FTP Upload")]
        //[Description("Data upload type through ftp")]
        //FTP,
    }

    public enum ClaimType
    {
        [Display(Name = "Death")]
        [Description("Claim type death")]
        DEATH,

        [Display(Name = "Health")]
        [Description("Claim type health")]
        HEALTH
    }

    public enum InsuranceType
    {
        [Display(Name = "CLAIM")]
        [Description("Insurance Claim")]
        CLAIM,

        [Display(Name = "UNDERWRITING")]
        [Description("Insurance Pre-Issuance")]
        UNDERWRITING
    }

    public enum Gender
    {
        [Display(Name = "UNKNOWN")]
        UNKNOWN,

        [Display(Name = "Male")]
        MALE,

        [Display(Name = "Female")]
        FEMALE,

        [Display(Name = "Other")]
        OTHER
    }

    public enum CustomerType
    {
        [Display(Name = "UNKNOWN")]
        UNKNOWN,
        [Display(Name = "HNI")]
        [Description("High Net Worth Individual")]
        HNI,

        [Display(Name = "Non-HNI")]
        [Description("Non-High Net Worth Individual")]
        NONHNI,
    }
    public enum SupervisorRemarkType
    {
        OK,
        REVIEW
    }

    public enum AssessorRemarkType
    {
        OK,
        REVIEW,
        REJECT
    }

    public enum CREATEDBY
    {
        [Description("Data was entered through auto-allocation")]
        AUTO,
        [Description("Data was entered through manual-allocation")]
        MANUAL
    }
    public enum ORIGIN
    {
        [Display(Name = "User")]
        [Description("Data was entered manually by a user")]
        USER,

        [Display(Name = "File")]
        [Description("Data was imported from a file")]
        FILE,

        [Display(Name = "FTP")]
        [Description("Data was uploaded via FTP")]
        FTP,

        [Display(Name = "API")]
        [Description("Data was submitted through an API")]
        API
    }
}
