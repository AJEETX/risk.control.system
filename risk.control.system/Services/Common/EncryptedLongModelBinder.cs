using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace risk.control.system.Services.Common
{
    public class EncryptedLongModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var encryptedValue = valueProviderResult.FirstValue;
            if (string.IsNullOrEmpty(encryptedValue))
            {
                return Task.CompletedTask;
            }

            var encryptionService = bindingContext.HttpContext.RequestServices.GetRequiredService<IEncryptionService>();
            var decryptedId = encryptionService.UnprotectId(encryptedValue);

            if (decryptedId > 0)
            {
                bindingContext.Result = ModelBindingResult.Success(decryptedId);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid or tampered identifier.");
            }

            return Task.CompletedTask;
        }
    }
}
