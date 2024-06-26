﻿using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api.Claims;
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
        private readonly IDashboardService dashboardService;
        private readonly IHttpClientService httpClientService;
        private readonly IClaimsService claimsService;

        //private static string latitude = "-37.839542";
        //private static string longitude = "145.164834";
        private static HttpClient httpClient = new();

        public ClaimsVendorService(IICheckifyService checkifyService,
            UserManager<VendorApplicationUser> userManager,
            ApplicationDbContext context,
            IDashboardService dashboardService,
            IHttpClientService httpClientService,
            IClaimsService claimsService)
        {
            this.checkifyService = checkifyService;
            this.userManager = userManager;
            this._context = context;
            this.dashboardService = dashboardService;
            this.httpClientService = httpClientService;
            this.claimsService = claimsService;
        }

        public async Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

            var data = new DocumentData
            {
                Email = userEmail,
                ClaimId = claimId,
                OcrImage = Convert.ToBase64String(image),
                OcrLongLat = locationLongLat
            };
            var result = await checkifyService.GetDocumentId(data);
            return result;
        }

        public async Task<AppiCheckifyResponse> PostFaceId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";
            var data = new FaceData
            {
                Email = userEmail,
                ClaimId = claimId,
                LocationImage = Convert.ToBase64String(image),
                LocationLongLat = locationLongLat
            };
            var result = await checkifyService.GetFaceId(data);
            return result;
        }

        public async Task<ClaimsInvestigation> AllocateToVendorAgent(string userEmail, string selectedcase)
        {
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var claimsInvestigation = claimsService.GetClaims().FirstOrDefault(m => m.ClaimsInvestigationId == selectedcase && m.VendorId == vendorUser.VendorId);

            return claimsInvestigation;
        }

        public async Task<ClaimsInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, string selectedcase)
        {
            var claimsAllocate2Agent = claimsService.GetClaims().FirstOrDefault(v => v.ClaimsInvestigationId == selectedcase);

            var beneficiaryDetail = _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.BeneficiaryDetailId == claimsAllocate2Agent.BeneficiaryDetail.BeneficiaryDetailId);

            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENT.ToString()));

            var vendorUsers = _context.VendorApplicationUser
                .Include(u => u.District)
                .Include(u => u.State)
                .Include(u => u.Country)
                .Include(u => u.PinCode)
                .Where(u => u.VendorId == claimsAllocate2Agent.VendorId && u.Active);

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
                CaseLocation = beneficiaryDetail,
                ClaimsInvestigation = claimsAllocate2Agent,
                VendorUserClaims = agents
            };
            return model;
        }

        public async Task<ClaimsInvestigationVendorsModel> GetInvestigate(string userEmail, string selectedcase, bool uploaded = false)
        {
            var claim = claimsService.GetClaims()
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.DocumentIdReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);

            if(claim.AgencyReport == null || claim.AgencyReport.AgentEmail != userEmail &&
                claim.AgencyReport.DocumentIdReport?.DocumentIdImageLongLat == null &&
                claim.AgencyReport.DocumentIdReport?.DocumentIdImageLongLat == null)
            {
                claim.AgencyReport = new AgencyReport();
                claim.AgencyReport.AgentEmail = userEmail;

                var emptyModel = new ClaimsInvestigationVendorsModel { AgencyReport = claim.AgencyReport, Location = claim.BeneficiaryDetail, ClaimsInvestigation = claim };
                _context.ClaimsInvestigation.Update(claim);
                var rowsUpdayed = _context.SaveChanges();
                return emptyModel;
            }
            
            var model = new ClaimsInvestigationVendorsModel { AgencyReport = claim.AgencyReport, Location = claim.BeneficiaryDetail, ClaimsInvestigation = claim };
            _context.ClaimsInvestigation.Update(claim);
            var rows = _context.SaveChanges();
            return model;
        }

        public async Task<ClaimsInvestigationVendorsModel> GetInvestigateReport(string userEmail, string selectedcase)
        {
            var claimsInvestigation = claimsService.GetClaims()
                .Include(c => c.AgencyReport)
                .ThenInclude(c=>c.EnquiryRequest)
                .Include(c => c.AgencyReport.DigitalIdReport)
                .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.AgencyReport.DocumentIdReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);

            var beneficiaryDetails = _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.Country)
                .Include(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            
            if (claimsInvestigation.IsReviewCase)
            {
                claimsInvestigation.AgencyReport.SupervisorRemarks = null;
            }
            return (new ClaimsInvestigationVendorsModel { AgencyReport = claimsInvestigation.AgencyReport, Location = beneficiaryDetails, ClaimsInvestigation = claimsInvestigation });
        }

        public async Task<ClaimTransactionModel> GetClaimsDetails(string userEmail, string selectedcase)
        {
            var agencyUser = _context.VendorApplicationUser.FirstOrDefault(u=>u.Email == userEmail);

            var claimsInvestigation = claimsService.GetClaims()
                .Include(c=>c.Vendor)
                .Include(c=>c.AgencyReport)
                .ThenInclude(c=>c.EnquiryRequest)
                .Include(c=>c.AgencyReport.DigitalIdReport)
                .Include(c=>c.AgencyReport.DocumentIdReport)
                .Include(c=>c.AgencyReport.ReportQuestionaire)
                .FirstOrDefault(m => m.ClaimsInvestigationId == selectedcase);
            
            claimsInvestigation.AgencyDeclineComment = string.Empty;
            return new ClaimTransactionModel
            {
                ClaimsInvestigation = claimsInvestigation,
                Location = claimsInvestigation.BeneficiaryDetail,
                NotWithdrawable = claimsInvestigation.NotDeclinable,
            };
        }

        public async Task<List<VendorUserClaim>> GetAgentLoad(string userEmail)
        {
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENT.ToString()));
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

            var claimsCaseToAllocateToVendorAgent = claimsService.GetClaims().FirstOrDefault(v => v.ClaimsInvestigationId == selectedcase);

            var location = claimsCaseToAllocateToVendorAgent.BeneficiaryDetail;

            var claimsCaseLocation = _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.BeneficiaryDetailId == location.BeneficiaryDetailId);

            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENT.ToString()));

            var vendorUsers = _context.VendorApplicationUser
                .Include(u => u.District)
                .Include(u => u.State)
                .Include(u => u.Country)
                .Include(u => u.PinCode)
                .Where(u => u.VendorId == claimsCaseToAllocateToVendorAgent.VendorId && u.Active);

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

            claimsCaseToAllocateToVendorAgent.AgencyReport = null;

            _context.SaveChanges();

            return model;
        }
    }
}