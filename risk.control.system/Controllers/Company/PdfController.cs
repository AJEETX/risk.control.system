using Microsoft.AspNetCore.Mvc;
using risk.control.system.Services;
using risk.control.system.Helpers;
namespace risk.control.system.Controllers.Company
{
    public class PdfController : Controller
    {
        private readonly IInvestigationService investigationService;

        public PdfController(IInvestigationService investigationService)
        {
            this.investigationService = investigationService;
        }
        public async Task<IActionResult> ApprovedDetail(long id)
        {
            try
            {
                var model = await investigationService.GetPdfReport( id);

                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
