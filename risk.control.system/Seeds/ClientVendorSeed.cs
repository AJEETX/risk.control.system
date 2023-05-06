using Microsoft.EntityFrameworkCore.ChangeTracking;
using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public class ClientVendorSeed
    {
        public static async Task<string> Seed(ApplicationDbContext context, EntityEntry<Country> indiaCountry, InvestigationServiceType investigationServiceType, LineOfBusiness lineOfBusiness)
        {
            //CREATE CLIENT COMPANY
            var currentPinCode = "515631";
            var currentDistrict = "ANANTAPUR";
            var currentState = "AD";
             var TataAig = new ClientCompany
            {
                ClientCompanyId = Guid.NewGuid().ToString(),
                Name = "TATA AIG INSURANCE",
                Addressline = "100 GOOD STREET ",
                Branch = "FOREST HILL CHASE",
                City = "FOREST HILL",
                Code = "TA001",
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                Description = "CORPORATE OFFICE ",
                Email = "tata-aig@mail.com",
                PhoneNumber = "(03) 88004739",
            };

            var tataAigCompany = await context.ClientCompany.AddAsync(TataAig);

            //CREATE VENDOR COMPANY

            var listOfSericesWithPinCodes = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 99,
                    StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(currentState))?.StateId ?? default!,
                    DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
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

            var abcVendor = new Vendor
            {
                Name = "abc investigation agency",
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
                Email = "abc@vendor.com",
                PhoneNumber = "(04) 123 234",
                VendorInvestigationServiceTypes = listOfSericesWithPinCodes
            };

            var abcVendorCompany = await context.Vendor.AddAsync(abcVendor);


            await context.SaveChangesAsync();
            return abcVendorCompany.Entity.VendorId;
        }
    }
}
