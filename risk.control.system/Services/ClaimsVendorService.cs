﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IClaimsVendorService
    {
        Task<ClaimsInvestigation> AllocateToVendorAgent(string userEmail, string selectedcase);

        Task<ClaimsInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, string selectedcase);

        Task<ClaimsInvestigationVendorAgentModel> ReSelectVendorAgent(string userEmail, string selectedcase);

        Task<ClaimsInvestigationVendorsModel> GetInvestigate(string userEmail, string selectedcase, bool uploaded = false);

        Task<ClaimsInvestigationVendorsModel> GetInvestigateReport(string userEmail, string selectedcase);

        Task<ClaimsInvestigationVendorAgentModel> GetInvestigateReportReview(string userEmail, string selectedcase);

        Task<ClaimTransactionModel> GetClaimsDetails(string userEmail, string selectedcase);

        Task<List<VendorUserClaim>> GetAgentLoad(string userEmail);

        Task<AppiCheckifyResponse> PostFaceId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null);

        Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null);
    }

    public class ClaimsVendorService : IClaimsVendorService
    {
        private readonly IICheckifyService checkifyService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDashboardService dashboardService;
        private readonly IHttpClientService httpClientService;
        private readonly IWebHostEnvironment webHostEnvironment;

        //private static string latitude = "-37.839542";
        //private static string longitude = "145.164834";
        private static HttpClient httpClient = new();

        public ClaimsVendorService(IICheckifyService checkifyService,
            UserManager<VendorApplicationUser> userManager,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IDashboardService dashboardService,
            IHttpClientService httpClientService,
            IWebHostEnvironment webHostEnvironment)
        {
            this.checkifyService = checkifyService;
            this.userManager = userManager;
            this._context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.dashboardService = dashboardService;
            this.httpClientService = httpClientService;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null)
        {
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "agency", "pan.jpg");

            var noDataimage = image != null ? image : await File.ReadAllBytesAsync(noDataImagefilePath);

            var data = new DocumentData
            {
                Email = userEmail,
                ClaimId = claimId,
                OcrImage = Convert.ToBase64String(noDataimage),
                OcrLongLat = $"{latitude}/{longitude}"
            };
            var result = await checkifyService.GetDocumentId(data);
            return result;
        }

        public async Task<AppiCheckifyResponse> PostFaceId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null)
        {
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "agency", "ajeet.jpg");

            var noDataimage = image != null ? image : await File.ReadAllBytesAsync(noDataImagefilePath);

            //var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            //if (ipAddress != null)
            //{
            //    var address = await httpClientService.GetAddressFromIp(ipAddress);
            //    if (address != null)
            //    {
            //        latitude = address.lat.ToString();
            //        longitude = address.lat.ToString();
            //    }
            //}

            var data = new FaceData
            {
                Email = userEmail,
                ClaimId = claimId,
                LocationImage = Convert.ToBase64String(noDataimage),
                LocationLongLat = $"{latitude}/{longitude}"
            };
            var result = await checkifyService.GetFaceId(data);
            return result;
        }

        public async Task<ClaimsInvestigation> AllocateToVendorAgent(string userEmail, string selectedcase)
        {
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);

            var claimsInvestigation = _context.ClaimsInvestigation
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
                .FirstOrDefault(m => m.ClaimsInvestigationId == selectedcase && m.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
            claimsInvestigation.CaseLocations = claimsInvestigation.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                        && c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId)?.ToList();

            return claimsInvestigation;
        }

        public async Task<ClaimsInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, string selectedcase)
        {
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
            i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);

            var claimsCaseToAllocateToVendorAgent = _context.ClaimsInvestigation
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
                .Include(c => c.Vendors)
                .FirstOrDefault(v => v.ClaimsInvestigationId == selectedcase);

            var claimsCaseLocation = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.Vendor)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.CaseLocationId == claimsCaseToAllocateToVendorAgent.CaseLocations.FirstOrDefault().CaseLocationId &&
                c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId);

            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));

            var vendorUsers = _context.VendorApplicationUser
                .Include(u => u.District)
                .Include(u => u.State)
                .Include(u => u.Country)
                .Include(u => u.PinCode)
                .Where(u => u.VendorId == claimsCaseLocation.VendorId && u.Active);

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            foreach (var vendorUser in vendorUsers)
            {
                var isTrue = await userManager.IsInRoleAsync(vendorUser, agentRole?.Name);
                if (isTrue)
                {
                    int claimCount = 0;
                    if (result.TryGetValue(vendorUser.Email, out claimCount))
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = claimCount,
                        };
                        agents.Add(agentData);
                    }
                    else
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = 0,
                        };
                        agents.Add(agentData);
                    }
                }
            }

            var model = new ClaimsInvestigationVendorAgentModel
            {
                CaseLocation = claimsCaseLocation,
                ClaimsInvestigation = claimsCaseToAllocateToVendorAgent,
                VendorUserClaims = agents
            };
            return model;
        }

        public async Task<ClaimsInvestigationVendorsModel> GetInvestigate(string userEmail, string selectedcase, bool uploaded = false)
        {
            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
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
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(c => c.District)
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.DocumentIdReport)
                .Include(c => c.ClaimReport)
                    .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.ReportQuestionaire)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                    );
            claimCase.ClaimReport.AgentEmail = userEmail;

            if (!claimsInvestigation.IsReviewCase && !claimCase.IsReviewCaseLocation && claimCase.ClaimReport.DigitalIdReport?.DigitalIdImageLongLat != null)
            {
                var longLat = claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLocationUrl = url;

                RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));

                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                else
                {
                    registeredLatitude = Convert.ToDouble(claimCase.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimCase.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLocationAddress = "No Address data";

                string weatherCustomData = $"No Location Info...";
                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageData = weatherCustomData;
            }

            if (claimCase.ClaimReport.DocumentIdReport?.DocumentIdImageLongLat != null)
            {
                var longLat = claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLocationUrl = url;
                RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }

                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;
                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageData = claimCase.ClaimReport.DocumentIdReport.DocumentIdImageData ?? "Sample data";
                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                string weatherCustomData = $"No Location Info...";
                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageData = weatherCustomData;
                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLocationAddress = "No Address data";
            }

            var model = new ClaimsInvestigationVendorsModel { Location = claimCase, ClaimsInvestigation = claimsInvestigation };

            return model;
        }

        public async Task<ClaimsInvestigationVendorsModel> GetInvestigateReport(string userEmail, string selectedcase)
        {
            var claimsInvestigation = _context.ClaimsInvestigation
              .Include(c => c.ClaimMessages)
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
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.ClaimReport)
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
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);

            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.DocumentIdReport)
                .Include(c => c.ClaimReport)
                    .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.ReportQuestionaire)
                .Include(c => c.District)
                .Include(c => c.Country)
                .Include(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            if (claimsInvestigation.IsReviewCase)
            {
                claimCase.ClaimReport.SupervisorRemarks = null;
            }
            return (new ClaimsInvestigationVendorsModel { Location = claimCase, ClaimsInvestigation = claimsInvestigation });
        }

        public async Task<ClaimTransactionModel> GetClaimsDetails(string userEmail, string selectedcase)
        {
            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == selectedcase)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
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
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport.DigitalIdReport)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport.DocumentIdReport)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport.ServiceReportTemplate.ReportTemplate.DigitalIdReport)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport.ServiceReportTemplate.ReportTemplate.DocumentIdReport)
                .Include(c => c.CaseLocations)
                    .ThenInclude(c => c.ClaimReport.ServiceReportTemplate.ReportTemplate.ReportQuestionaire)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };
            return model;
        }

        public async Task<List<VendorUserClaim>> GetAgentLoad(string userEmail)
        {
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));
            List<VendorUserClaim> agents = new List<VendorUserClaim>();

            var vendor = _context.Vendor
                .Include(c => c.VendorApplicationUser)
                .FirstOrDefault(c => c.VendorId == vendorUser.VendorId);

            var users = vendor.VendorApplicationUser.AsQueryable();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            foreach (var user in users)
            {
                var isAgent = await userManager.IsInRoleAsync(user, agentRole?.Name);
                if (isAgent)
                {
                    int claimCount = 0;
                    if (result.TryGetValue(user.Email, out claimCount))
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = user,
                            CurrentCaseCount = claimCount,
                        };
                        agents.Add(agentData);
                    }
                    else
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = user,
                            CurrentCaseCount = 0,
                        };
                        agents.Add(agentData);
                    }
                }
            }
            return agents;
        }

        public async Task<ClaimsInvestigationVendorAgentModel> ReSelectVendorAgent(string userEmail, string selectedcase)
        {
            var submittedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
            i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var claimsCaseToAllocateToVendorAgent = _context.ClaimsInvestigation
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
                .Include(c => c.Vendors)
                .FirstOrDefault(v => v.ClaimsInvestigationId == selectedcase);

            var location = claimsCaseToAllocateToVendorAgent.CaseLocations.FirstOrDefault();

            var claimsCaseLocation = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.Vendor)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.CaseLocationId == location.CaseLocationId &&
                c.InvestigationCaseSubStatusId == submittedStatus.InvestigationCaseSubStatusId);

            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));

            var vendorUsers = _context.VendorApplicationUser
                .Include(u => u.District)
                .Include(u => u.State)
                .Include(u => u.Country)
                .Include(u => u.PinCode)
                .Where(u => u.VendorId == claimsCaseLocation.VendorId && u.Active);

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            foreach (var vendorUser in vendorUsers)
            {
                var isTrue = await userManager.IsInRoleAsync(vendorUser, agentRole?.Name);
                if (isTrue)
                {
                    int claimCount = 0;
                    if (result.TryGetValue(vendorUser.Email, out claimCount))
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = claimCount,
                        };
                        agents.Add(agentData);
                    }
                    else
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = 0,
                        };
                        agents.Add(agentData);
                    }
                }
            }

            var model = new ClaimsInvestigationVendorAgentModel
            {
                CaseLocation = claimsCaseLocation,
                ClaimsInvestigation = claimsCaseToAllocateToVendorAgent,
                VendorUserClaims = agents
            };

            location.ClaimReport = null;

            _context.SaveChanges();

            return model;
        }

        public async Task<ClaimsInvestigationVendorAgentModel> GetInvestigateReportReview(string userEmail, string selectedcase)
        {
            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
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
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var submittedToSupervisortStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .Include(c => c.District)
                .Include(c => c.State)
                .FirstOrDefault(c => (c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == submittedToSupervisortStatus.InvestigationCaseSubStatusId) || c.IsReviewCaseLocation
                    );
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));

            var vendorUsers = _context.VendorApplicationUser.Where(u => u.VendorId == claimCase.VendorId);

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            foreach (var vendorUser in vendorUsers)
            {
                var isTrue = await userManager.IsInRoleAsync(vendorUser, agentRole?.Name);
                if (isTrue)
                {
                    int claimCount = 0;
                    if (result.TryGetValue(vendorUser.Email, out claimCount))
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = claimCount,
                        };
                        agents.Add(agentData);
                    }
                    else
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = 0,
                        };
                        agents.Add(agentData);
                    }
                }
            }
            return (new ClaimsInvestigationVendorAgentModel
            {
                CaseLocation = claimCase,
                ClaimsInvestigation = claimsInvestigation,
                VendorUserClaims = agents
            }
            );
        }
    }
}