using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Creator
{
    public interface ICaseUploadService
    {
        Task<UploadPermissionResult> GetUploadViewDataAsync(string userEmail, int uploadId);

        Task<DownloadFileResult> GetDownloadLogAsync(long id);

        Task<DownloadErrorFileResult> GetDownloadErrorLogAsync(long id);

        Task<(bool Success, string Message)> DeleteLogAsync(int id, string userEmail);
    }

    internal class CaseUploadService : ICaseUploadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CaseUploadService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILicenseService _licenseService;

        public CaseUploadService(
            ApplicationDbContext context,
            ILogger<CaseUploadService> logger,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            ILicenseService licenseService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _env = env;
            _licenseService = licenseService;
        }

        public async Task<UploadPermissionResult> GetUploadViewDataAsync(string userEmail, int uploadId)
        {
            var companyUser = await _context.ApplicationUser.AsNoTracking()
                .Include(u => u.ClientCompany)
                .Include(u => u.Country)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (companyUser == null) return null;

            var licenseStatus = await _licenseService.GetUploadPermissionsAsync(companyUser);
            var isManager = await _userManager.IsInRoleAsync(companyUser, MANAGER.DISPLAY_NAME);

            return new UploadPermissionResult
            {
                IsManager = isManager,
                UserCanCreate = licenseStatus.CanCreate,
                HasClaims = licenseStatus.HasClaimsPending,
                FileSampleIdentifier = companyUser.Country?.Code?.ToLower() ?? "default",
                LicenseStatus = licenseStatus,
                // Logic for notification trigger
                ShouldSendTrialNotification = uploadId == 0 && companyUser.ClientCompany.LicenseType == LicenseType.Trial
            };
        }

        public async Task<DownloadFileResult> GetDownloadLogAsync(long id)
        {
            var file = await _context.FilesOnFileSystem.FirstOrDefaultAsync(x => x.Id == id);

            if (file == null || string.IsNullOrWhiteSpace(file.FilePath))
                return new DownloadFileResult { ErrorMessage = "File record not found" };

            var fullPath = Path.Combine(_env.ContentRootPath, file.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return new DownloadFileResult { ErrorMessage = "File missing on server" };

            // Use FileShare.Read to ensure we don't lock the file from other processes
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return new DownloadFileResult
            {
                FileStream = stream,
                FileName = file.Name
            };
        }

        public async Task<DownloadErrorFileResult> GetDownloadErrorLogAsync(long id)
        {
            var file = await _context.FilesOnFileSystem
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (file == null || file.ErrorByteData == null)
            {
                return new DownloadErrorFileResult { ErrorMessage = "Error log not found" };
            }

            return new DownloadErrorFileResult
            {
                FileBytes = file.ErrorByteData,
                FileName = $"{file.Name}_UploadError_{id}.csv",
                ContentType = "text/csv"
            };
        }

        public async Task<(bool Success, string Message)> DeleteLogAsync(int id, string userEmail)
        {
            // 1. Fetch user (if you need specific permission checks, they go here)
            var user = await _context.ApplicationUser
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null) return (false, "User not found.");

            // 2. Fetch the file (Removed AsNoTracking because we are updating it)
            var file = await _context.FilesOnFileSystem
                .Include(f => f.CaseIds)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null) return (false, "File not found.");

            // 3. Physical File Deletion
            try
            {
                if (!string.IsNullOrEmpty(file.FilePath) && File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Physical file deletion failed for ID {Id}", id);
                // We continue even if physical delete fails so the DB stays in sync
            }

            // 4. Database Soft Delete
            file.Deleted = true;
            _context.FilesOnFileSystem.Update(file);
            await _context.SaveChangesAsync();

            return (true, "File deleted successfully.");
        }
    }
}