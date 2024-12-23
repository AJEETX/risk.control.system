﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Google.Rpc;

namespace risk.control.system.Models
{
    public class InvestigationCase : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string InvestigationId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;

        [Display(Name = "Line of Business")]
        public long? LineOfBusinessId { get; set; } = default!;

        [Display(Name = "Line of Business")]
        public LineOfBusiness? LineOfBusiness { get; set; } = default!;

        public long? InvestigationServiceTypeId { get; set; } = default!;
        public InvestigationServiceType? InvestigationServiceType { get; set; } = default!;

        [Display(Name = "Case status")]
        public string? InvestigationCaseStatusId { get; set; } = default!;

        [Display(Name = "Case status")]
        public InvestigationCaseStatus? InvestigationCaseStatus { get; set; } = default!;
        public override string ToString()
        {
            return $"Investigation Case Information: \n" +
                $"Name : {Name} \n" +
                $"Description : {Description} \n" +
                $"Line Of Business : {LineOfBusiness}  \n" +
                $"Investigation Service Type : {InvestigationServiceType}  \n" +
                $"Investigation Case Status : {InvestigationCaseStatus}";
        }
    }
}