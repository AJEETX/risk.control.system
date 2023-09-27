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
                Name = Applicationsettings.AGENCY1NAME,
                Addressline = "1, Nice Road  ",
                Branch = "MAHATTAN",
                Code = Applicationsettings.AGENCY1CODE,
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
                Email = Applicationsettings.AGENCY1DOMAIN,
                PhoneNumber = "8888004739",
                DocumentUrl = "/img/checker.png"
            };

            var abcVendorCompany = await context.Vendor.AddAsync(abcVendor);

            //CREATE CLIENT COMPANY
            var currentPinCode = "515631";
            var currentDistrict = "ANANTAPUR";
            var currentState = "AD";
            var TataAig = new ClientCompany
            {
                ClientCompanyId = Guid.NewGuid().ToString(),
                Name = Applicationsettings.COMPANYNAME,
                Addressline = "100 GOOD STREET ",
                Branch = "FOREST HILL CHASE",
                Code = Applicationsettings.COMPANYCODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "NAB",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                Description = "CORPORATE OFFICE ",
                Email = Applicationsettings.COMPANYDOMAIN,
                DocumentUrl = "/img/sbil.png",
                PhoneNumber = "9988004739",
                EmpanelledVendors = new List<Vendor> { abcVendor }
            };

            var tataAigCompany = await context.ClientCompany.AddAsync(TataAig);

            var xyzVendor = new Vendor
            {
                Name = Applicationsettings.AGENCY2NAME,
                Addressline = "10, Clear Road  ",
                Branch = "KANPUR",
                Code = Applicationsettings.AGENCY2CODE,
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
                Email = Applicationsettings.AGENCY2DOMAIN,
                PhoneNumber = "4444404739",
                DocumentUrl = "/img/verify.png"
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
                Name = Applicationsettings.AGENCY3NAME,
                Addressline = "1, Main Road  ",
                Branch = "KANPUR",
                Code = Applicationsettings.AGENCY3CODE,
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
                Email = Applicationsettings.AGENCY3DOMAIN,
                PhoneNumber = "7964404160",
                DocumentUrl = "/img/investigate.png"
            };

            var xyz1VendorCompany = await context.Vendor.AddAsync(xyz1Vendor);

            await context.SaveChangesAsync(null, false);
            return (abcVendor, xyzVendor, xyz1Vendor, tataAigCompany.Entity.ClientCompanyId);
        }
    }
}