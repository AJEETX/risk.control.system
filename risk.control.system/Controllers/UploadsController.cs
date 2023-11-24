using System.Data;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models.ViewModel;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    public class UploadsController : Controller
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ApplicationDbContext _context;
        private static string NO_DATA = " NO - DATA ";
        private static Regex regex = new Regex("\\\"(.*?)\\\"");

        public UploadsController(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            this.webHostEnvironment = webHostEnvironment;
            this._context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [Breadcrumb(" Upload Log", FromController = typeof(ClaimsInvestigationController))]
        public async Task<IActionResult> Uploads()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var fileuploadViewModel = await LoadAllFiles(userEmail);
            ViewBag.Message = TempData["Message"];
            return View(fileuploadViewModel);
        }

        public async Task<IActionResult> DownloadLog(int id)
        {
            var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (file == null) return null;
            var memory = new MemoryStream();
            using (var stream = new FileStream(file.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, file.FileType, file.Name + file.Extension);
        }

        public async Task<IActionResult> DeleteLog(int id)
        {
            var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (file == null) return null;
            if (System.IO.File.Exists(file.FilePath))
            {
                System.IO.File.Delete(file.FilePath);
            }
            _context.FilesOnFileSystem.Remove(file);
            _context.SaveChanges();
            TempData["Message"] = $"Removed {file.Name + file.Extension} successfully from File System.";
            return RedirectToAction("Uploads");
        }

        private async Task<FileUploadViewModel> LoadAllFiles(string userEmail)
        {
            var viewModel = new FileUploadViewModel();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            viewModel.FilesOnFileSystem = await _context.FilesOnFileSystem.Where(f => f.CompanyId == company.ClientCompanyId).ToListAsync();
            return viewModel;
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = Path.GetFileName(postedFile.FileName);
                string filePath = Path.Combine(path, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                string csvData = await System.IO.File.ReadAllTextAsync(filePath);
                DataTable dt = new DataTable();
                bool firstRow = true;
                foreach (string row in csvData.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(row))
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
                                dt.Rows.Add();
                                int i = 0;
                                var output = regex.Replace(row, m => m.Value.Replace(',', '@'));
                                var rowData = output.Split(',').ToList();
                                foreach (string cell in rowData)
                                {
                                    dt.Rows[dt.Rows.Count - 1][i] = cell?.Trim() ?? NO_DATA;
                                    i++;
                                }
                            }
                        }
                    }
                }

                return View(dt);
            }
            return Problem();
        }
    }
}