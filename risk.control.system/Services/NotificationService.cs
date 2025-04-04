using System.Net;

using Amazon.Rekognition.Model;

using AspNetCoreHero.ToastNotification.Abstractions;

using Google.Api;

using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SkiaSharp;

namespace risk.control.system.Services
{
    public interface INotificationService
    {
        Task ClearAll(string userEmail);
        Task MarkAsRead(int id, string userEmail);
        Task<List<StatusNotification>> GetNotifications(string userEmail);
        Task<ClaimsInvestigation> SendVerifySchedule(ClientSchedulingMessage message);

        Task<ClaimsInvestigation> ReplyVerifySchedule(string id, string confirm = "N");

        Task<IpApiResponse?> GetClientIp(string? ipAddress, CancellationToken ct, string page, string userEmail = "", bool isAuthenticated = false, string latlong = "");
        Task<IpApiResponse?> GetAgentIp(string? ipAddress, CancellationToken ct, string page, string userEmail = "", bool isAuthenticated = false, string latlong = "");

        Task<(ClaimMessage message, string yes, string no)> GetClaim(string baseUrl, string id);

        Task<string> SendSms2Customer(string currentUser, string claimId, string sms);

        Task<string> SendSms2Beneficiary(string currentUser, string claimId, string sms);
        bool IsWhiteListIpAddress(IPAddress remoteIp);
    }

    public class NotificationService : INotificationService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext context;
        private readonly ISmsService smsService;
        private readonly IWebHostEnvironment webHostEnvironment;
        //private readonly ICustomApiCLient customerApiclient;
        private readonly IHttpClientService httpClientService;
        private readonly IFeatureManager featureManager;
        private static string logo = "https://icheckify.co.in";
        private static System.Net.WebClient client = new System.Net.WebClient();
        private const string IP_BASE_URL = "http://ip-api.com";

        private static HttpClient _httpClient = new HttpClient();

        public NotificationService(ApplicationDbContext context,
            ISmsService SmsService,
            IWebHostEnvironment webHostEnvironment,
            //ICustomApiCLient customerApiclient,
            IHttpClientService httpClientService,
            IFeatureManager featureManager)
        {
            this.context = context;
            smsService = SmsService;
            this.webHostEnvironment = webHostEnvironment;
            //this.customerApiclient = customerApiclient;
            this.httpClientService = httpClientService;
            this.featureManager = featureManager;
        }

        public async Task<IpApiResponse?> GetClientIp(string? ipAddress, CancellationToken ct, string page, string userEmail = "", bool isAuthenticated = false, string latlong = "")
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(latlong))
                {
                    var route = $"{IP_BASE_URL}/json/{ipAddress}";
                    page = page == "/" ? "dashboard" : page;
                    var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(route, ct);
                    var lat = latlong.Substring(0, latlong.IndexOf(","));
                    var lng = latlong.Substring(latlong.IndexOf(",") + 1);
                    //var newAddress = await customerApiclient.GetAddressFromLatLong(double.Parse(lat), double.Parse(lng));
                    var address = await httpClientService.GetAddress(lat, lng);
                    var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={latlong}&zoom=15&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latlong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

                    if (response != null && (await featureManager.IsEnabledAsync(FeatureFlags.IPTracking)))
                    {
                        response.country = address?.features[0].properties.country;
                        response.regionName = address?.features[0].properties?.state;
                        response.city = address?.features[0].properties?.county ?? response.city;
                        response.district = address?.features[0].properties?.city;
                        response.zip = address?.features[0].properties?.postcode;
                        response.lat = address?.features[0].properties.lat;
                        response.lon = address?.features[0].properties.lon;
                        response.user = !string.IsNullOrWhiteSpace(userEmail) ? userEmail : "Guest";
                        response.MapUrl = mapUrl;
                        response.page = page;
                        response.isAuthenticated = isAuthenticated;
                        if ((isAuthenticated && !string.IsNullOrWhiteSpace(userEmail) && userEmail != Applicationsettings.PORTAL_ADMIN.EMAIL) || !isAuthenticated)
                        {
                            var user = context.ApplicationUser.FirstOrDefault(a => a.Email == userEmail);
                            if (user != null)
                            {
                                var userSessionAlive = new UserSessionAlive
                                {
                                    Updated = DateTime.Now,
                                    ActiveUser = user,
                                    CurrentPage = page,
                                    Created = DateTime.Now,
                                    IsUpdated = false,
                                    UpdatedBy = user.Email
                                };
                                context.UserSessionAlive.Add(userSessionAlive);
                            }
                            context.IpApiResponse.Add(response);
                            await context.SaveChangesAsync(false);
                        }
                        return response;
                    }
                }
                //else
                //{
                //    var longLatString = response?.lat.GetValueOrDefault().ToString() + "," + response?.lon.GetValueOrDefault().ToString();
                //    var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=6&size=560x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                //    response.page = page;
                //    response.user = userEmail;
                //    response.isAuthenticated = isAuthenticated;
                //    response.MapUrl = mapUrl;
                //    context.IpApiResponse.Add(response);
                //    await context.SaveChangesAsync();
                //}

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return null!;
        }
        public async Task<IpApiResponse?> GetAgentIp(string? ipAddress, CancellationToken ct, string page, string userEmail = "", bool isAuthenticated = false, string latlong = "")
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(latlong))
                {
                    var route = $"{IP_BASE_URL}/json/{ipAddress}";
                    page = page == "/" ? "dashboard" : page;
                    var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(route, ct);
                    var lat = latlong.Substring(0, latlong.IndexOf(","));
                    var lng = latlong.Substring(latlong.IndexOf(",") + 1);
                    //var newAddress = await customerApiclient.GetAddressFromLatLong(double.Parse(lat), double.Parse(lng));
                    var address = await httpClientService.GetAddress(lat, lng);
                    var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={latlong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latlong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

                    if (response != null && (await featureManager.IsEnabledAsync(FeatureFlags.IPTracking)))
                    {
                        response.country = address?.features[0].properties.country;
                        response.regionName = address?.features[0].properties?.state;
                        response.city = address?.features[0].properties?.county ?? response.city;
                        response.district = address?.features[0].properties?.city;
                        response.zip = address?.features[0].properties?.postcode;
                        response.lat = address?.features[0].properties.lat;
                        response.lon = address?.features[0].properties.lon;
                        response.user = !string.IsNullOrWhiteSpace(userEmail) ? userEmail : "Guest";
                        response.MapUrl = mapUrl;
                        response.page = page;
                        response.isAuthenticated = isAuthenticated;
                        if ((isAuthenticated && !string.IsNullOrWhiteSpace(userEmail) && userEmail != Applicationsettings.PORTAL_ADMIN.EMAIL) || !isAuthenticated)
                        {
                            var user = context.ApplicationUser.FirstOrDefault(a => a.Email == userEmail);
                            var userSessionAlive = new UserSessionAlive
                            {
                                Updated = DateTime.Now,
                                ActiveUser = user,
                                CurrentPage = page
                            };
                            context.UserSessionAlive.Add(userSessionAlive);

                            context.IpApiResponse.Add(response);
                            await context.SaveChangesAsync(false);
                        }
                        return response;
                    }
                }
                //else
                //{
                //    var longLatString = response?.lat.GetValueOrDefault().ToString() + "," + response?.lon.GetValueOrDefault().ToString();
                //    var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=6&size=560x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                //    response.page = page;
                //    response.user = userEmail;
                //    response.isAuthenticated = isAuthenticated;
                //    response.MapUrl = mapUrl;
                //    context.IpApiResponse.Add(response);
                //    await context.SaveChangesAsync();
                //}

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return null!;
        }
        public bool IsWhiteListIpAddress(IPAddress remoteIp)
        {
            var bytes = remoteIp.GetAddressBytes();
            var whitelistedIp = false;
            var ipAddresses = context.ClientCompany.Where(c => !string.IsNullOrWhiteSpace(c.WhitelistIpAddress)).Select(c => c.WhitelistIpAddress).ToList();

            if (ipAddresses.Any())
            {
                var safelist = string.Join(";", ipAddresses);
                var ips = safelist.Split(';');
                var _safelist = new byte[ips.Length][];
                for (var i = 0; i < ips.Length; i++)
                {
                    _safelist[i] = IPAddress.Parse(ips[i]).GetAddressBytes();
                }
                foreach (var address in _safelist)
                {
                    if (address.SequenceEqual(bytes))
                    {
                        return true;
                    }
                }
            }
            return whitelistedIp;
        }
        public async Task<ClaimsInvestigation> SendVerifySchedule(ClientSchedulingMessage message)
        {
            var claim = context.ClaimsInvestigation
                .Include(c => c.ClaimMessages)
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == message.ClaimId);
            var assignedToAgentStatus = context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var beneficiary = context.BeneficiaryDetail.Include(c=>c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == message.ClaimId);

            string mobile = string.Empty;
            string recepientName = string.Empty;
            string recepientPhone = string.Empty;
            int isdCode = claim.CustomerDetail.PinCode.Country.ISDCode;
            var underWritingLineOfBusiness = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
            {
                mobile = claim.CustomerDetail.ContactNumber.ToString();
                recepientName = claim.CustomerDetail.Name;
                recepientPhone = claim.CustomerDetail.ContactNumber.ToString();
            }
            else
            {
                mobile = beneficiary.ContactNumber.ToString();
                recepientName = beneficiary.Name;
                recepientPhone = beneficiary.ContactNumber.ToString();
                isdCode = beneficiary.Country.ISDCode;
            }

            string? attachments = $"<a href='{logo}'>team</a>";

            string baseUrl = $"{message.BaseUrl}/api/notification/ConfirmSchedule?id={message.ClaimId}&confirm=";
            string yesUrl = $"{baseUrl}Y";
            string noUrl = $"{baseUrl}N";

            var address = new Uri("http://tinyurl.com/api-create.php?url=" + yesUrl);
            var yesTinyUrl = client.DownloadString(address);

            address = new Uri("http://tinyurl.com/api-create.php?url=" + noUrl);
            var noTinyUrl = client.DownloadString(address);
            string agentMessage = $"Dear {recepientName}";
            agentMessage += "                       ";
            agentMessage += $"  {claim.CurrentClaimOwner} visit you on Date: {message.Time} for the claim policy {claim.PolicyDetail.ContractNumber}.            ";

            var verifyMessage = agentMessage;
            verifyMessage += "                                                       ";
            //verifyMessage += $"                                                       Click  (Yes){yesTinyUrl} ";
            //verifyMessage += "---------------------------------------";
            //verifyMessage += "                                                       ";
            //verifyMessage += $"                                                       or  (No){noTinyUrl}";
            //verifyMessage += "                                                       ";
            bool priority = true;

            var path = Path.Combine(webHostEnvironment.WebRootPath, "form", "ConfirmSchedule.html");

            var subject = "Verify Your E-mail Address ";
            string HtmlBody = "";
            using (StreamReader stream = File.OpenText(path))
            {
                HtmlBody = stream.ReadToEnd();
            }

            var confirmPage = $"{message.BaseUrl + "/Confirm?id=" + message.ClaimId}";
            address = new Uri("http://tinyurl.com/api-create.php?url=" + confirmPage);
            var confirmTinyUrl = client.DownloadString(address);

            string? callbackUrl = message.BaseUrl + "";
            string confirmPageUrl = "                                                                      Click to ";
            confirmPageUrl += $"                                                                      (CONFIRM){confirmTinyUrl}";
            confirmPageUrl += "                                                                      ";
            string finalMessage = verifyMessage;
            finalMessage += "                                                                               ";
            finalMessage += confirmPageUrl;
            finalMessage += "Thanks";
            finalMessage += "                                                                               ";
            finalMessage += logo;
            string messageBody = string.Format(HtmlBody,
                subject,
                string.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
                recepientName,
                recepientName,
                confirmPageUrl,
                yesTinyUrl,
                noTinyUrl
                );

            await smsService.DoSendSmsAsync("+" +isdCode + mobile, finalMessage);
            var meetingTime = DateTime.Now.AddDays(1);
            if (DateTime.TryParse(message.Time, out DateTime date))
            {
                meetingTime = date;
            }

            var senderDetail = context.ApplicationUser.FirstOrDefault(u => u.Email == claim.CurrentClaimOwner);

            var scheduleMessage = new ClaimMessage
            {
                Message = finalMessage,
                ClaimsInvestigationId = message.ClaimId,
                RecepicientEmail = recepientName,
                SenderEmail = claim.CurrentClaimOwner,
                UpdatedBy = claim.CurrentClaimOwner,
                Updated = DateTime.Now,
                ScheduleTime = meetingTime,
                SenderPhone = senderDetail.PhoneNumber,
                RecepicientPhone = recepientPhone
            };
            claim.ClaimMessages.Add(scheduleMessage);
            await context.SaveChangesAsync();

            return claim;
        }

        public async Task<ClaimsInvestigation> ReplyVerifySchedule(string id, string confirm = "N")
        {
            var claim = context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == id);
            var assignedToAgentStatus = context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var beneficiary = context.BeneficiaryDetail.Include(b=>b.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == id);
            var underWritingLineOfBusiness = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

            string recepientName = string.Empty;
            string recepientPhone = string.Empty;
            int isdCode = claim.CustomerDetail.Country.ISDCode;
            if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
            {
                recepientName = claim.CustomerDetail.Name;
                recepientPhone = claim.CustomerDetail.ContactNumber.ToString();
            }
            else
            {
                recepientName = beneficiary.Name;
                recepientPhone = beneficiary.ContactNumber.ToString();
                isdCode = beneficiary.Country.ISDCode;
            }

            if (confirm.ToUpper() == "Y")
            {
                confirm = "YES";
            }
            else if (confirm.ToUpper() == "N")
            {
                confirm = "NO";
            }

            string device = "0";
            long? timestamp = null;
            bool isMMS = false;

            string agentMessage = $"Dear {claim.CurrentClaimOwner},";
            agentMessage += $"                              ";
            agentMessage += $"{recepientName} has replied  {confirm} to your Visit schedule for claim Policy :{claim.PolicyDetail.ContractNumber}";
            agentMessage += "                              ";

            var finalMessage = $"{agentMessage}";
            finalMessage += "                               ";
            finalMessage += $"Dated: {DateTime.Now.ToString("dd-MMM-yyyy HH:mm")}";
            finalMessage += "                               ";
            finalMessage += $"{logo}";
            bool priority = true;

            var previousMessage = context.ClaimMessage.FirstOrDefault(u => u.ClaimsInvestigationId == claim.ClaimsInvestigationId);
            var agent = context.VendorApplicationUser.FirstOrDefault(u => u.Email == claim.CurrentClaimOwner);

            await smsService.DoSendSmsAsync("+" + agent.PhoneNumber, finalMessage);

            var scheduleMessage = new ClaimMessage
            {
                Message = finalMessage,
                ClaimsInvestigationId = id,
                RecepicientEmail = claim.CurrentClaimOwner,
                SenderEmail = recepientName,
                UpdatedBy = recepientName,
                Updated = DateTime.Now,
                ScheduleTime = previousMessage.ScheduleTime,
                SenderPhone = recepientPhone,
                RecepicientPhone = agent.PhoneNumber,
                PreviousClaimMessageId = previousMessage.PreviousClaimMessageId
            };
            claim.ClaimMessages.Add(scheduleMessage);
            await context.SaveChangesAsync();
            return claim;
        }

        public async Task<(ClaimMessage message, string yes, string no)> GetClaim(string baseUrl, string id)
        {
            var claim = await context.ClaimsInvestigation
             .Include(c => c.ClaimMessages)
             .Include(c => c.PolicyDetail)
             .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
             .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == id);
            var assignedToAgentStatus = context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var beneficiary = context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.Country)
                .Include(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == id);
            var underWritingLineOfBusiness = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

            string mobile = string.Empty;
            string recepientName = string.Empty;
            if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
            {
                mobile = claim.CustomerDetail.ContactNumber.ToString();
                recepientName = claim.CustomerDetail.Name;
            }
            else 
            {
                mobile = beneficiary.ContactNumber.ToString();
                recepientName = beneficiary.Name;
            }

            //var path = Path.Combine(webHostEnvironment.WebRootPath, "form", "ConfirmAcountRegister.html");

            //var subject = "Verify Your E-mail Address ";
            //string HtmlBody = "";
            //using (StreamReader stream = File.OpenText(path))
            //{
            //    HtmlBody = stream.ReadToEnd();
            //}

            string fullUrl = $"{baseUrl}/api/notification/ConfirmSchedule?id={id}&confirm=";
            string yesUrl = $"{fullUrl}Y";
            string noUrl = $"{fullUrl}N";

            var address = new Uri("http://tinyurl.com/api-create.php?url=" + yesUrl);
            var yesTinyUrl = client.DownloadString(address);

            address = new Uri("http://tinyurl.com/api-create.php?url=" + noUrl);
            var noTinyUrl = client.DownloadString(address);

            var agent = context.VendorApplicationUser.FirstOrDefault(u => u.Email == claim.CurrentClaimOwner);

            //string messageBody = string.Format(HtmlBody,
            //    subject,
            //    string.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
            //    recepientName,
            //    recepientName,
            //    "#",
            //    yesTinyUrl,
            //    noTinyUrl
            //    );
            var scheduleMessage = context.ClaimMessage
                .Where(m => m.ClaimsInvestigationId == id)?
                .OrderByDescending(m => m.Created)?.FirstOrDefault();
            return (scheduleMessage, yesUrl, noUrl);
        }

        public async Task<string> SendSms2Customer(string currentUser, string claimId, string sms)
        {
            var claim = await context.ClaimsInvestigation
            .Include(c => c.ClaimMessages)
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claimId);

            var mobile = claim.CustomerDetail.ContactNumber.ToString();
            var user = context.ApplicationUser.FirstOrDefault(u => u.Email == currentUser);
            var isdCode = claim.CustomerDetail.Country.ISDCode;
            var isInsurerUser = user is ClientCompanyApplicationUser;
            var isVendorUser = user is VendorApplicationUser;

            string company = string.Empty;
            ClientCompanyApplicationUser insurerUser;
            VendorApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = (ClientCompanyApplicationUser)user;
                company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == insurerUser.ClientCompanyId)?.Name;
            }
            else if (isVendorUser)
            {
                agencyUser = (VendorApplicationUser)user;
                company = context.Vendor.FirstOrDefault(v => v.VendorId == agencyUser.VendorId).Name;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var message = $"Dear {claim.CustomerDetail.Name}";
            message += "                                                                                ";
            message += $"{sms}";
            message += "                                                                                ";
            message += $"Thanks";
            message += "                                                                                ";
            message += $"{user.FirstName} {user.LastName}";
            message += "                                                                                ";
            message += $"Policy #:{claim.PolicyDetail.ContractNumber}";
            message += "                                                                                ";
            message += $"{company}";
            message += "                                                                                ";
            message += $"{logo}";

            var scheduleMessage = new ClaimMessage
            {
                Message = message,
                ClaimsInvestigationId = claimId,
                RecepicientEmail = claim.CurrentClaimOwner,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.Now
            };
            claim.ClaimMessages.Add(scheduleMessage);
            context.SaveChanges();
            await smsService.DoSendSmsAsync("+"+isdCode + mobile, message);
            return claim.CustomerDetail.Name;
        }

        public async Task<string> SendSms2Beneficiary(string currentUser, string claimId, string sms)
        {
            var beneficiary = await context.BeneficiaryDetail
                .Include(b => b.Country)
                .Include(b => b.ClaimsInvestigation)
                .ThenInclude(c => c.PolicyDetail)
               .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claimId);

            var mobile = beneficiary.ContactNumber.ToString();
            var user = context.ApplicationUser.FirstOrDefault(u => u.Email == currentUser);
            var isdCode = beneficiary.Country.ISDCode;

            var isInsurerUser = user is ClientCompanyApplicationUser;
            var isVendorUser = user is VendorApplicationUser;

            string company = string.Empty;
            ClientCompanyApplicationUser insurerUser;
            VendorApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = (ClientCompanyApplicationUser)user;
                company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == insurerUser.ClientCompanyId)?.Name;
            }
            else if (isVendorUser)
            {
                agencyUser = (VendorApplicationUser)user;
                company = context.Vendor.FirstOrDefault(v => v.VendorId == agencyUser.VendorId).Name;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var message = $"Dear {beneficiary.Name}";
            message += "                                                                                ";
            message += $"{sms}";
            message += "                                                                                ";
            message += $"Thanks";
            message += "                                                                                ";
            message += $"{user.FirstName} {user.LastName}";
            message += "                                                                                ";
            message += $"Policy #:{beneficiary.ClaimsInvestigation.PolicyDetail.ContractNumber}";
            message += "                                                                                ";
            message += $"{company}";
            message += "                                                                                ";
            message += $"{logo}";

            var scheduleMessage = new ClaimMessage
            {
                Message = message,
                ClaimsInvestigationId = claimId,
                RecepicientEmail = beneficiary.Name,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.Now
            };
            var claim = context.ClaimsInvestigation
            .Include(c => c.ClaimMessages)
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
            .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            claim.ClaimMessages.Add(scheduleMessage);
            context.SaveChanges();
            await smsService.DoSendSmsAsync("+"+isdCode + mobile, message);
            return beneficiary.Name;
        }

        public async Task<List<StatusNotification>> GetNotifications(string userEmail)
        {
            
            var companyUser = context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            ApplicationRole role = null!;
            ClientCompany company = null!;
            Vendor agency = null!;
            if (companyUser != null)
            {
                role = context.ApplicationRole.FirstOrDefault(r => r.Name == companyUser.Role.ToString());
                company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                var notifications = context.Notifications.Where(n => n.Role == role && n.Company == company && (!n.IsReadByCreator || !n.IsReadByManager || !n.IsReadByAssessor));
                if (role.Name == AppRoles.ASSESSOR.ToString())
                {
                    notifications = notifications.Where(n => n.Role == role && !n.IsReadByAssessor);
                }
                else if (role.Name == AppRoles.MANAGER.ToString())
                {
                    notifications = notifications.Where(n => n.Role == role && !n.IsReadByManager && n.CreatedBy != userEmail);
                }

                else if (role.Name == AppRoles.CREATOR.ToString())
                {
                    notifications = notifications.Where(n => n.Role == role && !n.IsReadByCreator);
                }

                var activeNotifications = await notifications
                    .OrderByDescending(n => n.CreatedAt).ToListAsync();
                return activeNotifications;
            }
            else if (vendorUser != null)
            {
                role = context.ApplicationRole.FirstOrDefault(r => r.Name == vendorUser.Role.ToString());
                agency = context.Vendor.FirstOrDefault(c => c.VendorId == vendorUser.VendorId);
                
                var notifications = context.Notifications.Where(n => n.Agency == agency && (!n.IsReadByVendor || !n.IsReadByVendorAgent));
                var notificationsss = context.Notifications.Where(n => n.Agency == agency && (!n.IsReadByVendor || !n.IsReadByVendorAgent)).ToList();

                if (role.Name == AppRoles.AGENT.ToString())
                {
                    notifications = notifications.Where(n => n.UserEmail == userEmail);
                }
                else
                {
                    var superRole = context.ApplicationRole.FirstOrDefault(r => r.Name == AppRoles.SUPERVISOR.ToString());
                    notifications = notifications.Where(n => n.Role == superRole && (!n.IsReadByVendor));
                }


               var activeNotifications = await notifications
                    .OrderByDescending(n => n.CreatedAt).ToListAsync();
                return activeNotifications;
            }
            var allNotifications = await context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return allNotifications;
        }

        public async Task MarkAsRead(int id, string userEmail)
        {
            var companyUser = context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            ApplicationRole role = null!;
            ClientCompany company = null!;
            Vendor agency = null!;
            if(companyUser !=null)
            {
                role = context.ApplicationRole.FirstOrDefault(r => r.Name == companyUser.Role.ToString());
                company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                var notification = context.Notifications.FirstOrDefault(s=>s.Role == role && s.Company == company && s.StatusNotificationId == id);
                if(notification == null)
                {
                    return;
                }
                if (role.Name == AppRoles.ASSESSOR.ToString())
                {
                    notification.IsReadByAssessor = true;
                }
                else if(role.Name == AppRoles.MANAGER.ToString())
                {
                    notification.IsReadByManager = true;
                }

                else if (role.Name == AppRoles.CREATOR.ToString())
                {
                    notification.IsReadByCreator = true;
                }
                context.Notifications.Update(notification);
                var rows = await context.SaveChangesAsync();
            }
            else if (vendorUser != null)
            {
                role = context.ApplicationRole.FirstOrDefault(r => r.Name == vendorUser.Role.ToString());
                agency = context.Vendor.FirstOrDefault(c => c.VendorId == vendorUser.VendorId);
                var notification = context.Notifications.FirstOrDefault(s=> s.Agency == agency && s.StatusNotificationId == id);
                if (notification == null)
                {
                    return;
                }
                if (role.Name == AppRoles.AGENCY_ADMIN.ToString() || role.Name == AppRoles.SUPERVISOR.ToString())
                {
                    notification.IsReadByVendor = true;
                }

                else if (role.Name == AppRoles.AGENT.ToString())
                {
                    notification.IsReadByVendorAgent = true;
                }
                context.Notifications.Update(notification);
                var rows = await context.SaveChangesAsync();
            }
        }

        public async Task ClearAll(string userEmail)
        {
            var notifications = await GetNotifications(userEmail);
            foreach (var notification in notifications)
            {
                await MarkAsRead(notification.StatusNotificationId,userEmail);
            }
        }
    }
}