using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IUploadService
    {
        Task<bool> DoUpload(ClientCompanyApplicationUser companyUser, string[] dataRows, CREATEDBY autoOrManual, ZipArchive archive, ORIGIN fileOrFtp, long lineOfBusinessId);
        Task<List<ClaimsInvestigation>> PerformCustomUpload(ClientCompanyApplicationUser companyUser, List<UploadCase> customData, FileOnFileSystemModel model);
    }
    public class UploadService : IUploadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomApiCLient customApiCLient;
        private readonly IProgressService uploadProgressService;
        private readonly Regex regex = new Regex("\"(.*?)\"");
        private const string NO_DATA = "NO DATA";
        private readonly ICaseCreationService _caseCreationService;

        public UploadService(ICaseCreationService caseCreationService, ApplicationDbContext context, ICustomApiCLient customApiCLient,
            IProgressService uploadProgressService)
        {
            _context = context;
            _caseCreationService = caseCreationService;
            this.customApiCLient = customApiCLient;
            this.uploadProgressService = uploadProgressService;
        }
        public async Task<bool> DoUpload(ClientCompanyApplicationUser companyUser, string[] dataRows, CREATEDBY autoOrManual, ZipArchive archive, ORIGIN fileOrFtp, long lineOfBusinessId)
        {
            try
            {

                DataTable dt = new DataTable();
                bool firstRow = true;

                foreach (string row in dataRows)
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        if (firstRow)
                        {
                            foreach (string cell in row.Split(','))
                            {
                                dt.Columns.Add(cell.Trim());
                            }
                            firstRow = false;
                        }
                        else
                        {
                            try
                            {
                                dt.Rows.Add();
                                int i = 0;
                                var output = regex.Replace(row, m => m.Value.Replace(',', '@'));
                                var rowData = output.Split(',').ToList();
                                foreach (string cell in rowData)
                                {
                                    dt.Rows[dt.Rows.Count - 1][i] = cell?.Trim() ?? NO_DATA;
                                    i++;
                                }

                                var claim = await CreatePolicy(rowData, companyUser, autoOrManual, archive, fileOrFtp, lineOfBusinessId);

                                if (claim is null)
                                {
                                    continue;
                                }
                                //CREATE CUSTOMER
                                var customerTask = CreateCustomer(companyUser, rowData, claim, archive);

                                //CREATE BENEFICIARY
                                var beneficiaryTask = CreateBeneficiary(companyUser, rowData, claim, archive);

                                // Await both tasks
                                await Task.WhenAll(customerTask, beneficiaryTask);

                                // Get the results
                                var customer = await customerTask;
                                var beneficiary = await beneficiaryTask;

                                if (customer is null || beneficiary is null)
                                {
                                    continue;
                                }
                                _context.ClaimsInvestigation.Add(claim);

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.StackTrace);
                                return false;
                            }
                        }
                    }
                }

                return _context.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }
        private async Task<ClaimsInvestigation> CreatePolicy(List<string> rowData, ClientCompanyApplicationUser companyUser, CREATEDBY autoOrManual, ZipArchive archive, ORIGIN fileOrFtp, long lineOfBusinessId)
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
            var policyImage = archive.Entries.FirstOrDefault(p => p.FullName.ToLower().EndsWith(rowData[0]?.Trim().ToLower() + "/policy.jpg"));
            byte[] savedNewImage = null;
            using (var pImage = policyImage.Open())
            {
                using (var ps = new MemoryStream())
                {
                    await pImage.CopyToAsync(ps);
                    savedNewImage = ps.ToArray();
                }
            }
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
                DocumentImage = savedNewImage,
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email
            };

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

            return claim;
        }

        private async Task<CustomerDetail> CreateCustomer(ClientCompanyApplicationUser companyUser, List<string> rowData, ClaimsInvestigation claim, ZipArchive archive)
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


            var customerImage = archive.Entries.FirstOrDefault(p => p.FullName.ToLower().EndsWith(rowData[0]?.Trim().ToLower() + "/customer.jpg"));
            byte[] customerNewImage = null;
            using (var cImage = customerImage.Open())
            {
                using (var cs = new MemoryStream())
                {
                    await cImage.CopyToAsync(cs);
                    customerNewImage = cs.ToArray();
                }
            }
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
                ProfilePicture = customerNewImage,
                UpdatedBy = companyUser.Email,
                Updated = DateTime.Now,
                ClaimsInvestigation = claim
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
            _context.CustomerDetail.Add(customerDetail);
            return customerDetail;
        }

        private async Task<BeneficiaryDetail> CreateBeneficiary(ClientCompanyApplicationUser companyUser, List<string> rowData, ClaimsInvestigation claim, ZipArchive archive)
        {
            var pinCode = _context.PinCode
                                    .Include(p => p.District)
                                    .Include(p => p.State)
                                    .Include(p => p.Country)
                                    .FirstOrDefault(p => p.Code == rowData[28].Trim());
            if (pinCode.CountryId != companyUser.ClientCompany.CountryId)
            {
                return null;
            }
            var relation = _context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == rowData[23].Trim().ToLower());

            var beneficiaryImage = archive.Entries.FirstOrDefault(p => p.FullName.ToLower().EndsWith(rowData[0]?.Trim().ToLower() + "/beneficiary.jpg"));
            byte[] beneficiaryNewImage = null;
            using (var bImage = beneficiaryImage.Open())
            {
                using var bs = new MemoryStream();
                {
                    await bImage.CopyToAsync(bs);

                    beneficiaryNewImage = bs.ToArray();
                }
            }

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
                ProfilePicture = beneficiaryNewImage,
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email,
                ClaimsInvestigation = claim
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

        public async Task<List<ClaimsInvestigation>> PerformCustomUpload(ClientCompanyApplicationUser companyUser, List<UploadCase> customData, FileOnFileSystemModel model)
        {
            try
            {
                if (customData == null || customData.Count == 0)
                {
                    return null; // Return 0 if no CSV data is found
                }
                var uploadedClaims = new List<ClaimsInvestigation>();
                var uploadedRecordsCount = 0;
                var totalCount = customData.Count;
                foreach (var row in customData)
                {
                    var claimUploaded = await _caseCreationService.PerformUpload(companyUser, row, model);
                    if (claimUploaded == null)
                    {
                        return null;
                    }
                    uploadedClaims.Add(claimUploaded);
                    int progress = (int)(((uploadedRecordsCount + 1) / (double)totalCount) * 100);
                    uploadProgressService.UpdateProgress(model.Id, progress);
                    uploadedRecordsCount++;
                }
                var rowsSaved = _context.SaveChanges() > 0;
                return rowsSaved ? uploadedClaims : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }
    }
}
