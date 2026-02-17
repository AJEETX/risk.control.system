using risk.control.system.Controllers.Creator;
using risk.control.system.Helpers;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Services.Common
{
    public interface INavigationService
    {
        MvcBreadcrumbNode GetEditCasePath(long id);

        MvcBreadcrumbNode GetEmpanelledVendorsPath(long id, long vendorId, bool fromEditPage = false);

        MvcBreadcrumbNode GetVendorDetailPath(long caseId, long vendorId);

        MvcBreadcrumbNode GetInvestigationPath(long id, string title, string action, string controller);

        MvcBreadcrumbNode GetAgencyActionPath(long id, string controller, string listTitle, string actionTitle, string actionName);

        MvcBreadcrumbNode GetAgencyServiceManagerPath(long id, string controller, string listTitle);

        MvcBreadcrumbNode GetAgencyServiceActionPath(long id, string controller, string listTitle, string actionTitle, string actionName);

        MvcBreadcrumbNode GetAgencyUserManagerPath(long id, string agencyController, string listTitle);

        MvcBreadcrumbNode GetAgencyUserActionPath(long id, string agencyController, string listTitle, string actionTitle, string actionName);

        MvcBreadcrumbNode GetInvoiceBreadcrumb(long invoiceId, long caseId, string controller, string rootAction, string rootTitle, string listAction, string listTitle, string detailAction);

        MvcBreadcrumbNode GetAssessorEnquiryPath(long id, string controller);
    }

    internal class NavigationService : INavigationService
    {
        private MvcBreadcrumbNode GetCaseBasePath(long id)
        {
            var cases = new MvcBreadcrumbNode(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name, "Cases");

            return new MvcBreadcrumbNode(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name, "Add/Assign") { Parent = cases };
        }

        private MvcBreadcrumbNode GetCaseDetailsBase(long id)
        {
            return new MvcBreadcrumbNode(nameof(InvestigationController.Details), ControllerName<InvestigationController>.Name, "Details")
            {
                Parent = GetCaseBasePath(id),
                RouteValues = new { id }
            };
        }

        public MvcBreadcrumbNode GetInvestigationPath(long id, string title, string action, string controller)
        {
            if (action == nameof(InvestigationController.Details))
            {
                return GetCaseDetailsBase(id);
            }

            return new MvcBreadcrumbNode(action, controller, title)
            {
                Parent = GetCaseDetailsBase(id),
                RouteValues = new { id }
            };
        }

        public MvcBreadcrumbNode GetEditCasePath(long id)
        {
            return new MvcBreadcrumbNode(nameof(CaseCreateEditController.Edit),
                ControllerName<CaseCreateEditController>.Name, "Edit Case")
            {
                Parent = GetCaseDetailsBase(id),
                RouteValues = new { id }
            };
        }

        public MvcBreadcrumbNode GetEmpanelledVendorsPath(long id, long vendorId, bool fromEditPage = false)
        {
            return new MvcBreadcrumbNode(nameof(InvestigationController.EmpanelledVendors), ControllerName<InvestigationController>.Name, "Empanelled Agencies")
            {
                Parent = fromEditPage ? GetCaseDetailsBase(id) : GetCaseBasePath(id),
                RouteValues = new { id, vendorId }
            };
        }

        public MvcBreadcrumbNode GetVendorDetailPath(long caseId, long vendorId)
        {
            return new MvcBreadcrumbNode(nameof(InvestigationController.VendorDetail), ControllerName<InvestigationController>.Name, "Agency Detail")
            {
                Parent = GetEmpanelledVendorsPath(caseId, vendorId),
                RouteValues = new { id = vendorId, selectedcase = caseId }
            };
        }

        public MvcBreadcrumbNode GetAgencyActionPath(long id, string controller, string listTitle, string actionTitle, string actionName)
        {
            return new MvcBreadcrumbNode(actionName, controller, actionTitle)
            {
                Parent = GetAgencyProfileBase(id, controller, listTitle),
                RouteValues = new { id }
            };
        }

        public MvcBreadcrumbNode GetAgencyServiceManagerPath(long id, string controller, string listTitle)
        {
            return new MvcBreadcrumbNode("Service", $"{controller}Service", "Manage Service")
            {
                Parent = GetAgencyProfileBase(id, controller, listTitle),
                RouteValues = new { id }
            };
        }

        public MvcBreadcrumbNode GetAgencyServiceActionPath(long id, string controller, string listTitle, string actionTitle, string actionName)
        {
            // Reuse the 4-level "Manage Service" path as the parent
            var serviceManagerNode = GetAgencyServiceManagerPath(id, controller, listTitle);

            return new MvcBreadcrumbNode(actionName, $"{controller}Service", actionTitle)
            {
                Parent = serviceManagerNode,
                RouteValues = new { id }
            };
        }

        public MvcBreadcrumbNode GetAgencyUserManagerPath(long id, string agencyController, string listTitle)
        {
            var profileNode = GetAgencyProfileBase(id, agencyController, listTitle);

            return new MvcBreadcrumbNode("Users", $"{agencyController}User", "Manage Users")
            {
                Parent = profileNode,
                RouteValues = new { id }
            };
        }

        public MvcBreadcrumbNode GetAgencyUserActionPath(long id, string agencyController, string listTitle, string actionTitle, string actionName)
        {
            var userManagerNode = GetAgencyUserManagerPath(id, agencyController, listTitle);

            return new MvcBreadcrumbNode(actionName, $"{agencyController}User", actionTitle)
            {
                Parent = userManagerNode,
                RouteValues = new { id }
            };
        }

        private MvcBreadcrumbNode GetAgencyProfileBase(long id, string controller, string listTitle)
        {
            var root = new MvcBreadcrumbNode("Agencies", controller, "Manage Agency");

            var list = new MvcBreadcrumbNode("Agencies", controller, listTitle)
            {
                Parent = root
            };

            return new MvcBreadcrumbNode("Detail", controller, "Agency Profile")
            {
                Parent = list,
                RouteValues = new { id }
            };
        }

        public MvcBreadcrumbNode GetInvoiceBreadcrumb(long invoiceId, long caseId, string controller, string rootAction, string rootTitle, string listAction, string listTitle, string detailAction)
        {
            // 1. Root Level (e.g., Allocate/Cases or Assessor/Cases)
            var root = new MvcBreadcrumbNode(rootAction, controller, rootTitle);

            // 2. List Level (e.g., Completed or Approved)
            var list = new MvcBreadcrumbNode(listAction, controller, listTitle)
            {
                Parent = root
            };

            // 3. Details Level (e.g., CompletedDetail or ApprovedDetail)
            var details = new MvcBreadcrumbNode(detailAction, controller, "Details")
            {
                Parent = list,
                RouteValues = new { id = caseId }
            };

            // 4. Final Leaf: Invoice
            return new MvcBreadcrumbNode("ShowInvoice", controller, "Invoice")
            {
                Parent = details,
                RouteValues = new { id = invoiceId }
            };
        }

        public MvcBreadcrumbNode GetAssessorEnquiryPath(long id, string controller)
        {
            // 1. Root: Cases
            var root = new MvcBreadcrumbNode("Assessor", controller, "Cases");

            // 2. List: Assess(report)
            var list = new MvcBreadcrumbNode("Assessor", controller, "Assess(report)")
            {
                Parent = root
            };

            // 3. Details: GetInvestigateReport
            // Note: Uses 'selectedcase' as the route parameter per your requirements
            var details = new MvcBreadcrumbNode("GetInvestigateReport", controller, "Details")
            {
                Parent = list,
                RouteValues = new { selectedcase = id }
            };

            // 4. Leaf: Send Enquiry
            return new MvcBreadcrumbNode("SendEnquiry", controller, "Send Enquiry")
            {
                Parent = details,
                RouteValues = new { id = id }
            };
        }
    }
}