using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using System.Linq;
using SmartBreadcrumbs.Attributes;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using risk.control.system.Helpers;

namespace risk.control.system.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private Regex longLatRegex = new Regex("(?<lat>[-|+| ]\\d+.\\d+)\\s* \\/\\s*(?<lon>\\d+.\\d+)");

        public ReportController(ApplicationDbContext context)
        {
            this._context = context;
            _httpClient = new HttpClient();
        }

        [Breadcrumb(title: " Report", FromController = typeof(ClaimsInvestigationController))]
        public IActionResult Index()
        {
            return View();
        }

        [Breadcrumb(" Detail")]
        public async Task<IActionResult> Detail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.ClientCompany)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CaseEnabler)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CostCentre)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.InvestigationCaseSubStatus)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.BeneficiaryRelation)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.Country)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.District)
              .Include(c => c.InvestigationCaseStatus)
              .Include(c => c.InvestigationCaseSubStatus)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.InvestigationServiceType)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.LineOfBusiness)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = await _context.CaseLocation
                .Include(l => l.ClaimReport)
                .Include(l => l.Vendor)
                .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            if (location.ClaimReport.LocationLongLat != null)
            {
                var longLat = location.ClaimReport.LocationLongLat.IndexOf("/");
                var latitude = location.ClaimReport.LocationLongLat.Substring(0, longLat)?.Trim();
                var longitude = location.ClaimReport.LocationLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();

                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.LocationUrl = url;
                RootObject rootObject = getAddress(latitude, longitude);

                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                else
                {
                    registeredLatitude = Convert.ToDouble(location.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(location.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, 222, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                ViewBag.LocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = getAddress("-37.839542", "145.164834");
                ViewBag.LocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.LocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }
            if (location.ClaimReport.OcrLongLat != null)
            {
                var longLat = location.ClaimReport.OcrLongLat.IndexOf("/");
                var latitude = location.ClaimReport.OcrLongLat.Substring(0, longLat)?.Trim();
                var longitude = location.ClaimReport.OcrLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.OcrLocationUrl = url;
                RootObject rootObject = getAddress(latitude, longitude);

                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, 222, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                ViewBag.OcrLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = getAddress("-37.839542", "145.164834");
                ViewBag.OcrLocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.OcrLocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }

            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var curl = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = curl;

            var beneficiarylatLong = location.PinCode.Latitude + "," + location.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;
            return View(model);
        }

        public static RootObject getAddress(string lat, string lon)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            webClient.Headers.Add("Referer", "http://www.microsoft.com");
            var jsonData = webClient.DownloadData("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(RootObject));
            RootObject rootObject = (RootObject)ser.ReadObject(new MemoryStream(jsonData));
            return rootObject;
        }

        public async Task<RootObject> GetAddress(string lat, string lon)
        {
            var jsonData = await _httpClient.GetStringAsync("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);

            var rootObject = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(jsonData);
            return rootObject;
        }
    }
}