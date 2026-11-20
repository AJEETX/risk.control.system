using Microsoft.Extensions.DependencyInjection.Extensions;
using risk.control.system.Helpers;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;
using risk.control.system.Services.Fakes;
using risk.control.system.Services.Tool;

namespace risk.control.system.StartupExtensions;

/// <summary>
/// Swaps the AI/cloud services behind agent evidence capture (AWS Rekognition/Textract/S3/Transcribe,
/// Google Vision OCR) for local fakes, so the agent flow works without credentials, e.g. in E2E tests.
/// Only active when USE_FAKE_EXTERNAL_SERVICES=true AND the environment is Development.
/// </summary>
public static class FakeExternalServiceExtension
{
    public const string SwitchName = "USE_FAKE_EXTERNAL_SERVICES";

    public static IServiceCollection AddFakeExternalServicesIfEnabled(this IServiceCollection services, IWebHostEnvironment env)
    {
        var enabled = string.Equals(EnvHelper.Get(SwitchName), "true", StringComparison.OrdinalIgnoreCase);
        if (!enabled)
        {
            return services;
        }

        if (!env.IsDevelopment())
        {
            throw new InvalidOperationException($"{SwitchName}=true is only allowed in the Development environment.");
        }

        services.Replace(ServiceDescriptor.Scoped<IAmazonApiService, FakeAmazonApiService>());
        services.Replace(ServiceDescriptor.Scoped<IGoogleOcrService, FakeGoogleOcrService>());
        services.Replace(ServiceDescriptor.Scoped<ISpeech2TextService, FakeSpeech2TextService>());

        // Outbound location/verification HTTP calls (slow or hanging without API keys).
        //services.Replace(ServiceDescriptor.Scoped<ICustomApiClient, FakeCustomApiClient>());
        services.Replace(ServiceDescriptor.Scoped<IHttpClientService, FakeHttpClientService>());
        services.Replace(ServiceDescriptor.Scoped<IWeatherInfoService, FakeWeatherInfoService>());

        Console.WriteLine($"[{SwitchName}] AWS Rekognition/Textract/Transcribe, Google OCR/Maps/geocoding, weather and ID-verification APIs are FAKED.");
        return services;
    }
}
