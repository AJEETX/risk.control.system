using Microsoft.AspNetCore.Mvc;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;
using risk.control.system.Data;

namespace risk.control.system.Controllers
{
    public class InsuranceClaimsController : Controller
    {
        private readonly ApplicationDbContext context;

        public InsuranceClaimsController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [Breadcrumb(" Add New", FromAction = "Index", FromController = typeof(ClaimsInvestigationController))]
        public IActionResult Index()
        {
            var claim = new ClaimsInvestigation
            {
                PolicyDetail = new PolicyDetail
                {
                    LineOfBusinessId = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId
                }
            };

            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claim,
                Log = null,
                Location = new CaseLocation { }
            };

            return View(model);
        }
    }
}