using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace risk.control.system.Helpers
{
    public static class ControllerExtensions
    {
        public static async Task<string> RenderViewAsync<TModel>(this Controller controller, string viewName, TModel model, bool partial = false)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;

            controller.ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !partial);

                if (!viewResult.Success)
                    throw new FileNotFoundException($"View '{viewName}' not found.");

                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }

        public static IActionResult RedirectToAction<TController>(
            this Controller controller,
            Expression<Action<TController>> action)
            where TController : Controller
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // Get action name
            if (!(action.Body is MethodCallExpression methodCall))
                throw new ArgumentException("Expression must be a method call", nameof(action));

            string actionName = methodCall.Method.Name;

            // Get controller name without "Controller" suffix
            string controllerName = typeof(TController).Name;
            if (controllerName.EndsWith("Controller"))
                controllerName = controllerName.Substring(0, controllerName.Length - 10);

            return controller.RedirectToAction(actionName, controllerName);
        }
    }
}