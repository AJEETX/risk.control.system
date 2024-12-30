using System.Diagnostics.Metrics;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public class VendorSeed
    {
        public static async Task<(List<Vendor> vendors, List<ClientCompany> companyIds)> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, LineOfBusiness lineOfBusiness)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSetting = new GlobalSettings
            {
                EnableMailbox = true
            };
            var newGlobalSetting = await context.GlobalSettings.AddAsync(globalSetting);
            await context.SaveChangesAsync(null, false);
            var globalSettings = context.GlobalSettings.FirstOrDefault();

            var enableMailbox = globalSettings?.EnableMailbox ?? false;
            //CREATE VENDOR COMPANY

            var checkerPinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
            string checkerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.AGENCY1PHOTO));
            var checkerImage = File.ReadAllBytes(checkerImagePath);

            if (checkerImage == null)
            {
                checkerImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var checker = new Vendor
            {
                Name = Applicationsettings.AGENCY1NAME,
                Addressline = "1, Nice Road  ",
                Branch = "MAHATTAN",
                Code = Applicationsettings.AGENCY1CODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "WESTPAC",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                CountryId = checkerPinCode.CountryId,
                DistrictId = checkerPinCode.DistrictId,
                StateId = checkerPinCode.StateId,
                PinCodeId = checkerPinCode.PinCodeId,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY1DOMAIN,
                PhoneNumber = "8888004739",
                DocumentUrl = Applicationsettings.AGENCY1PHOTO,
                DocumentImage = checkerImage,
                Updated = DateTime.Now,
                Status = VendorStatus.ACTIVE,
                EnableMailbox = enableMailbox
            };

            var checkerAgency = await context.Vendor.AddAsync(checker);

            var verifyPinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE3);
            var verifyDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == verifyPinCode.District.DistrictId);
            var verifyState = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == verifyDistrict.State.StateId);
            var verifyCountry = context.Country.FirstOrDefault(s => s.CountryId == verifyState.Country.CountryId) ?? default!;
            string verifyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.AGENCY2PHOTO));
            var verifyImage = File.ReadAllBytes(verifyImagePath);

            if (verifyImage == null)
            {
                verifyImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var verify = new Vendor
            {
                Name = Applicationsettings.AGENCY2NAME,
                Addressline = "10, Clear Road  ",
                Branch = "BLACKBURN",
                Code = Applicationsettings.AGENCY2CODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "SBI BANK",
                BankAccountNumber = "9876543",
                IFSCCode = "IFSC999",
                Country = verifyCountry,
                District = verifyDistrict,
                State = verifyState,
                PinCode = verifyPinCode,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY2DOMAIN,
                PhoneNumber = "4444404739",
                DocumentUrl = Applicationsettings.AGENCY2PHOTO,
                DocumentImage = verifyImage,
                Status = VendorStatus.ACTIVE,
                Updated = DateTime.Now,
                EnableMailbox = enableMailbox
            };

            var verifyAgency = await context.Vendor.AddAsync(verify);

            var investigatePinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4);
            var investigateDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == investigatePinCode.District.DistrictId);
            var investigateState = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == investigateDistrict.State.StateId);
            var investigateCountry = context.Country.FirstOrDefault(s => s.CountryId == investigateState.Country.CountryId) ?? default!;
            string investigateImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.AGENCY3PHOTO));
            var investigateImage = File.ReadAllBytes(investigateImagePath);

            if (investigateImage == null)
            {
                investigateImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var investigate = new Vendor
            {
                Name = Applicationsettings.AGENCY3NAME,
                Addressline = "1, Main Road  ",
                Branch = "CLAYTON ROAD",
                Code = Applicationsettings.AGENCY3CODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "HDFC BANK",
                BankAccountNumber = "9876543",
                IFSCCode = "IFSC999",
                Country = investigateCountry,
                District = investigateDistrict,
                State = investigateState,
                PinCode = investigatePinCode,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY3DOMAIN,
                PhoneNumber = "7964404160",
                DocumentUrl = Applicationsettings.AGENCY3PHOTO,
                DocumentImage = investigateImage,
                Status = VendorStatus.ACTIVE,
                Updated = DateTime.Now,
                EnableMailbox = enableMailbox
            };

            var investigateAgency = await context.Vendor.AddAsync(investigate);

            await context.SaveChangesAsync(null, false);

            var vendors = new List<Vendor> { checker, verify, investigate };

            var companyPinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE);
            var companyDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == companyPinCode.District.DistrictId);
            var companyState = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == companyDistrict.State.StateId);
            var country = context.Country.FirstOrDefault(s => s.CountryId == companyState.Country.CountryId) ?? default!;

            //CREATE COMPANY1
            string insurerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.INSURERLOGO));
            var insurerImage = File.ReadAllBytes(insurerImagePath);

            if (insurerImage == null)
            {
                insurerImage = File.ReadAllBytes(noCompanyImagePath);
            }
            var insurer = new ClientCompany
            {
                Name = Applicationsettings.INSURER,
                Addressline = "34 Lasiandra Avenue ",
                Branch = "FOREST HILL CHASE",
                Code = Applicationsettings.INSURERCODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "NAB",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                Country = country,
                District = companyDistrict,
                State = companyState,
                PinCode = companyPinCode,
                Description = "CORPORATE OFFICE ",
                Email = Applicationsettings.INSURERDOMAIN,
                DocumentUrl = Applicationsettings.INSURERLOGO,
                DocumentImage = insurerImage,
                PhoneNumber = "9988004739",
                ExpiryDate = DateTime.Now.AddDays(5),
                EmpanelledVendors = new List<Vendor> { checker, verify, investigate },
                Status = CompanyStatus.ACTIVE,
                 AutoAllocation = true,
                 BulkUpload = true,
                Updated = DateTime.Now,
                Deleted = false,
                EnableMailbox = enableMailbox
            };

            var insurerCompany = await context.ClientCompany.AddAsync(insurer);

            var companyIds = new List<ClientCompany> { insurerCompany.Entity
                //, hdfcCompany.Entity, bajajCompany.Entity, tataCompany.Entity
            };

            var checkerServices = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = checkerAgency.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 199,
                    District = companyDistrict,
                    State = companyState,
                    LineOfBusiness = lineOfBusiness,
                    Country = country,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = checkerAgency.Entity.VendorId,
                    InvestigationServiceTypeId = docServiceType.InvestigationServiceTypeId,
                    Price = 99,
                    District = companyDistrict,
                    State = companyState,
                    LineOfBusiness = lineOfBusiness,
                    Country = country,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                }
            };

            var verifyServices = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = verifyAgency.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 399,
                    District = companyDistrict,
                    State = companyState,
                    Country = country,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = verifyAgency.Entity.VendorId,
                    InvestigationServiceTypeId = discreetServiceType.InvestigationServiceTypeId,
                    Price = 299,
                    District = companyDistrict,
                    State = companyState,
                    Country = country,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                }
            };

            var investigateServices = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = investigateAgency.Entity.VendorId,
                    InvestigationServiceTypeId = docServiceType.InvestigationServiceTypeId,
                    Price = 199,
                    District = companyDistrict,
                    State = companyState,
                    Country = country,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = investigateAgency.Entity.VendorId,
                    InvestigationServiceTypeId = discreetServiceType.InvestigationServiceTypeId,
                    Price = 299,
                    District = companyDistrict,
                    State = companyState,
                    Country = country,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = investigateAgency.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 599,
                    District = companyDistrict,
                    State = companyState,
                    Country = country,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                }
            };

            checker.VendorInvestigationServiceTypes = checkerServices;
            verify.VendorInvestigationServiceTypes = verifyServices;
            investigate.VendorInvestigationServiceTypes = investigateServices;

            checker.Clients.Add(insurerCompany.Entity);
            verify.Clients.Add(insurerCompany.Entity);
            investigate.Clients.Add(insurerCompany.Entity);

            await context.SaveChangesAsync(null, false);
            return (vendors, companyIds);
        }
    }
}