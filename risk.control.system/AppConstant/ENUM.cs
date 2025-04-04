using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{

    public enum Income
    {
        [Display(Name = "UNKNOWN")]
        [Description("Income status is unknown")]
        UNKNOWN,

        [Display(Name = "0.0 Lac")]
        [Description("No income or negligible income")]
        NO_INCOME,

        [Display(Name = "0 - 2.5 Lac")]
        [Description("Income falls within the tax-free slab")]
        TAXFREE_SLOT,

        [Display(Name = "2.5 - 5 Lac")]
        [Description("Basic income level")]
        BASIC_INCOME,

        [Display(Name = "5 - 8 Lac")]
        [Description("Medium income level")]
        MEDIUM_INCOME,

        [Display(Name = "8 - 15 Lac")]
        [Description("Upper-middle income level")]
        UPPER_INCOME,

        [Display(Name = "15 - 30 Lac")]
        [Description("Higher income level")]
        HIGHER_INCOME,

        [Display(Name = "30 - 50 Lac")]
        [Description("Top higher-income bracket")]
        TOP_HIGHER_INCOME,

        [Display(Name = "50 + Lac")]
        [Description("Premium income category")]
        PREMIUM_INCOME,
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
        [Display(Name = "LIFE")]
        [Description("Insurance type life")]
        LIFE,

        [Display(Name = "Health")]
        [Description("Insurance type health")]
        HEALTH
    }

    public enum Gender
    {
        [Display(Name = "Male")]
        MALE,

        [Display(Name = "Female")]
        FEMALE,

        [Display(Name = "Other")]
        OTHER
    }

    public enum CustomerType
    {
        [Display(Name = "HNI")]
        [Description("High Net Worth Individual")]
        HNI,

        [Display(Name = "Non-HNI")]
        [Description("Non-High Net Worth Individual")]
        NONHNI,
    }
    public enum DwellType
    {
        [Display(Name = "Mortgaged")]
        [Description("Property is owned with an active mortgage")]
        MORTGAGED,

        [Display(Name = "Owned")]
        [Description("Property is fully owned without any mortgage")]
        OWNED,

        [Display(Name = "Rented")]
        [Description("Property is rented")]
        RENTED,

        [Display(Name = "Shared")]
        [Description("Property is shared with others, such as roommates or family")]
        SHARED
    }

    public enum YESNO
    {
        [Display(Name = "YES")]
        [Description("Affirmative response")]
        YES,

        [Display(Name = "NO")]
        [Description("Negative response")]
        NO
    }
    public enum DURATION
    {
        [Display(Name = "No Time")]
        [Description("No specific duration assigned")]
        ZERO,

        [Display(Name = "Less than 6 Months")]
        [Description("The duration is less than 6 months")]
        LESS_THAN_6_MONTHS,

        [Display(Name = "More than 6 Months")]
        [Description("The duration exceeds 6 months")]
        MORE_THAN_6_MONTHS
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
