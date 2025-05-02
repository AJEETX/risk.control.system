using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
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
                Options = "SOLE- OWNER, JOINT-OWNER, RENTED, UNKNOWN",
                IsRequired = true
            };
            var question2 = new Question
            {
                QuestionText = "Person Financial Status",
                QuestionType = "dropdown",
                Options = "Rs. 0 - 10000, Rs. 10000 - 100000, Rs. 100000 +, UNKNOWN",
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
                QuestionText = "Date and time met with Person",
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

            var template = new ReportTemplate
            {
                Name = InsuranceType.UNDERWRITING.GetEnumDisplayName() + " TEMPLATE",
                InsuranceType = InsuranceType.UNDERWRITING,
                ClientCompanyId = company.ClientCompanyId,
                Basetemplate = true,
                LocationTemplate = new List<LocationTemplate>
                {
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.VERIFIER_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportName = CONSTANTS.LOCATIONS.AGENT_PHOTO,                                          // You can set other properties of Agent here if needed
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                        },
                        FaceIds = new List<DigitalIdReport>
                        {
                            new DigitalIdReport
                            {
                                Selected = true,
                                ReportName = CONSTANTS.LOCATIONS.CUSTOMER_PHOTO,
                                Has2Face = false,
                                ReportType = DigitalIdReportType.SINGLE_FACE
                            },
                            new DigitalIdReport
                            {
                                ReportName = CONSTANTS.LOCATIONS.AGENT_CUSTOMER_PHOTO,
                                Has2Face = true,
                                ReportType = DigitalIdReportType.DUAL_FACE
                            },
                            new DigitalIdReport
                            {
                                Selected = true,
                                ReportName = CONSTANTS.LOCATIONS.BENEFICIARY_PHOTO,
                                Has2Face = false,
                                ReportType = DigitalIdReportType.SINGLE_FACE
                            },
                            new DigitalIdReport
                            {
                                ReportName = CONSTANTS.LOCATIONS.AGENT_BENEFICIARY_PHOTO,
                                Has2Face = true,
                                ReportType = DigitalIdReportType.DUAL_FACE
                            }
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                Selected = false,
                                ReportName = DocumentIdReportType.ADHAAR.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.ADHAAR
                            },
                            new DocumentIdReport
                            {
                                Selected = false,
                                ReportName = DocumentIdReportType.DRIVING_LICENSE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.DRIVING_LICENSE
                            },
                            new DocumentIdReport
                            {
                                Selected = true,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.PAN.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.PAN
                            },
                            new DocumentIdReport
                            {
                                Selected = false,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.BIRTH_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.BIRTH_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                Selected = false,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.MEDICAL_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.MEDICAL_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                Selected = false,
                                ReportName = DocumentIdReportType.VOTER_CARD.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.VOTER_CARD
                            }
                        },

                        Questions = new List<Question> {question1,question2,question3,question4 }
                    }
                }
            };

            // Add the new template to the database
            context.ReportTemplates.Add(template);

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
            var template = new ReportTemplate
            {
                Name = InsuranceType.CLAIM.GetEnumDisplayName() + " TEMPLATE",
                InsuranceType = InsuranceType.CLAIM,
                ClientCompanyId = company.ClientCompanyId,
                Basetemplate = true,
                LocationTemplate = new List<LocationTemplate>
                {
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.VERIFIER_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = CONSTANTS.LOCATIONS.AGENT_PHOTO                                              // You can set other properties of Agent here if needed
                        },
                        FaceIds = new List<DigitalIdReport>
                        {
                            new DigitalIdReport
                            {
                                ReportName = CONSTANTS.LOCATIONS.CUSTOMER_PHOTO,
                                ReportType = DigitalIdReportType.SINGLE_FACE
                            },
                            new DigitalIdReport
                            {
                                Selected = true,
                                ReportName = CONSTANTS.LOCATIONS.BENEFICIARY_PHOTO,
                                ReportType = DigitalIdReportType.SINGLE_FACE
                            }
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.DEATH_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.DEATH_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                ReportName = DocumentIdReportType.ADHAAR.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.ADHAAR
                            },
                            new DocumentIdReport
                            {
                                ReportName = DocumentIdReportType.DRIVING_LICENSE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.DRIVING_LICENSE
                            },
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.PAN.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.PAN
                            },
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.BIRTH_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.BIRTH_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.MEDICAL_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.MEDICAL_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                ReportName = DocumentIdReportType.VOTER_CARD.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.VOTER_CARD
                            }
                        },
                        Questions = new List<Question> { question1, question2, question3, question4 }
                    },
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.POLICE_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = CONSTANTS.LOCATIONS.AGENT_PHOTO                                            
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.POLICE_REPORT.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.POLICE_REPORT
                            }
                        },
                        Questions = new List<Question> 
                        { 
                            new Question 
                            {
                                QuestionText = "Cause of Death ?",
                                QuestionType = "text",
                                IsRequired = true 
                            },
                            new Question
                            {
                                QuestionText = "Name of Policeman met ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Was there any foul play ?",
                                QuestionType = "dropdown",
                                Options = "YES, NO",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Date time of Policeman met ?",
                                QuestionType = "date",
                                IsRequired = true
                            } 
                        }
                    },
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.HOSPITAL_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = CONSTANTS.LOCATIONS.AGENT_PHOTO                                            // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.MEDICAL_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.MEDICAL_CERTIFICATE
                            }
                        },
                        Questions = new List<Question>
                        {
                            new Question
                            {
                                QuestionText = "Nature of death ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Name of Medical staff met ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Was there any foul play ?",
                                QuestionType = "dropdown",
                                Options = "YES, NO",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Date time of Medical staff  met ?",
                                QuestionType = "date",
                                IsRequired = true
                            }
                        }
                    }
                }
            };

            // Add the new template to the database
            context.ReportTemplates.Add(template);
            context.CaseQuestionnaire.Add(caseQuestionnaire);
        }

    }
}