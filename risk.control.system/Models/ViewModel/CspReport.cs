﻿using System.Text.Json.Serialization;

namespace risk.control.system.Models.ViewModel
{
    public class CspViolation
    {
        [JsonPropertyName("csp-report")]
        public CspReport? CspReport { get; set; }
    }

    public class CspReport
    {
        [JsonPropertyName("document-uri")]
        public string? DocumentUri { get; set; }

        [JsonPropertyName("referrer")]
        public string? Referrer { get; set; }

        [JsonPropertyName("violated-directive")]
        public string? ViolatedDirective { get; set; }

        [JsonPropertyName("effective-directive")]
        public string? EffectiveDirective { get; set; }

        [JsonPropertyName("original-policy")]
        public string? OriginalPolicy { get; set; }

        [JsonPropertyName("blocked-uri")]
        public string? BlockedUri { get; set; }

        [JsonPropertyName("status-code")]
        public int StatusCode { get; set; }
    }
}