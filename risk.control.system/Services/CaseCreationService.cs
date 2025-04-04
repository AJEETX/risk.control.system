using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface ICaseCreationService
    {
        Task<bool> PerformUpload(ClientCompanyApplicationUser companyUser, string row, CREATEDBY autoOrManual, ORIGIN fileOrFtp, long lineOfBusinessId, byte[] data, DataTable dt);
    }
    public class CaseCreationService : ICaseCreationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomApiCLient customApiCLient;
        private readonly Regex regex = new Regex("\"(.*?)\"");
        private const string NO_DATA = "NO DATA";
        public CaseCreationService(ApplicationDbContext context, ICustomApiCLient customApiCLient)
        {
            _context = context;
            this.customApiCLient = customApiCLient;
        }

        public async Task<bool> PerformUpload(ClientCompanyApplicationUser companyUser, string row, CREATEDBY autoOrManual, ORIGIN fileOrFtp, long lineOfBusinessId, byte[] data, DataTable dt)
        {
            try
            {
                var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
                dt.Rows.Add();
                int i = 0;
                var output = regex.Replace(row, m => m.Value.Replace(',', '@'));
                var rowData = output.Split(',').ToList();
                foreach (string cell in rowData)
                {
                    dt.Rows[dt.Rows.Count - 1][i] = cell?.Trim() ?? NO_DATA;
                    i++;
                }

                var claimAdded = await CreateNewPolicy(rowData, companyUser, autoOrManual, fileOrFtp, lineOfBusinessId, data);

                if (!claimAdded)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        private async Task<bool> CreateNewPolicy(List<string> rowData, ClientCompanyApplicationUser companyUser, CREATEDBY autoOrManual, ORIGIN fileOrFtp, long lineOfBusinessId, byte[] data)
        {
            //CREATE CLAIM
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var autoEnabled = companyUser.ClientCompany.AutoAllocation;
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var subStatus = companyUser.ClientCompany.AutoAllocation && autoOrManual == CREATEDBY.AUTO ? createdStatus : assignedStatus;
            var claim = new ClaimsInvestigation
            {
                InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                InvestigationCaseStatus = status,
                InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                InvestigationCaseSubStatus = subStatus,
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email,
                CurrentUserEmail = companyUser.Email,
                CurrentClaimOwner = companyUser.Email,
                Deleted = false,
                HasClientCompany = true,
                AssignedToAgency = false,
                IsReady2Assign = true,
                IsReviewCase = false,
                UserEmailActioned = companyUser.Email,
                UserEmailActionedTo = companyUser.Email,
                CREATEDBY = autoOrManual,
                ORIGIN = fileOrFtp,
                ClientCompanyId = companyUser.ClientCompanyId,
                UserRoleActionedTo = $"{companyUser.ClientCompany.Email}",
                CreatorSla = companyUser.ClientCompany.CreatorSla
            };

            //CREATE POLICY
            var servicetype = _context.InvestigationServiceType.FirstOrDefault(s => s.Code.ToLower() == (rowData[4].Trim().ToLower()));
            var imagesWithData = GetImagesWithDataInSubfolder(data, rowData[0]?.Trim().ToLower());

            var savedNewImage = imagesWithData.FirstOrDefault(s=>s.FileName.ToLower().EndsWith("policy.jpg"));
            //+ "/policy.jpg"
            claim.PolicyDetail = new PolicyDetail
            {
                ContractNumber = rowData[0]?.Trim(),
                SumAssuredValue = Convert.ToDecimal(rowData[1]?.Trim()),
                ContractIssueDate = DateTime.ParseExact(rowData[2]?.Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture),
                ClaimType = (ClaimType)Enum.Parse(typeof(ClaimType), rowData[3]?.Trim()),
                InvestigationServiceTypeId = servicetype?.InvestigationServiceTypeId,
                DateOfIncident = DateTime.ParseExact(rowData[5]?.Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture),
                CauseOfLoss = rowData[6]?.Trim(),
                CaseEnablerId = _context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == rowData[7].Trim().ToLower()).CaseEnablerId,
                CostCentreId = _context.CostCentre.FirstOrDefault(c => c.Code.ToLower() == rowData[8].Trim().ToLower()).CostCentreId,
                LineOfBusinessId = lineOfBusinessId,
                DocumentImage = savedNewImage.ImageData,
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email
            };
            var customerTask = CreateNewCustomer(companyUser, rowData, data);
            var beneficiaryTask =  CreateNewBeneficiary(companyUser, rowData, data);
            await Task.WhenAll(customerTask, beneficiaryTask);

            // Get the results
            var customer = await customerTask;
            var beneficiary = await beneficiaryTask;
            if (customer is null || beneficiary is null)
            {
                return false;
            }
            claim.CustomerDetail = customer;
            claim.BeneficiaryDetail = beneficiary;
            _context.ClaimsInvestigation.Add(claim);
            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claim.ClaimsInvestigationId,
                UserEmailActioned = claim.UserEmailActioned,
                UserRoleActionedTo = claim.UserRoleActionedTo,
                CurrentClaimOwner = companyUser.Email,
                HopCount = 0,
                Time2Update = 0,
                InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId,
                UpdatedBy = companyUser.Email
            };

            _context.InvestigationTransaction.Add(log);
            return true;
        }
        private async Task<CustomerDetail> CreateNewCustomer(ClientCompanyApplicationUser companyUser, List<string> rowData, byte[] data)
        {
            var pinCode = _context.PinCode
                                    .Include(p => p.District)
                                    .Include(p => p.State)
                                    .Include(p => p.Country)
                                    .FirstOrDefault(p => p.Code == rowData[19].Trim());
            if (pinCode.CountryId != companyUser.ClientCompany.CountryId)
            {
                return null;
            }


            var imagesWithData = GetImagesWithDataInSubfolder(data, rowData[0]?.Trim().ToLower());
            var customerNewImage = imagesWithData.FirstOrDefault(s => s.FileName.ToLower().EndsWith("customer.jpg"));
            
            var customerDetail = new CustomerDetail
            {
                Name = rowData[10]?.Trim(),
                //CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), rowData[11]?.Trim()),
                Gender = (Gender)Enum.Parse(typeof(Gender), rowData[12]?.Trim()),
                DateOfBirth = DateTime.ParseExact(rowData[13]?.Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture),
                ContactNumber = (rowData[14]?.Trim()),
                Education = (Education)Enum.Parse(typeof(Education), rowData[15]?.Trim()),
                Occupation = (Occupation)Enum.Parse(typeof(Occupation), rowData[16]?.Trim()),
                Income = (Income)Enum.Parse(typeof(Income), rowData[17]?.Trim()),
                Addressline = rowData[18]?.Trim(),
                CountryId = pinCode.CountryId,
                PinCodeId = pinCode.PinCodeId,
                StateId = pinCode.StateId,
                DistrictId = pinCode.DistrictId,
                //Description = rowData[20]?.Trim(),
                ProfilePicture = customerNewImage.ImageData,
                UpdatedBy = companyUser.Email,
                Updated = DateTime.Now
            };
            var address = customerDetail.Addressline + ", " +
                pinCode.District.Name + ", " +
                pinCode.State.Name + ", " +
                pinCode.Country.Code + ", " +
                pinCode.Code;

            var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);
            customerDetail.Latitude = coordinates.Latitude;
            customerDetail.Longitude = coordinates.Longitude;
            var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            customerDetail.CustomerLocationMap = url;
            return customerDetail;
        }

        private async Task<BeneficiaryDetail> CreateNewBeneficiary(ClientCompanyApplicationUser companyUser, List<string> rowData, byte[] data)
        {
            var pinCode = _context.PinCode
                                    .Include(p => p.District)
                                    .Include(p => p.State)
                                    .Include(p => p.Country)
                                    .FirstOrDefault(p => p.Code == rowData[28].Trim());
            if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
            {
                return null;
            }
            var relation = _context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == rowData[23].Trim().ToLower());

            var imagesWithData = GetImagesWithDataInSubfolder(data, rowData[0]?.Trim().ToLower());
            var beneficiaryNewImage = imagesWithData.FirstOrDefault(s => s.FileName.ToLower().EndsWith("beneficiary.jpg"));

            var beneficairy = new BeneficiaryDetail
            {
                Name = rowData[22]?.Trim(),
                BeneficiaryRelationId = relation.BeneficiaryRelationId,
                DateOfBirth = DateTime.ParseExact(rowData[24]?.Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture),
                Income = (Income)Enum.Parse(typeof(Income), rowData[25]?.Trim()),
                ContactNumber = (rowData[26]?.Trim()),
                Addressline = rowData[27]?.Trim(),
                PinCodeId = pinCode.PinCodeId,
                DistrictId = pinCode.District.DistrictId,
                StateId = pinCode.State.StateId,
                CountryId = pinCode.Country.CountryId,
                ProfilePicture = beneficiaryNewImage.ImageData,
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email
            };

            var address = beneficairy.Addressline + ", " +
                pinCode.District.Name + ", " +
                pinCode.State.Name + ", " +
                pinCode.Country.Code + ", " +
                pinCode.Code;

            var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);

            var beneLatLong = coordinates.Latitude + "," + coordinates.Longitude;
            beneficairy.Latitude = coordinates.Latitude;
            beneficairy.Longitude = coordinates.Longitude;

            var beneUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{beneLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            beneficairy.BeneficiaryLocationMap = beneUrl;
            _context.BeneficiaryDetail.Add(beneficairy);
            return beneficairy;
        }

        public static List<(string FileName, byte[] ImageData)> GetImagesWithDataInSubfolder(byte[] zipData, string subfolderName)
        {
            List<(string FileName, byte[] ImageData)> images = new List<(string, byte[])>();

            using (MemoryStream zipStream = new MemoryStream(zipData))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // Loop through each entry in the archive
                foreach (var entry in archive.Entries)
                {
                    // Convert path to standard format (Windows)
                    string folderPath = entry.FullName.Replace("/", "\\");

                    // Check if the entry is inside the desired subfolder and is an image file
                    if (folderPath.ToLower().Contains("\\" + subfolderName + "\\") && IsImageFile(entry.FullName))
                    {
                        // Extract image data
                        using (MemoryStream imageStream = new MemoryStream())
                        {
                            using (Stream entryStream = entry.Open())
                            {
                                entryStream.CopyTo(imageStream);
                            }

                            // Add file name and byte array to the result list
                            images.Add((entry.Name, imageStream.ToArray()));
                        }
                    }
                }
            }

            return images;
        }

        private static bool IsImageFile(string filePath)
        {
            // Check if the file is an image based on file extension
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            string extension = Path.GetExtension(filePath)?.ToLower();
            return imageExtensions.Contains(extension);
        }


    }
}
