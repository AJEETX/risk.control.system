using Microsoft.AspNetCore.Mvc.Rendering;
namespace risk.control.system.Models.ViewModel
{
    public class InvoiceDetail
    {
        public VendorInvoice VendorInvoice { get; set; } = default!;
        public string ContractNumber { get; set; } = default!;
    }

    public class VendorInvoiceIndexViewModel
    {
        // Holds the selected Vendor ID from the dropdown
        public long? SelectedVendorId { get; set; }

        // Strongly typed item collection for the HTML Select element
        public List<SelectListItem> VendorList { get; set; } = new();
    }
}
