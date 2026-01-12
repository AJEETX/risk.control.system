using MetadataExtractor;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

using Image = SixLabors.ImageSharp.Image;

namespace risk.control.system.Controllers.Tools
{
    public class DocumentVerificationController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> userManager;

        public DocumentVerificationController(IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _env = env;
            this.userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Unauthorized");
            }
            var model = new ImageAnalysisViewModel
            {
                RemainingTries = 5 - (user?.DocumentAnalysisCount ?? 0)
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // 1. Check Usage Limit before calling AWS
                var user = await userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized("Unauthorized");
                }
                if (user.DocumentAnalysisCount >= 5)
                {
                    return StatusCode(403, new { message = "Dcoument Analysis limit reached (5/5)" });
                }

                if (file == null || file.Length == 0) return RedirectToAction("Index");

                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!System.IO.Directory.Exists(uploads)) System.IO.Directory.CreateDirectory(uploads);

                var filePath = Path.Combine(uploads, file.FileName);
                var elaPath = Path.Combine(uploads, "ela_" + file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 1. Run Metadata Check
                bool isSuspicious = CheckMetadata(filePath);

                // 2. Generate ELA Image
                GenerateElaImage(filePath, elaPath);

                user.DocumentAnalysisCount++;
                await userManager.UpdateAsync(user);

                var model = new ImageAnalysisViewModel
                {
                    OriginalImageUrl = "/uploads/" + file.FileName,
                    ElaImageUrl = "/uploads/ela_" + file.FileName,
                    MetadataFlagged = isSuspicious
                };

                if (isSuspicious) model.AnalysisNotes.Add("Warning: Editing software (Photoshop/GIMP) detected in metadata.");

                return View("Index", model);
            }
            catch (Exception)
            {

                throw;
            }

        }

        private void GenerateElaImage(string sourcePath, string outputPath, int quality = 90)
        {
            using var image = Image.Load<Rgba32>(sourcePath);
            using var ms = new MemoryStream();

            // Save at lower quality to highlight artifacts
            image.SaveAsJpeg(ms, new JpegEncoder { Quality = quality });
            ms.Position = 0;
            using var compressed = Image.Load<Rgba32>(ms);

            using var elaImage = new Image<Rgba32>(image.Width, image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var p1 = image[x, y];
                    var p2 = compressed[x, y];

                    // Calculate difference and scale it (multiplier 20 helps visibility)
                    byte r = (byte)Math.Clamp(Math.Abs(p1.R - p2.R) * 20, 0, 255);
                    byte g = (byte)Math.Clamp(Math.Abs(p1.G - p2.G) * 20, 0, 255);
                    byte b = (byte)Math.Clamp(Math.Abs(p1.B - p2.B) * 20, 0, 255);

                    elaImage[x, y] = new Rgba32(r, g, b);
                }
            }
            elaImage.SaveAsPng(outputPath);
        }

        private bool CheckMetadata(string path)
        {
            var directories = ImageMetadataReader.ReadMetadata(path);
            return directories.Any(d => d.Tags.Any(t =>
                t.Name.Contains("Software") && (t.Description.Contains("Adobe") || t.Description.Contains("GIMP"))));
        }
    }
}
