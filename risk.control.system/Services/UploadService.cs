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
        Task<List<InvestigationTask>> FileUpload(ClientCompanyApplicationUser companyUser, List<UploadCase> customData, FileOnFileSystemModel model);
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

        public async Task<List<InvestigationTask>> FileUpload(ClientCompanyApplicationUser companyUser, List<UploadCase> customData, FileOnFileSystemModel model)
        {
            try
            {
                if (customData == null || customData.Count == 0)
                {
                    return null; // Return 0 if no CSV data is found
                }
                var uploadedClaims = new List<InvestigationTask>();
                var uploadedRecordsCount = 0;
                var totalCount = customData.Count;
                foreach (var row in customData)
                {
                    var claimUploaded = await _caseCreationService.FileUpload(companyUser, row, model);
                    if (claimUploaded == null)
                    {
                        return null;
                    }
                    uploadedClaims.Add(claimUploaded);
                    int progress = (int)(((uploadedRecordsCount + 1) / (double)totalCount) * 100);
                    uploadProgressService.UpdateProgress(model.Id, progress);
                    uploadedRecordsCount++;
                }
                return uploadedClaims;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }
    }
}
