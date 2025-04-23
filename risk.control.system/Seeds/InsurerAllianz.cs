using System.ComponentModel.Design;
using System.Diagnostics.Metrics;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class InsurerAllianz
    {
        private const string vendorMapSize = "800x800";
        private const string companyMapSize = "800x800";
        public static async Task<ClientCompany> Seed(ApplicationDbContext context, List<Vendor> vendors, IWebHostEnvironment webHostEnvironment,
                    ICustomApiCLient customApiCLient, UserManager<ClientCompanyApplicationUser> clientUserManager, SeedInput input)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            var companyPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Country.Code.ToLower() == input.COUNTRY);

            var companyAddressline = "34 Lasiandra Avenue ";
            var companyAddress = companyAddressline + ", " + companyPinCode.District.Name + ", " + companyPinCode.State.Name + ", " + companyPinCode.Country.Code;
            var companyAddressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
            var companyAddressCoordinatesLatLong = companyAddressCoordinates.Latitude + "," + companyAddressCoordinates.Longitude;
            var companyAddressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={companyAddressCoordinatesLatLong}&zoom=14&size={companyMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyAddressCoordinatesLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            //CREATE COMPANY1
            string insurerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(input.PHOTO));
            var insurerImage = File.ReadAllBytes(insurerImagePath);

            if (insurerImage == null)
            {
                insurerImage = File.ReadAllBytes(noCompanyImagePath);
            }
            vendors = vendors.Where(v => v.CountryId == companyPinCode.CountryId).ToList();

            var insurer = new ClientCompany
            {
                Name = input.NAME,
                Addressline = companyAddressline,
                Branch = "FOREST HILL CHASE",
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "NAB",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                PinCode = companyPinCode,
                Country = companyPinCode.Country,
                CountryId = companyPinCode.CountryId,
                StateId = companyPinCode.StateId,
                DistrictId = companyPinCode.DistrictId,
                PinCodeId = companyPinCode.PinCodeId,
                //Description = "CORPORATE OFFICE ",
                Email = input.DOMAIN,
                DocumentUrl = input.PHOTO,
                DocumentImage = insurerImage,
                PhoneNumber = "9988004739",
                ExpiryDate = DateTime.Now.AddDays(5),
                EmpanelledVendors = vendors,
                Status = CompanyStatus.ACTIVE,
                AutoAllocation = globalSettings.AutoAllocation,
                BulkUpload = globalSettings.BulkUpload,
                Updated = DateTime.Now,
                Deleted = false,
                MobileAppUrl = globalSettings.MobileAppUrl,
                VerifyPan = globalSettings.VerifyPan,
                VerifyPassport = globalSettings.VerifyPassport,
                EnableMedia = globalSettings.EnableMedia,
                PanIdfyUrl = globalSettings.PanIdfyUrl,
                AiEnabled = globalSettings.AiEnabled,
                CanChangePassword = globalSettings.CanChangePassword,
                EnablePassport = globalSettings.EnablePassport,
                HasSampleData = globalSettings.HasSampleData,
                PassportApiHost = globalSettings.PassportApiHost,
                PassportApiKey = globalSettings.PassportApiKey,
                PassportApiUrl = globalSettings.PassportApiUrl,
                PanAPIHost = globalSettings.PanAPIHost,
                PanAPIKey = globalSettings.PanAPIKey,
                UpdateAgentAnswer = globalSettings.UpdateAgentAnswer,
                UpdateAgentReport = globalSettings.UpdateAgentReport,

                AddressMapLocation = companyAddressUrl,
                AddressLatitude = companyAddressCoordinates.Latitude,
                AddressLongitude = companyAddressCoordinates.Longitude
            };

            var insurerCompany = await context.ClientCompany.AddAsync(insurer);



            await context.SaveChangesAsync(null, false);

            var creator = await ClientApplicationUserSeed.Seed(context, webHostEnvironment, clientUserManager, insurerCompany.Entity);

            QuestionsCLAIM(context, insurer, creator);
            QuestionsUNDERWRITING(context, insurer, creator);
            await context.SaveChangesAsync(null, false);

            return insurerCompany.Entity;
        }

        private static void QuestionsUNDERWRITING(ApplicationDbContext context, ClientCompany company, ClientCompanyApplicationUser creator)
        {
            var question1 = new Question
            {
                QuestionText = "Ownership status of the home visited",
                QuestionType = "dropdown",
                Options = "SOLE- OWNED, OWNED-JOINTLY, RENTED, FANILY - OWNED",
                IsRequired = true
            };
            var question2 = new Question
            {
                QuestionText = "Neighbour Financial Status",
                QuestionType = "dropdown",
                Options = "Rs. 0 - 10,000, Rs. 10,000 - 1,00,000, Rs. 1,00,000 +",
                IsRequired = true
            };
            var question3 = new Question
            {
                QuestionText = "Name of the Person Met",
                QuestionType = "text",
                IsRequired = true
            };
            var question4 = new Question
            {
                QuestionText = "date and time met with Person",
                QuestionType = "date",
                IsRequired = true
            };

            var caseQuestionnaire = new CaseQuestionnaire
            {
                ClientCompanyId = company.ClientCompanyId,
                InsuranceType = InsuranceType.UNDERWRITING,
                CreatedUser = creator.Email,
                Questions = new List<Question> {question1, question2, question3,question4 }
            };

            context.CaseQuestionnaire.Add(caseQuestionnaire);
        }

        private static void QuestionsCLAIM(ApplicationDbContext context, ClientCompany company, ClientCompanyApplicationUser creator)
        {
            var question1 = new Question
            {
                QuestionText = "Injury/Illness prior to commencement/revival ?",
                QuestionType = "dropdown",
                Options = "YES, NO",
                IsRequired = true
            };
            var question2 = new Question
            {
                QuestionText = "Duration of treatment ?",
                QuestionType = "dropdown",
                Options = "0 , Less Than 6 months, More Than 6 months",
                IsRequired = true
            };
            var question3 = new Question
            {
                QuestionText = "Name of person met at the cemetery",
                QuestionType = "text",
                IsRequired = true
            };
            var question4 = new Question
            {
                QuestionText = "Date and time of death",
                QuestionType = "date",
                IsRequired = true
            };

            var caseQuestionnaire = new CaseQuestionnaire
            {
                ClientCompanyId = company.ClientCompanyId,
                InsuranceType = InsuranceType.CLAIM,
                CreatedUser = creator.Email,
                Questions = new List<Question> { question1, question2, question3, question4 }
            };

            context.CaseQuestionnaire.Add(caseQuestionnaire);
        }

    }
}