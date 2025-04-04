﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DigitalIdReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DigitalIdReportId { get; set; }

        [Display(Name = "Digital Id Image")]
        public string? DigitalIdImagePath { get; set; }

        [Display(Name = "Digital Id Image")]
        public byte[]? DigitalIdImage { get; set; }

        [Display(Name = "Digital Id Data")]
        public string? DigitalIdImageData { get; set; } = "No Location Info...";

        [Display(Name = "Digital Id Location")]
        public string? DigitalIdImageLocationUrl { get; set; }

        public string? Distance { get; set; }
        public float? DistanceInMetres { get; set; }
        public string? Duration { get; set; }
        public int? DurationInSeconds { get; set; }

        [Display(Name = "Digital Id Location Address")]
        public string? DigitalIdImageLocationAddress { get; set; } = "No Address data";

        public string? DigitalIdImageMatchConfidence { get; set; } = string.Empty;
        public bool MatchExecuted { get; set; } = false;
        public float Similarity { get; set; } = 0;

        public string? DigitalIdImageLongLat { get; set; }
        public DateTime? DigitalIdImageLongLatTime { get; set; }
        public DigitalIdReportType ReportType { get; set; } = DigitalIdReportType.SINGLE_FACE;

        public override string ToString()
        {
            return $"Digital Id Information: \n" +
                $"- Valid: {MatchExecuted}";
        }
    }

    public enum DigitalIdReportType
    {
        AGENT_FACE,
        SINGLE_FACE,
        DUAL_FACE,
        HOUSE_FRONT,
    }
}