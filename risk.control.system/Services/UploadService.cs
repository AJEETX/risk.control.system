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
    public interface IUploadService
    {
        Task<bool> DoUpload(ClientCompanyApplicationUser companyUser, string[] dataRows, CREATEDBY autoOrManual, ZipArchive archive, ORIGIN fileOrFtp, long lineOfBusinessId);
    }
    public class UploadService : IUploadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomApiCLient customApiCLient;
        private readonly Regex regex = new Regex("\"(.*?)\"");
        private const string NO_DATA = "NO DATA";
        public UploadService(ApplicationDbContext context, ICustomApiCLient customApiCLient)
        {
            _context = context;
            this.customApiCLient = customApiCLient;
        }
        public async Task<bool> DoUpload(ClientCompanyApplicationUser companyUser, string[] dataRows, CREATEDBY autoOrManual, ZipArchive archive, ORIGIN fileOrFtp, long lineOfBusinessId)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var autoEnabled = companyUser.ClientCompany.AutoAllocation;
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

                            var pinCode = _context.PinCode
                                .Include(p => p.District)
                                .Include(p => p.State)
                                .Include(p => p.Country)
                                .FirstOrDefault(p => p.Code == rowData[19].Trim());
                            if (pinCode.CountryId != companyUser.ClientCompany.CountryId)
                            {
                                continue;
                            }
                            //CREATE CLAIM
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
                            };

                            //CREATE CUSTOMER

                            var district = _context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);

                            var state = _context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);

                            var country = _context.Country.FirstOrDefault(c => c.CountryId == pinCode.Country.CountryId);

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
                            claim.CustomerDetail = new CustomerDetail
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
                                CountryId = country.CountryId,
                                PinCodeId = pinCode.PinCodeId,
                                StateId = state.StateId,
                                DistrictId = district.DistrictId,
                                //Description = rowData[20]?.Trim(),
                                ProfilePicture = customerNewImage,
                            };

                            var address = claim.CustomerDetail.Addressline + ", " +
                                pinCode.District.Name + ", " +
                                pinCode.State.Name + ", " +
                                pinCode.Country.Code + ", " +
                                pinCode.Code;

                            var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                            claim.CustomerDetail.Latitude = coordinates.Latitude;
                            claim.CustomerDetail.Longitude = coordinates.Longitude;
                            var customerLatLong = claim.CustomerDetail.Latitude + "," + claim.CustomerDetail.Longitude;
                            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                            claim.CustomerDetail.CustomerLocationMap = url;

                            //CREATE BENEFICIARY
                            var benePinCode = _context.PinCode
                                .Include(p => p.District)
                                .Include(p => p.State)
                                .Include(p => p.Country)
                                .FirstOrDefault(p => p.Code == rowData[28].Trim());

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
                                PinCodeId = benePinCode.PinCodeId,
                                DistrictId = benePinCode.District.DistrictId,
                                StateId = benePinCode.State.StateId,
                                CountryId = benePinCode.Country.CountryId,
                                ProfilePicture = beneficiaryNewImage,
                                Updated = DateTime.Now,
                                UpdatedBy = companyUser.Email,
                                ClaimsInvestigation = claim
                            };
                            beneficairy.ClaimsInvestigationId = claim.ClaimsInvestigationId;

                            var address2 = beneficairy.Addressline + ", " +
                                benePinCode.District.Name + ", " +
                                benePinCode.State.Name + ", " +
                                benePinCode.Country.Code + ", " +
                                benePinCode.Code;

                            var beneCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address2);

                            var beneLatLong = beneCoordinates.Latitude + "," + beneCoordinates.Longitude;
                            beneficairy.Latitude = beneCoordinates.Latitude;
                            beneficairy.Longitude = beneCoordinates.Longitude;

                            var beneUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{beneLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                            beneficairy.BeneficiaryLocationMap = beneUrl;
                            _context.BeneficiaryDetail.Add(beneficairy);
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
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                            return false;
                        }
                    }
                }
            }
            try
            {
                return _context.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}
