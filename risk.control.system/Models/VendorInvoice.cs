using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class VendorInvoice : BaseEntity
    {
        public VendorInvoice()
        {
            this.InvoiceNumber = DateTime.UtcNow.Date.Year.ToString() +
                DateTime.UtcNow.Date.Month.ToString() +
                DateTime.UtcNow.Date.Day.ToString() + Guid.NewGuid().ToString().Substring(0, 4).ToUpper() + "INV";
            this.DueDate = DateTime.UtcNow.Date.AddMonths(1);
            this.SubTotal = 0;
            this.TaxAmount = 0;
            this.GrandTotal = 0;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string VendorInvoiceId { get; set; } = Guid.NewGuid().ToString();

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
        public string? VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        [Display(Name = "Insurer name")]
        public string? ClientCompanyId { get; set; }

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

        public string? ClaimReportId { get; set; }
        public virtual ClaimReport? Report { get; set; }
        public string? InvestigationServiceTypeId { get; set; }
        public virtual InvestigationServiceType? InvestigationServiceType { get; set; }
        public string? ClaimId { get; set; }
    }
}