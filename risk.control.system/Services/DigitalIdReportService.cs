using Highsoft.Web.Mvc.Charts;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using System.Net.Http;
using risk.control.system.Data;
using risk.control.system.Controllers.Api.Claims;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;

namespace risk.control.system.Services
{
    public interface IDigitalIdReportService
    {

    }
    public class DigitalIdReportService
    {
        private readonly ApplicationDbContext context;
        private readonly IClaimsService claimsService;
        private readonly IHttpClientService httpClientService;
        private static HttpClient httpClient = new();

        public DigitalIdReportService(ApplicationDbContext context,
            IClaimsService claimsService,
            IHttpClientService httpClientService
            )
        {
            this.context = context;
            this.claimsService = claimsService;
            this.httpClientService = httpClientService;
        }
        public async Task SaveReport(FaceData data)
        {
            var claim = claimsService.GetClaims()
                               .Include(c => c.AgencyReport)
                               .ThenInclude(c => c.DigitalIdReport)
                               .Include(c => c.AgencyReport)
                               .ThenInclude(c => c.ReportQuestionaire)
                               .Include(c => c.AgencyReport)
                               .ThenInclude(c => c.DocumentIdReport)
                               .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime = DateTime.Now;
            claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat = data.LocationLongLat;

            var longLat = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
            var latitude = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";
            claim.AgencyReport.DigitalIdReport.DigitalIdImageData = weatherCustomData;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
            claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl = url;

            var address = await httpClientService.GetRawAddress((latitude), (longitude));
            double registeredLatitude = 0;
            double registeredLongitude = 0;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                registeredLatitude = Convert.ToDouble(claim.CustomerDetail.PinCode.Latitude);
                registeredLongitude = Convert.ToDouble(claim.CustomerDetail.PinCode.Longitude);
            }
            else
            {
                registeredLatitude = Convert.ToDouble(claim.BeneficiaryDetail.PinCode.Latitude);
                registeredLongitude = Convert.ToDouble(claim.BeneficiaryDetail.PinCode.Longitude);
            }
            var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

            claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationAddress = address;
            claim.AgencyReport.DigitalIdReport.Updated = DateTime.Now;
            claim.AgencyReport.DigitalIdReport.UpdatedBy = claim.AgencyReport.AgentEmail;
        }
    }
}
