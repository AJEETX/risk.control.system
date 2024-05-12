using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{

    public enum Income
    {
        [Display(Name = "UNKNOWN")]
        UNKNOWN,

        [Display(Name = "0.0 Lac")]
        NO_INCOME,

        [Display(Name = "0 - 2.5 Lac")]
        TAXFREE_SLOT,

        [Display(Name = "2.5 - 5 Lac")]
        BASIC_INCOME,

        [Display(Name = "5 - 8 Lac")]
        MEDIUUM_INCOME,

        [Display(Name = "8 - 15 Lac")]
        UPPER_INCOME,

        [Display(Name = "15 - 30 Lac")]
        HIGHER_INCOME,

        [Display(Name = "30 - 50 Lac")]
        TOP_HIGHER_INCOME,

        [Display(Name = "50 + Lac")]
        PREMIUM_INCOME,
    }

    public enum Occupation
    {
        UNKNOWN,

        [Display(Name = "UNEMPLOYED")]
        UNEMPLOYED,

        [Display(Name = "DOCTOR")]
        DOCTOR,

        [Display(Name = "ENGINEER")]
        ENGINEER,

        [Display(Name = "ACCOUNTANT")]
        ACCOUNTANT,

        [Display(Name = "SELF EMPLOYED")]
        SELF_EMPLOYED,

        [Display(Name = "OTHER")]
        OTHER
    }

    public enum Education
    {
        [Display(Name = "PRIMARY SCHOOL")]
        PRIMARY_SCHOOL,

        [Display(Name = "HIGH SCHOOL")]
        HIGH_SCHOOL,

        [Display(Name = "12th Class")]
        CLASS_12,

        [Display(Name = "GRADUATE")]
        GRADUATE,

        [Display(Name = "POST GRADUATE")]
        POST_GRADUATE,

        [Display(Name = "PROFESSIONAL")]
        PROFESSIONAL,
    }

    public enum UploadType
    {
        [Display(Name = "File Upload")]
        FILE,

        [Display(Name = "FTP Upload")]
        FTP,
    }

    public enum ClaimType
    {
        [Display(Name = "Death")]
        DEATH,

        [Display(Name = "Health")]
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
        HNI,

        [Display(Name = "Non-HNI")]
        NONHNI,
    }
    public enum DwellType
    {
        MORTGAGED,
        OWNED,
        RENTED,
        SHARED
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

    public enum OcrImageType
    {
        PAN,
        ADHAAR,
        DL
    }
    public enum ORIGIN
    {
        USER,
        FILE,
        FTP,
        API
    }
}
