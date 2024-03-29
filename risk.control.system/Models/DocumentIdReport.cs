﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DocumentIdReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DocumentIdReportId { get; set; }

        [Display(Name = "Insurer name")]
        public long? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Document Id Image")]
        public string? DocumentIdImagePath { get; set; }

        [Display(Name = "Document Id Image")]
        public byte[]? DocumentIdImage { get; set; }

        public bool? DocumentIdImageValid { get; set; } = false;
        public string? DocumentIdImageType { get; set; }

        [Display(Name = "Document Id Data")]
        public string? DocumentIdImageData { get; set; }

        [Display(Name = "Document Id Location")]
        public string? DocumentIdImageLocationUrl { get; set; }

        [Display(Name = "Document Id Location Address")]
        public string? DocumentIdImageLocationAddress { get; set; }

        public string? DocumentIdImageLongLat { get; set; }
        public DateTime? DocumentIdImageLongLatTime { get; set; }
        public DocumentIdReportType DocumentIdReportType { get; set; } = DocumentIdReportType.PAN;
    }

    public enum DocumentIdReportType
    {
        ADHAAR,
        PAN,
        DRIVING_LICENSE,
    }
}