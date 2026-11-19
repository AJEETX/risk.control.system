using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Fakes
{
    // Development/E2E stand-ins for the outbound location/verification HTTP calls (Google geocoding, directions,
    // reverse geocoding, weather, ID verification APIs). Real calls without keys are slow or hang, which made
    // case creation and evidence uploads flaky. Registered only when USE_FAKE_EXTERNAL_SERVICES=true in Development.

    internal sealed class FakeCustomApiClient : ICustomApiClient
    {
        // Melbourne CBD: the same point the E2E browser reports as the agent's location.
        private const string Latitude = "-37.8136";
        private const string Longitude = "144.9631";

        public Task<(string Latitude, string Longitude)> GetCoordinatesFromAddressAsync(string address) =>
            Task.FromResult((Latitude, Longitude));

        public Task<string> GetAddressFromLatLong(double latitude, double longitude) =>
            Task.FromResult(FakeAddress.Text);

        public Task<(string distance, float distanceInMetres, string duration, int durationInSeconds, string map)> GetMap(
            double startLat, double startLong, double endLat, double endLong)
        {
            // Callers fill the size with string.Format(map, width, height), like the real Google static map URL.
            var map = "https://maps.googleapis.com/maps/api/staticmap?size={0}x{1}"
                + $"&markers=color:red|label:S|{startLat},{startLong}&markers=color:green|label:E|{endLat},{endLong}&key=";
            return Task.FromResult(("0.1 km", 100f, "1 min", 60, map));
        }
    }

    internal sealed class FakeHttpClientService : IHttpClientService
    {
        public Task<string> GetRawAddress(string lat, string lon) => Task.FromResult(FakeAddress.Text);

        public Task<bool> VerifyAdhar(string adharNumber) => Task.FromResult(true);

        public Task<PanResponse?> VerifyPanNew(string pan, string panUrl, string key, string host) =>
            Task.FromResult<PanResponse?>(null);

        public Task<bool> VerifyPassport(string passport, string dateOfBirth) => Task.FromResult(true);

        public Task<PassportOcrData> GetPassportOcrResult(byte[] imageBytes, string url, string key, string host) =>
            Task.FromResult(new PassportOcrData());
    }

    internal sealed class FakeWeatherInfoService : IWeatherInfoService
    {
        public Task<string> GetWeatherAsync(string latitude, string longitude) =>
            Task.FromResult("Temperature: 21 C. Windspeed: 10 km/h. Elevation(sea level): 31 metres.");
    }

    internal static class FakeAddress
    {
        public const string Text = "1 E2E Street, Melbourne VIC 3000, Australia";
    }
}
