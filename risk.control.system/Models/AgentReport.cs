﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class AgentReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AgentReportId { get; set; }

        public string? AgentEmail { get; set; }
        public DateTime? AgentRemarksUpdated { get; set; }
        public string? AgentRemarks { get; set; }

        [Display(Name = "Agent Location Image")]
        public string? AgentLocationPictureUrl { get; set; }

        [Display(Name = "Agent Location Image")]
        public byte[]? AgentLocationPicture { get; set; }

        [Display(Name = "Agent Location Image")]
        [NotMapped]
        public IFormFile? AgentLocationImage { get; set; }

        [Display(Name = "Agent Location Image")]
        public string? AgentOcrUrl { get; set; }

        [Display(Name = "Agent Location Image")]
        public byte[]? AgentOcrPicture { get; set; }

        [Display(Name = "Agent Ocr Image")]
        [NotMapped]
        public IFormFile? AgentOcrImage { get; set; }

        [Display(Name = "Agent Ocr Data")]
        public string? AgentOcrData { get; set; }

        [Display(Name = "Agent Ocr Image")]
        public string? AgentQrUrl { get; set; }

        [Display(Name = "Agent Qr Image")]
        public byte[]? AgentQrPicture { get; set; }

        [Display(Name = "Agent Qr Image")]
        [NotMapped]
        public IFormFile? AgentQrImage { get; set; }

        [Display(Name = "Agent Qr Data")]
        public string? QrData { get; set; }

        public string? LongLat { get; set; }
        public override string ToString()
        {
            return $"Agent Report Information:\n" +
                $"- Agent Email: {AgentEmail}\n" +
                $"- Agent Remarks Updated time: {AgentRemarksUpdated}\n" +
                $"- Agent Remarks: {AgentRemarks}\n" +
                $"- Agent Address Location Picture Url: {AgentLocationPictureUrl}\n" +
                $"- Agent Document url: {AgentOcrUrl}\n" +
                $"- Agent Face Url: {AgentQrUrl}\n" +
                $"- Longitude and Latitude: {LongLat}";
        }
    }
}