using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IZipFileService
    {
        Task<int> Save(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, bool uploadAndAssign = false);
    }

    internal class ZipFileService : IZipFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ZipFileService> logger;
        private readonly IFileStorageService fileStorageService;

        public ZipFileService(ApplicationDbContext context,
            ILogger<ZipFileService> logger,
            IFileStorageService fileStorageService)
        {
            _context = context;
            this.logger = logger;
            this.fileStorageService = fileStorageService;
        }

        public async Task<int> Save(string userEmail, IFormFile postedFile, CREATEDBY autoOrManual, bool uploadAndAssign = false)
        {
            var (fileName, relativePath) = await fileStorageService.SaveAsync(postedFile, "UploadFile");

            using (var dataStream = new MemoryStream())
            {
                await postedFile.CopyToAsync(dataStream);
                await File.WriteAllBytesAsync(relativePath, dataStream.ToArray());
            }

            var uploadId = await SaveUpload(postedFile, relativePath, fileName, userEmail, autoOrManual, ORIGIN.FILE, uploadAndAssign);
            return uploadId;
        }

        private async Task<int> SaveUpload(IFormFile file, string filePath, string description, string uploadedBy, CREATEDBY autoOrManual, ORIGIN fileOrFtp, bool uploadAndAssign = false)
        {
            var fileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var company = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == uploadedBy);
            int lastCompanySequence = await _context.FilesOnFileSystem.Where(f => f.CompanyId == company.ClientCompanyId).MaxAsync(f => (int?)f.CompanySequenceNumber) ?? 0;

            int lastUserSequence = await _context.FilesOnFileSystem.Where(f => f.CompanyId == company.ClientCompanyId && f.UploadedBy == uploadedBy).MaxAsync(f => (int?)f.UserSequenceNumber) ?? 0;
            var fileModel = new FileOnFileSystemModel
            {
                CompanySequenceNumber = lastCompanySequence + 1,
                UserSequenceNumber = lastUserSequence + 1,
                CreatedOn = DateTime.Now,
                FileType = file.ContentType,
                Extension = extension,
                Name = fileName,
                Description = description,
                FilePath = filePath,
                UploadedBy = uploadedBy,
                CompanyId = company.ClientCompanyId,
                AutoOrManual = autoOrManual,
                Message = uploadAndAssign ? "Assign In progress" : "Upload In progress",
                FileOrFtp = fileOrFtp,
                DirectAssign = uploadAndAssign
            };
            var uploadData = _context.FilesOnFileSystem.Add(fileModel);
            await _context.SaveChangesAsync();
            return uploadData.Entity.Id;
        }
    }
}