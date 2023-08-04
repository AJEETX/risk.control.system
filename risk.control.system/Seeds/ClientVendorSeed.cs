using Microsoft.EntityFrameworkCore.ChangeTracking;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public class ClientVendorSeed
    {
        public static async Task<(Vendor abcVendor, Vendor xyzVendor, Vendor xyz1Vendor, string clientCompanyId)> Seed(ApplicationDbContext context, EntityEntry<Country> indiaCountry, InvestigationServiceType investigationServiceType, LineOfBusiness lineOfBusiness)
        {
            //CREATE VENDOR COMPANY

            var abcVendor = new Vendor
            {
                Name = "Agency 1",
                Addressline = "1, Main Road  ",
                Branch = "MAHATTAN",
                Code = "VA001",
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "WESTPAC",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                Description = "HEAD OFFICE ",
                Email = "agency1.com",
                PhoneNumber = "8888004739",
                DocumentUrl = "/img/agency.png"
            };

            var abcVendorCompany = await context.Vendor.AddAsync(abcVendor);

            //CREATE CLIENT COMPANY
            var currentPinCode = "515631";
            var currentDistrict = "ANANTAPUR";
            var currentState = "AD";
            var TataAig = new ClientCompany
            {
                ClientCompanyId = Guid.NewGuid().ToString(),
                Name = "XYZ Insurance",
                Addressline = "100 GOOD STREET ",
                Branch = "FOREST HILL CHASE",
                Code = "TA001",
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "WESTPAC",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                Description = "CORPORATE OFFICE ",
                Email = "company.com",
                DocumentUrl = "/img/company.png",
                PhoneNumber = "9988004739",
                EmpanelledVendors = new List<Vendor> { abcVendor }
            };

            var tataAigCompany = await context.ClientCompany.AddAsync(TataAig);

            var xyzVendor = new Vendor
            {
                Name = "Agency 2",
                Addressline = "1, Main Road  ",
                Branch = "KANPUR",
                Code = "XY001",
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "SBI BANK",
                BankAccountNumber = "9876543",
                IFSCCode = "IFSC999",
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                Description = "HEAD OFFICE ",
                Email = "agency2.com",
                PhoneNumber = "4444404739",
                DocumentUrl = "/img/2.jpg"
            };

            var xyzVendorCompany = await context.Vendor.AddAsync(xyzVendor);

            var abcSericesWithPinCodes = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = abcVendorCompany.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 199,
                    StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(currentState))?.StateId ?? default!,
                    DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    CountryId = indiaCountry.Entity.CountryId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == currentPinCode)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == currentPinCode)?.Name ?? default !
                        }
                    }
                }
            };

            var listOfSericesWithPinCodes = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = xyzVendorCompany.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 299,
                    StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(currentState))?.StateId ?? default!,
                    DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                    CountryId = indiaCountry.Entity.CountryId,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == currentPinCode)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == currentPinCode)?.Name ?? default !
                        }
                    }
                }
            };

            abcVendor.VendorInvestigationServiceTypes = abcSericesWithPinCodes;
            xyzVendor.VendorInvestigationServiceTypes = listOfSericesWithPinCodes;

            var xyz1Vendor = new Vendor
            {
                Name = "Agency 3",
                Addressline = "1, Main Road  ",
                Branch = "KANPUR",
                Code = "XY100",
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "HDFC BANK",
                BankAccountNumber = "9876543",
                IFSCCode = "IFSC999",
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                Description = "HEAD OFFICE ",
                Email = "agency3.com",
                PhoneNumber = "7964404160",
                DocumentUrl = "/img/agency1.png"
            };

            var xyz1VendorCompany = await context.Vendor.AddAsync(xyz1Vendor);

            await context.SaveChangesAsync(null, false);
            return (abcVendor, xyzVendor, xyz1Vendor, tataAigCompany.Entity.ClientCompanyId);
        }
    }
}