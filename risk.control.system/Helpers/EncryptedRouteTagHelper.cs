using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using risk.control.system.Services.Common;

namespace risk.control.system.Helpers
{
    [HtmlTargetElement("a", Attributes = "asp-action")]
    [HtmlTargetElement("form", Attributes = "asp-action")]
    public class EncryptedRouteTagHelper : TagHelper
    {
        private readonly IEncryptionService _encryptionService;
        private readonly IUrlHelperFactory _urlHelperFactory;

        public EncryptedRouteTagHelper(IEncryptionService encryptionService, IUrlHelperFactory urlHelperFactory)
        {
            _encryptionService = encryptionService;
            _urlHelperFactory = urlHelperFactory;
        }

        [HtmlAttributeName("asp-controller")]
        public string? Controller { get; set; }

        [HtmlAttributeName("asp-action")]
        public string Action { get; set; } = default!;

        [HtmlAttributeName(DictionaryAttributePrefix = "asp-route-")]
        public Dictionary<string, object> RouteValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(Action) || ViewContext == null)
                return;

            var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);

            // Automatically encrypt any route parameter named "id" or ending with "Id" if it's numeric
            foreach (var key in RouteValues.Keys.ToList())
            {
                if ((key.Equals("id", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    && RouteValues[key] != null)
                {
                    var valStr = RouteValues[key].ToString();
                    if (long.TryParse(valStr, out long numericId))
                    {
                        RouteValues[key] = _encryptionService.ProtectId(numericId);
                    }
                }
            }

            // Build route dictionary
            var routeValuesDict = new RouteValueDictionary(RouteValues!);
            if (!string.IsNullOrEmpty(Controller))
            {
                routeValuesDict["controller"] = Controller;
            }
            routeValuesDict["action"] = Action;

            string generatedUrl = urlHelper.Action(Action, Controller, routeValuesDict) ?? string.Empty;

            if (!string.IsNullOrEmpty(generatedUrl))
            {
                string attributeName = output.TagName.Equals("a", StringComparison.OrdinalIgnoreCase) ? "href" : "action";
                output.Attributes.SetAttribute(attributeName, generatedUrl);

                // Handle form hidden input fallback for standard "id"
                if (output.TagName.Equals("form", StringComparison.OrdinalIgnoreCase) && RouteValues.TryGetValue("id", out var idVal) && idVal != null)
                {
                    output.PostContent.AppendHtml($"<input type=\"hidden\" name=\"id\" value=\"{idVal}\" />");
                }
            }

            base.Process(context, output);
        }
    }
}