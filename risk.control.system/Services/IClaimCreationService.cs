﻿using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Company;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IClaimCreationService
    {
        Task<ClaimsInvestigation> CreatePolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument, IFormFile? customerDocument, bool create = true);

        Task<ClaimsInvestigation> EdiPolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument);

        Task<bool> CreateCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument);

        Task<bool> EditCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument);
        Task<bool> CreateBeneficiary(string userEmail, string ClaimsInvestigationId, BeneficiaryDetail beneficiary, IFormFile? customerDocument);
        Task<bool> EditBeneficiary(string userEmail, long beneficiaryDetailId, BeneficiaryDetail beneficiary, IFormFile? customerDocument);
    }
    public class ClaimCreationService : IClaimCreationService
    {
        private readonly ApplicationDbContext context;
        private readonly ICustomApiCLient customApiCLient;

        public ClaimCreationService(ApplicationDbContext context, ICustomApiCLient customApiCLient)
        {
            this.context = context;
            this.customApiCLient = customApiCLient;
        }
        public async Task<ClaimsInvestigation> CreatePolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument, IFormFile? customerDocument, bool create = true)
        {
            try
            {
                var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    claimsInvestigation.PolicyDetail.DocumentImage = dataStream.ToArray();
                }
                var initiatedStatus = context.InvestigationCaseStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED);
                var createdStatus = context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);

                var assigned2AssignerStatus = context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UserEmailActioned = userEmail;
                claimsInvestigation.UserEmailActionedTo = userEmail;
                claimsInvestigation.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.CurrentUserEmail = userEmail;
                claimsInvestigation.CurrentClaimOwner = currentUser.Email;
                claimsInvestigation.InvestigationCaseStatusId = initiatedStatus.InvestigationCaseStatusId;
                claimsInvestigation.InvestigationCaseSubStatusId = create ? createdStatus.InvestigationCaseSubStatusId : assigned2AssignerStatus.InvestigationCaseSubStatusId;
                claimsInvestigation.CreatorSla = currentUser.ClientCompany.CreatorSla;
                var aaddedClaimId = context.ClaimsInvestigation.Add(claimsInvestigation);
                var log = new InvestigationTransaction
                {
                    ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                    UserEmailActioned = userEmail,
                    UserEmailActionedTo = userEmail,
                    UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
                    CurrentClaimOwner = currentUser.Email,
                    HopCount = 0,
                    Time2Update = 0,
                    InvestigationCaseStatusId = initiatedStatus.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = create ? createdStatus.InvestigationCaseSubStatusId : assigned2AssignerStatus.InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail,
                };
                context.InvestigationTransaction.Add(log);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return claimsInvestigation;
        }

        public async Task<ClaimsInvestigation> EdiPolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument)
        {
            try
            {
                var existingPolicy = await context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                        .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId);
                existingPolicy.PolicyDetail.ContractIssueDate = claimsInvestigation.PolicyDetail.ContractIssueDate;
                existingPolicy.PolicyDetail.InvestigationServiceTypeId = claimsInvestigation.PolicyDetail.InvestigationServiceTypeId;
                existingPolicy.PolicyDetail.ClaimType = claimsInvestigation.PolicyDetail.ClaimType;
                existingPolicy.PolicyDetail.CostCentreId = claimsInvestigation.PolicyDetail.CostCentreId;
                existingPolicy.PolicyDetail.CaseEnablerId = claimsInvestigation.PolicyDetail.CaseEnablerId;
                existingPolicy.PolicyDetail.DateOfIncident = claimsInvestigation.PolicyDetail.DateOfIncident;
                existingPolicy.PolicyDetail.ContractNumber = claimsInvestigation.PolicyDetail.ContractNumber;
                existingPolicy.PolicyDetail.SumAssuredValue = claimsInvestigation.PolicyDetail.SumAssuredValue;
                existingPolicy.PolicyDetail.CauseOfLoss = claimsInvestigation.PolicyDetail.CauseOfLoss;
                existingPolicy.Updated = DateTime.Now;
                existingPolicy.UpdatedBy = userEmail;
                existingPolicy.CurrentUserEmail = userEmail;
                existingPolicy.CurrentClaimOwner = userEmail;

                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    existingPolicy.PolicyDetail.DocumentImage = dataStream.ToArray();
                }

                context.ClaimsInvestigation.Update(existingPolicy);

                await context.SaveChangesAsync();

                return existingPolicy;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> EditCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument)
        {
            try
            {
                // If a new document is provided, set the ProfilePicture
                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    await customerDocument.CopyToAsync(dataStream);
                    customerDetail.ProfilePicture = dataStream.ToArray();
                }
                else
                {
                    // Fetch existing customer to retain the existing ProfilePicture
                    var existingCustomer = await context.CustomerDetail
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == customerDetail.ClaimsInvestigationId);

                    if (existingCustomer == null)
                    {
                        Console.WriteLine("Customer not found");
                        return false;
                    }

                    customerDetail.ProfilePicture ??= existingCustomer.ProfilePicture;
                }

                // Update foreign key IDs
                customerDetail.CountryId = customerDetail.SelectedCountryId;
                customerDetail.StateId = customerDetail.SelectedStateId;
                customerDetail.DistrictId = customerDetail.SelectedDistrictId;
                customerDetail.PinCodeId = customerDetail.SelectedPincodeId;
                var pincode = context.PinCode
                        .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                        .FirstOrDefault(p => p.PinCodeId == customerDetail.PinCodeId);

                var address = customerDetail.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latLong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latLong.Latitude + "," + latLong.Longitude;
                customerDetail.Latitude = latLong.Latitude;
                customerDetail.Longitude = latLong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                customerDetail.CustomerLocationMap = url;

                // Attach the customerDetail object to the context and mark it as modified
                context.CustomerDetail.Attach(customerDetail);
                context.Entry(customerDetail).State = EntityState.Modified;

                // Save changes to the database
                return await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> CreateCustomer(string userEmail, CustomerDetail customerDetail, IFormFile? customerDocument)
        {
            try
            {
                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    customerDetail.ProfilePicture = dataStream.ToArray();
                }

                customerDetail.CountryId = customerDetail.SelectedCountryId;
                customerDetail.StateId = customerDetail.SelectedStateId;
                customerDetail.DistrictId = customerDetail.SelectedDistrictId;
                customerDetail.PinCodeId = customerDetail.SelectedPincodeId;

                var pincode = context.PinCode
                    .Include(p => p.District)
                    .Include(p => p.State)
                    .Include(p => p.Country)
                    .FirstOrDefault(p => p.PinCodeId == customerDetail.PinCodeId);

                var address = customerDetail.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latLong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latLong.Latitude + "," + latLong.Longitude;
                customerDetail.Latitude = latLong.Latitude;
                customerDetail.Longitude = latLong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                customerDetail.CustomerLocationMap = url;

                var addedClaim = context.CustomerDetail.Add(customerDetail);

                return await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> CreateBeneficiary(string userEmail, string ClaimsInvestigationId, BeneficiaryDetail beneficiary, IFormFile? customerDocument)
        {
            try
            {
                beneficiary.Updated = DateTime.Now;
                beneficiary.UpdatedBy = userEmail;

                if (customerDocument != null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    beneficiary.ProfilePicture = dataStream.ToArray();
                }

                beneficiary.CountryId = beneficiary.SelectedCountryId;
                beneficiary.StateId = beneficiary.SelectedStateId;
                beneficiary.DistrictId = beneficiary.SelectedDistrictId;
                beneficiary.PinCodeId = beneficiary.SelectedPincodeId;

                var pincode = context.PinCode
                    .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                    .FirstOrDefault(p => p.PinCodeId == beneficiary.PinCodeId);

                var address = beneficiary.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latlong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latlong.Latitude + "," + latlong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                beneficiary.BeneficiaryLocationMap = url;
                beneficiary.Latitude = latlong.Latitude;
                beneficiary.Longitude = latlong.Longitude;
                context.BeneficiaryDetail.Add(beneficiary);

                var claimsInvestigation = await context.ClaimsInvestigation
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == ClaimsInvestigationId);
                claimsInvestigation.IsReady2Assign = true;

                context.ClaimsInvestigation.Update(claimsInvestigation);
                return await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> EditBeneficiary(string userEmail, long beneficiaryDetailId, BeneficiaryDetail beneficiary, IFormFile? customerDocument)
        {
            try
            {
                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    beneficiary.ProfilePicture = dataStream.ToArray();
                }
                else
                {
                    var existingBeneficiary = context.BeneficiaryDetail.AsNoTracking().Where(c => c.BeneficiaryDetailId == beneficiaryDetailId).FirstOrDefault();
                    if (existingBeneficiary.ProfilePicture != null)
                    {
                        beneficiary.ProfilePicture = existingBeneficiary.ProfilePicture;
                    }
                }
                beneficiary.CountryId = beneficiary.SelectedCountryId;
                beneficiary.StateId = beneficiary.SelectedStateId;
                beneficiary.DistrictId = beneficiary.SelectedDistrictId;
                beneficiary.PinCodeId = beneficiary.SelectedPincodeId;

                var pincode = context.PinCode
                    .Include(p => p.District)
                        .Include(p => p.State)
                        .Include(p => p.Country)
                    .FirstOrDefault(p => p.PinCodeId == beneficiary.PinCodeId);

                var address = beneficiary.Addressline + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
                var latlong = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                var customerLatLong = latlong.Latitude + "," + latlong.Longitude;
                beneficiary.Latitude = latlong.Latitude;
                beneficiary.Longitude = latlong.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                beneficiary.BeneficiaryLocationMap = url;

                context.BeneficiaryDetail.Attach(beneficiary);
                context.Entry(beneficiary).State = EntityState.Modified;
                return (await context.SaveChangesAsync() > 0);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}