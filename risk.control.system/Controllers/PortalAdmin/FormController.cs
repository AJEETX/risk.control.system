using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME}")]
    [Breadcrumb("Cases ")]
    public class FormController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly INotyfService _notifyService;
        private readonly ITimelineService _timelineService;

        public FormController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, ITimelineService timelineService, INotyfService notifyService)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _timelineService = timelineService;
            _notifyService = notifyService;
        }
        public IActionResult Index()
        {
            return View();
        }
        [Breadcrumb("Create Form")]
        [HttpGet]
        public IActionResult CreateForm(long? companyId, InsuranceType? type)
        {
            var activeCompanies = _context.ClientCompany
                .Where(c => !c.Deleted && c.Status == CompanyStatus.ACTIVE)
                .Select(c => new CompanySelectItem
                {
                    Id = c.ClientCompanyId,
                    Name = c.Name
                })
                .ToList();

            var model = new DynamicFormDesignerViewModel
            {
                SelectedCompanyId = companyId, // Will be null on first load
                TargetFormType = type,          // Will be null on first load
                Companies = activeCompanies
            };
            if (!companyId.HasValue || !type.HasValue)
            {
                return View(model);
            }
            // If both filters are selected, fetch the fields; otherwise return an empty list
            if (companyId.HasValue && type.HasValue)
            {
                model.Fields = _context.FormFields
                    .Where(f => f.InsuranceType == type.Value && f.CompanyId == companyId.Value)
                    .ToList();

            }
            if (model.Fields.Count == 0)
            {
                model.Fields.Add(new FormField
                {
                    Label = "Policy #",
                    FieldType = "PolicyNumber", // or "PolicyNumber" depending on your model strings
                    Section = "Policy",
                    IsRequired = true,
                    CompanyId = companyId.HasValue ? companyId.Value : 0,
                    InsuranceType = type.HasValue ? type.Value : InsuranceType.CLAIM
                });
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult CreateForm(long? selectedCompanyId, InsuranceType targetFormType, List<FormField> fields)
        {
            // 1. Guard check: Reject submit if companyId is missing or invalid
            if (!selectedCompanyId.HasValue || selectedCompanyId.Value <= 0)
            {
                _notifyService.Warning($"Please select a valid Client Company before saving!");
                return RedirectToAction(nameof(CreateForm), new { companyId = selectedCompanyId, type = targetFormType });
            }

            long companyId = selectedCompanyId.Value;

            // Filter out empty rows without labels
            fields = fields?.Where(f => !string.IsNullOrWhiteSpace(f.Label)).ToList() ?? new List<FormField>();

            // 2. Fetch and delete existing fields for this specific CompanyId and FormType
            var oldFields = _context.FormFields
                .Where(f => f.InsuranceType == targetFormType && f.CompanyId == companyId)
                .ToList();

            if (oldFields.Any())
            {
                _context.FormFields.RemoveRange(oldFields);
            }

            // 3. Bind CompanyId, FormType, and reset Id to 0 for clean insertion
            if (fields.Any())
            {
                foreach (var field in fields)
                {
                    field.Id = 0; // Ensures EF Core treats this as a new insert
                    field.CompanyId = companyId;
                    field.InsuranceType = targetFormType;
                }

                _context.FormFields.AddRange(fields);
            }

            _context.SaveChanges();

            _notifyService.Success($"{targetFormType} Form saved successfully!");

            return RedirectToAction(nameof(CreateForm), new { companyId = companyId, type = targetFormType });
        }
    }
}