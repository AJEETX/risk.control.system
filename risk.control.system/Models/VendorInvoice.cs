using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class VendorInvoice : BaseEntity
    {
        public VendorInvoice()
        {
            InvoiceNumber = DateTime.UtcNow.Date.Year.ToString() +
                DateTime.UtcNow.Date.Month.ToString() +
                DateTime.UtcNow.Date.Day.ToString() + Guid.NewGuid().ToString().Substring(0, 4).ToUpper() + "INV";
            DueDate = DateTime.UtcNow.Date.AddMonths(1);
            SubTotal = 0;
            TaxAmount = 0;
            GrandTotal = 0;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VendorInvoiceId { get; set; }

        [Display(Name = "Invoice Number")]
        [Required]
        public string InvoiceNumber { get; set; }

        [Display(Name = "Invoice Date")]
        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Due Date")]
        [Required]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        [Display(Name = "Agency")]
        public long? VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        [Display(Name = "Insurer name")]
        public long? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Note To Recipient")]
        public string NoteToRecipient { get; set; }

        [Display(Name = "Sub Total")]
        public decimal SubTotal { get; set; }

        [Display(Name = "Tax Amount")]
        public decimal TaxAmount { get; set; }

        [Display(Name = "Grand Total")]
        public decimal GrandTotal { get; set; }

        public long? InvestigationReportId { get; set; }
        public virtual InvestigationReport? InvestigationReport { get; set; }
        public virtual InvestigationServiceType? InvestigationServiceType { get; set; }
        public long? CaseId { get; set; }

        public string? Currency { get; set; }
    }
}