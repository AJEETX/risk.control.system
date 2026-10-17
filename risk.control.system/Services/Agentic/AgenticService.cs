using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agentic
{
    public interface IAgenticService
    {
        Task<string> GetAgenticUrl(string userEmail, string agenticBaseUrl, string agenticUrl);
        Task<bool> StartAiProcessAsync(string policyNumber);
        Task<(bool, string)> FaceExistsAsync(IFormFile image);

        Task<bool> CaseAdjudicatedAsync(AdjudicationRequest request);
        //byte[] ConvertImageToSearchablePdfBytes(string inputImagePath);
    }
    internal class AgenticService(
        ApplicationDbContext dbContext,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IAmazonApiService amazonApiService,
        ISmsService smsService,
        IHttpClientFactory httpClientFactory,
        ILogger<AgenticService> logger) : IAgenticService
    {
        //private static string tessDataPath = @"./tessdata"; // Path to your tessdata folder
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ISmsService _smsService = smsService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<AgenticService> _logger = logger;

        public async Task<bool> CaseAdjudicatedAsync(AdjudicationRequest request)
        {
            var caseTask = await _dbContext.Investigations.Include(c => c.PolicyDetail).FirstOrDefaultAsync(i => !i.Deleted && i.AiEnabled && i.PolicyDetail!.ContractNumber == request.PolicyNumber.Trim());
            if (caseTask == null)
            {
                throw new InvalidOperationException("Investigation task not found.");
            }
            caseTask.AdjudicationCompleted = request.Set;
            _dbContext.Investigations.Update(caseTask);
            var result = await _dbContext.SaveChangesAsync() > 0;
            if (result)
            {
                var companyUsers = _dbContext.ApplicationUser.Include(a => a.Country).Where(u => u.ClientCompanyId == caseTask.ClientCompanyId);
                var assessorRole = await _roleManager.FindByNameAsync(AGENT.DISPLAY_NAME);
                foreach (var companyUser in companyUsers)
                {
                    var isAssessor = await _userManager.IsInRoleAsync(companyUser, assessorRole!.Name!);

                    if (isAssessor)
                    {
                        var message = $"Dear {companyUser.Email}\n";
                        message += request.Set ? $"Policy # {caseTask.PolicyDetail!.ContractNumber} has been adjudicated.\n" : $"Policy # {caseTask.PolicyDetail!.ContractNumber} has been un-adjudicated.\n";
                        message += request.Set ? "Please process the case.\n" : "";
                        message += "Thanks \n";
                        await _smsService.SendSmsAsync(companyUser.Country!.Code, companyUser.Country!.ISDCode.ToString() + companyUser.PhoneNumber, message);
                    }
                }
            }

            return result;
        }

        public async Task<(bool, string)> FaceExistsAsync(IFormFile image)
        {
            var imageCollection = EnvHelper.Get(CONSTANTS.FaceImageCollection);

            await _amazonApiService.EnsureCollectionExistsAsync(imageCollection!);

            var memoryStream = new MemoryStream();

            await image.CopyToAsync(memoryStream);

            var searchRequest = new SearchFacesByImageRequest
            {
                CollectionId = imageCollection!,
                Image = new Image { Bytes = memoryStream },
                MaxFaces = 1,
                FaceMatchThreshold = 90F
            };

            var response = await _amazonApiService.SearchFacesByImageAsync(searchRequest);
            if (response.FaceMatches.Count > 0)
            {
                return (true, "Face match found.");
            }
            var indexRequest = new IndexFacesRequest
            {
                CollectionId = imageCollection!,
                Image = new Image { Bytes = memoryStream },
                ExternalImageId = Guid.NewGuid().ToString(),
                MaxFaces = 1,
                QualityFilter = QualityFilter.AUTO
            };
            var faceAddResponse = await _amazonApiService.IndexFacesAsync(indexRequest);
            var faceId = faceAddResponse.FaceRecords.FirstOrDefault()?.Face.FaceId;
            if (faceId != null)
            {
                return (false, "Face added to collection.");
            }
            return (true, "Error occurred while processing the image.");
        }

        public async Task<string> GetAgenticUrl(string userEmail, string agenticBaseUrl, string agenticUrl)
        {
            var _httpClient = _httpClientFactory.CreateClient();
            var loginData = new LoginRequest(Applicationsettings.AI_LOGIN, Applicationsettings.AI_DATA);

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{agenticBaseUrl}/v1/auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<AiResponse>();

                    var token = loginResponse?.Token;
                    var agenticFullUrl = agenticUrl + "&email=" + userEmail + "#token=" + token;
                    return agenticFullUrl!;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while attempting to process AI adjudication.");
                throw;
            }
        }
        public async Task<bool> StartAiProcessAsync(string policyNumber)
        {
            var _httpClient = _httpClientFactory.CreateClient();
            var loginData = new LoginRequest(Applicationsettings.AI_LOGIN, Applicationsettings.AI_DATA);

            try
            {
                var caseTask = await _dbContext.Investigations.Include(c => c.PolicyDetail).Include(c => c.ClientCompany).FirstOrDefaultAsync(i => !i.Deleted && i.AiEnabled && i.PolicyDetail!.ContractNumber == policyNumber);
                var agenticBaseUrl = caseTask?.ClientCompany?.AgenticBaseUrl;

                var response = await _httpClient.PostAsJsonAsync($"{agenticBaseUrl}/v1/auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<AiResponse>();

                    var token = loginResponse?.Token;

                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var reviewType = caseTask?.PolicyDetail?.InsuranceType == InsuranceType.CLAIM ? "claim_review" : "underwriting_review";
                    //TESTING PURPOSES
                    //reviewType = "claim_review";
                    var requestData = new StartAdjudicationRequest(reviewType, "policyintel");
                    var finalResponse = await _httpClient.PostAsJsonAsync($"{agenticBaseUrl}/v1/policies/{caseTask!.PolicyDetail!.ContractNumber}/adjudicate", requestData);
                    if (finalResponse.IsSuccessStatusCode)
                    {
                        var adjudicationResponse = await finalResponse.Content.ReadFromJsonAsync<AdjudicationResponse>();
                        var agenticUrl = adjudicationResponse?.Progress_url.Replace("{policyNumber}", caseTask?.PolicyDetail?.ContractNumber!);
                        caseTask!.AgenticUrl = agenticUrl!;
                        _dbContext.Investigations.Update(caseTask);
                        var result = await _dbContext.SaveChangesAsync();
                        return result > 0;
                    }
                    else
                    {
                        var errorContent = await finalResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Failed to start AI adjudication for policy {PolicyNumber}. Status Code: {StatusCode}, Response: {Response}", policyNumber, finalResponse.StatusCode, errorContent);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to authenticate with AI service for policy {PolicyNumber}. Status Code: {StatusCode}, Response: {Response}", policyNumber, response.StatusCode, errorContent);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while attempting to process AI adjudication.");
                throw;
            }
        }


        //public byte[] ConvertImageToSearchablePdfBytes(string inputImagePath)
        //{
        //    string tempPathWithoutExtension = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        //    string expectedPhysicalPdfPath = tempPathWithoutExtension + ".pdf";

        //    try
        //    {
        //        using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
        //        {
        //            // 2. Create the PDF renderer targeting our temporary file path location
        //            using (var renderer = ResultRenderer.CreatePdfRenderer(tempPathWithoutExtension, tessDataPath, false))
        //            {
        //                using (renderer.BeginDocument("Extracted Document"))
        //                {
        //                    using (var img = Pix.LoadFromFile(inputImagePath))
        //                    {
        //                        using (var page = engine.Process(img))
        //                        {
        //                            bool success = renderer.AddPage(page);
        //                            if (!success)
        //                            {
        //                                throw new Exception("Tesseract failed to layout the OCR map to the renderer.");
        //                            }
        //                        }
        //                    }
        //                } // renderer.EndDocument() is explicitly executed here on disposal
        //            }
        //        }

        //        if (File.Exists(expectedPhysicalPdfPath))
        //        {
        //            byte[] pdfBytes = File.ReadAllBytes(expectedPhysicalPdfPath);
        //            return pdfBytes;
        //        }

        //        throw new FileNotFoundException("The OCR output PDF file was not generated by Tesseract.");
        //    }
        //    finally
        //    {
        //        if (File.Exists(expectedPhysicalPdfPath))
        //        {
        //            try
        //            {
        //                File.Delete(expectedPhysicalPdfPath);
        //            }
        //            catch
        //            {
        //                // Log or suppress cleanup error so it doesn't break the return flow
        //            }
        //        }
        //    }
        //}
    }
}
