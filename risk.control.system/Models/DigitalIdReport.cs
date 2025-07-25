﻿using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DigitalIdReport : IdReportBase
    {
        public string? MatchConfidence { get; set; } = string.Empty;
        public float Similarity { get; set; } = 0;
        public bool Has2Face { get; set; } = false;
        public DigitalIdReportType ReportType { get; set; }
        // Foreign key to LocationTemplate
        public long? LocationTemplateId { get; set; }  // This is the FK property
        public LocationTemplate? LocationTemplate { get; set; }  // Navigation property

    }

    public enum DigitalIdReportType
    {
        [Display(Name = "Agent Face")]
        AGENT_FACE,

        [Display(Name = "Single Face")]
        SINGLE_FACE,

        [Display(Name = "Customer Face")]
        CUSTOMER_FACE,

        [Display(Name = "Beneficiary Face")]
        BENEFICIARY_FACE,

        [Display(Name = "Dual Face")]
        DUAL_FACE,

        [Display(Name = "House Front")]
        HOUSE_FRONT,
    }

}