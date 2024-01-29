using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public class ClientVendorSeed
    {
        public static async Task<(List<Vendor> vendors, List<ClientCompany> companyIds)> Seed(ApplicationDbContext context,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, LineOfBusiness lineOfBusiness)
        {
            //CREATE VENDOR COMPANY

            var checkerPinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
            var checkerDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == checkerPinCode.District.DistrictId);
            var checkerState = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == checkerDistrict.State.StateId);
            var checkerCountry = context.Country.FirstOrDefault(s => s.CountryId == checkerState.Country.CountryId) ?? default!;

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
                Country = checkerCountry,
                District = checkerDistrict,
                State = checkerState,
                PinCode = checkerPinCode,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY1DOMAIN,
                PhoneNumber = "8888004739",
                DocumentUrl = "/img/checker.png"
            };

            var checkerAgency = await context.Vendor.AddAsync(checker);

            var verifyPinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE3);
            var verifyDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == verifyPinCode.District.DistrictId);
            var verifyState = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == verifyDistrict.State.StateId);
            var verifyCountry = context.Country.FirstOrDefault(s => s.CountryId == verifyState.Country.CountryId) ?? default!;

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
                DocumentUrl = "/img/verify.png"
            };

            var verifyAgency = await context.Vendor.AddAsync(verify);

            var investigatePinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4);
            var investigateDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == investigatePinCode.District.DistrictId);
            var investigateState = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == investigateDistrict.State.StateId);
            var investigateCountry = context.Country.FirstOrDefault(s => s.CountryId == investigateState.Country.CountryId) ?? default!;

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
                DocumentUrl = "/img/investigate.png"
            };

            var investigateAgency = await context.Vendor.AddAsync(investigate);

            await context.SaveChangesAsync(null, false);

            var vendors = new List<Vendor> { checker, verify, investigate };

            var companyPinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE);
            var companyDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == companyPinCode.District.DistrictId);
            var companyState = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == companyDistrict.State.StateId);
            var country = context.Country.FirstOrDefault(s => s.CountryId == companyState.Country.CountryId) ?? default!;

            //CREATE COMPANY1

            var canara = new ClientCompany
            {
                Name = Applicationsettings.CANARA,
                Addressline = "34 Lasiandra Avenue ",
                Branch = "FOREST HILL CHASE",
                Code = Applicationsettings.CANARACODE,
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
                Email = Applicationsettings.CANARADOMAIN,
                DocumentUrl = Applicationsettings.CANARALOGO,
                PhoneNumber = "9988004739",
                EmpanelledVendors = new List<Vendor> { checker, verify, investigate }
            };

            var canaraCompany = await context.ClientCompany.AddAsync(canara);

            //CREATE COMPANY2

            var hdfc = new ClientCompany
            {
                Name = Applicationsettings.HDFC,
                Addressline = "34 Lasiandra Avenue ",
                Branch = "FOREST HILL CHASE",
                Code = Applicationsettings.HDFCCODE,
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
                Email = Applicationsettings.HDFCDOMAIN,
                DocumentUrl = Applicationsettings.HDFCLOGO,
                PhoneNumber = "9988004739",
                EmpanelledVendors = new List<Vendor> { checker, verify, investigate }
            };

            var hdfcCompany = await context.ClientCompany.AddAsync(hdfc);

            //CREATE COMPANY3

            var bajaj = new ClientCompany
            {
                Name = Applicationsettings.BAJAJ,
                Addressline = "34 Lasiandra Avenue ",
                Branch = "FOREST HILL CHASE",
                Code = Applicationsettings.BAJAJ_CODE,
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
                Email = Applicationsettings.BAJAJ_DOMAIN,
                DocumentUrl = Applicationsettings.BAJAJ_LOGO,
                PhoneNumber = "9988004739",
                EmpanelledVendors = new List<Vendor> { checker, verify, investigate }
            };

            var bajajCompany = await context.ClientCompany.AddAsync(bajaj);

            //CREATE COMPANY4
            var tata = new ClientCompany
            {
                Name = Applicationsettings.TATA,
                Addressline = "34 Lasiandra Avenue ",
                Branch = "FOREST HILL CHASE",
                Code = Applicationsettings.TATA_CODE,
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
                Email = Applicationsettings.TATA_DOMAIN,
                DocumentUrl = Applicationsettings.TATA_LOGO,
                PhoneNumber = "9988004739",
                EmpanelledVendors = new List<Vendor> { checker, verify, investigate }
            };

            var tataCompany = await context.ClientCompany.AddAsync(tata);

            var companyIds = new List<ClientCompany> { canaraCompany.Entity, hdfcCompany.Entity, bajajCompany.Entity, tataCompany.Entity };

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
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Name ?? default !
                        }
                    }
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
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Name ?? default !
                        }
                    }
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
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.Name ?? default !
                        }
                    }
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
                    }
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
                    }
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
                    }
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
                    }
                }
            };

            checker.VendorInvestigationServiceTypes = checkerServices;
            verify.VendorInvestigationServiceTypes = verifyServices;
            investigate.VendorInvestigationServiceTypes = investigateServices;

            checker.Clients.Add(canaraCompany.Entity);
            verify.Clients.Add(canaraCompany.Entity);
            investigate.Clients.Add(canaraCompany.Entity);

            checker.Clients.Add(hdfcCompany.Entity);
            verify.Clients.Add(hdfcCompany.Entity);
            investigate.Clients.Add(hdfcCompany.Entity);

            checker.Clients.Add(bajajCompany.Entity);
            verify.Clients.Add(bajajCompany.Entity);
            investigate.Clients.Add(bajajCompany.Entity);

            checker.Clients.Add(tataCompany.Entity);
            verify.Clients.Add(tataCompany.Entity);
            investigate.Clients.Add(tataCompany.Entity);

            await context.SaveChangesAsync(null, false);
            return (vendors, companyIds);
        }
    }
}