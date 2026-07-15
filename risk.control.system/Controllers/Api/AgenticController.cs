using System.IO.Compression;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Agentic;
using risk.control.system.Services.Common;
using risk.control.system.Services.Tool;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class AgenticController(IGoogleOcrService googleService, IFileStorageService fileStorageService, IAmazonS3 s3Client, IAgenticService agenticService, IAmazonApiService amazonApiService) : ControllerBase
    {
        private readonly IGoogleOcrService _googleService = googleService;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly IAmazonS3 _s3Client = s3Client;
        private readonly IAgenticService _agenticService = agenticService;
        private readonly IAmazonApiService _amazonApiService = amazonApiService;

        //Ocr Endpoint
        [HttpPost("Ocr")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> OcrDocument(IFormFile image)
        {
            try
            {
                var (file, path) = await _fileStorageService.SaveAsync(image, "tool");
                var ocrTextData = await _googleService.DetectTextAsync(path);
                if (string.IsNullOrWhiteSpace(ocrTextData))
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "No text found in the image.",
                        Data = ocrTextData
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "OCR completed successfully.",
                    Data = ocrTextData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }

        //Face Existence Endpoint
        [HttpPost("FaceExists")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> FaceExists(IFormFile image)
        {
            try
            {
                var faceExists = await _agenticService.FaceExistsAsync(image);
                if (faceExists.Item1)
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = faceExists.Item2,
                    });
                }
                return Ok(new
                {
                    Success = true,
                    Message = faceExists.Item2,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpPost("zip-and-upload-sample-report/{contractNumber}")]
        public async Task<IActionResult> UploadReport(string contractNumber)
        {
            var s3KeyName = $"backups/{contractNumber}/Agency_Report.zip";

            try
            {
                byte[] zipBytes = CreateZippedPdfInMemory();

                await UploadBytesToS3Async(zipBytes, EnvHelper.Get(CONSTANTS.S3_BUCKET)!, s3KeyName);
                return Ok(new { Success = true, Message = $"Report for contract {contractNumber} uploaded successfully." });
            }
            catch (AmazonS3Exception amazonException)
            {
                return BadRequest($"{amazonException.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }
        private async Task UploadBytesToS3Async(byte[] fileBytes, string bucketName, string s3Key)
        {
            bool bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

            if (!bucketExists)
            {
                var putBucketRequest = new PutBucketRequest { BucketName = bucketName, UseClientRegion = true };
                await _s3Client.PutBucketAsync(putBucketRequest);

                var publicAccessBlockRequest = new PutPublicAccessBlockRequest
                {
                    BucketName = bucketName,
                    PublicAccessBlockConfiguration = new PublicAccessBlockConfiguration
                    {
                        BlockPublicAcls = true,
                        BlockPublicPolicy = true,
                        IgnorePublicAcls = true,
                        RestrictPublicBuckets = true
                    }
                };
                await _s3Client.PutPublicAccessBlockAsync(publicAccessBlockRequest);
            }

            using var fileTransferUtility = new TransferUtility(_s3Client);
            await using var uploadStream = new MemoryStream(fileBytes);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = uploadStream,
                BucketName = bucketName,
                Key = s3Key,
                ContentType = "application/zip"
            };

            await fileTransferUtility.UploadAsync(uploadRequest);
        }
        private static byte[] CreateZippedPdfInMemory()
        {
            // 1. Declare the stream outside so we can access it after the archive is disposed
            using (var outputZipStream = new MemoryStream())
            {
                // 2. Wrap the archive creation in its own block scope
                using (var archive = new ZipArchive(outputZipStream, ZipArchiveMode.Create, true))
                {
                    var pdfEntry = archive.CreateEntry("SampleDocument.pdf", CompressionLevel.Optimal);

                    using (var entryStream = pdfEntry.Open())
                    using (var writer = new PdfWriter(entryStream))
                    using (var pdf = new PdfDocument(writer))
                    {
                        Document document = new Document(pdf);

                        Paragraph title = new Paragraph("Dynamically Generated Document")
                            .SetFontSize(22)
                            .SetFontColor(ColorConstants.BLUE)
                            .SetMarginBottom(15);
                        document.Add(title);

                        Paragraph body = new Paragraph(
                            "This PDF was generated directly into a compressed ZIP stream in C#. " +
                            "The workflow avoids any heavy local disk I/O operations by leveraging MemoryStreams.")
                            .SetFontSize(11)
                            .SetMarginBottom(15);
                        document.Add(body);

                        Table table = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Parameter")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Value")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                        table.AddCell("Generated By");
                        table.AddCell(".NET Core application");
                        table.AddCell("Compression Type");
                        table.AddCell("System.IO.Compression.ZipArchive");

                        document.Add(table);

                        document.Close(); // Explicitly close the iText document layout structure
                    }
                    // entryStream closes naturally here
                }
                // 3. CRITICAL: The ZipArchive goes out of scope and disposes right here!
                // This action finalizes the zip structure and commits the final bytes to outputZipStream.

                // 4. Safe to extract and return the fully structured zip byte array now
                return outputZipStream.ToArray();
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpPost("download-zip-report/{contractNumber}")]
        public async Task<IActionResult> DownloadReport(string contractNumber)
        {
            var s3KeyName = $"backups/{contractNumber}/Agency_Report.zip";

            try
            {
                GetObjectResponse response = await _s3Client.GetObjectAsync(CONSTANTS.S3_BUCKET, s3KeyName);

                return File(response.ResponseStream, "application/zip", $"Report_{contractNumber}.zip");
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("The requested report zip file does not exist.");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("image-collection-ids")]
        public async Task<IActionResult> Collections()
        {
            try
            {
                var collectionIds = await _amazonApiService.GetAllFaceCollectionIdsAsync();
                return Ok(collectionIds);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Internal server error");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("images-count-in-aws")]
        public async Task<IActionResult> ImagesCountInAws(string? imageCollection = null)
        {
            imageCollection ??= EnvHelper.Get(CONSTANTS.FaceImageCollection);
            try
            {
                var response = await _amazonApiService.FaceCollectionExistAsync(imageCollection!);
                return Ok(new
                {
                    Success = true,
                    Message = response
                });
            }
            catch (AmazonS3Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("get-all-images-from-collection-id")]
        public async Task<IActionResult> GetAllFacesFromCollection(string? imageCollection = null)
        {
            imageCollection ??= EnvHelper.Get(CONSTANTS.FaceImageCollection);

            try
            {
                var faces = await _amazonApiService.GetAllFacesFromCollectionAsync(imageCollection!);
                return Ok(faces);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Internal server error");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpPost("delete-images-from-collection-id")]
        public async Task<IActionResult> DeleteImagesFromAws(string? imageCollection = null)
        {
            imageCollection ??= EnvHelper.Get(CONSTANTS.FaceImageCollection);

            try
            {
                var deletedResponse = await _amazonApiService.DeleteCollectionAsync(imageCollection!);

                return Ok(new
                {
                    Success = true,
                    Message = "Images deleted successfully from Aws."
                });
            }
            catch (AmazonS3Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("s3-buckets")]
        public async Task<IActionResult> ListBuckets()
        {
            try
            {
                var bucketsResponse = await _s3Client.ListBucketsAsync();
                var bucketListTasks = bucketsResponse.Buckets.Select(async b =>
                {
                    string regionName = "us-east-1"; // Fallback default
                    try
                    {
                        // Query AWS for this specific bucket's home region
                        var locationResponse = await _s3Client.GetBucketLocationAsync(new GetBucketLocationRequest
                        {
                            BucketName = b.BucketName
                        });

                        string locationConstraint = locationResponse.Location?.Value!;

                        // AWS returns empty string or "US" if the bucket resides in us-east-1
                        regionName = string.IsNullOrEmpty(locationConstraint) || locationConstraint == "US"
                            ? "us-east-1"
                            : locationConstraint;
                    }
                    catch (Exception)
                    {
                        // Handle cases where you might not have permissions to read a specific bucket's location
                        regionName = "Unknown/AccessDenied";
                    }

                    return new
                    {
                        Name = b.BucketName,
                        Region = regionName,
                        CreatedOn = $"{b.CreationDate:yyyy-MM-dd HH:mm:ss}"
                    };
                });

                // Await all the parallel region check tasks cleanly
                var buckets = (await Task.WhenAll(bucketListTasks)).ToList();
                return Ok(new { Success = true, Buckets = buckets });
            }
            catch (AmazonS3Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("list-bucket-contents")]
        public async Task<IActionResult> ListBucketContents(string? s3BucketName = null)
        {
            s3BucketName ??= EnvHelper.Get(CONSTANTS.S3_BUCKET)!;
            try
            {
                bool bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, s3BucketName);
                if (!bucketExists)
                {
                    return NotFound($"Bucket '{s3BucketName}' does not exist.");
                }
                var request = new ListObjectsV2Request { BucketName = s3BucketName };
                var response = await _s3Client.ListObjectsV2Async(request);
                var objectKeys = response.S3Objects.Select(obj => obj.Key).ToList();
                return Ok(new { Success = true, BucketName = s3BucketName, Contents = objectKeys });
            }
            catch (AmazonS3Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpPost("empty-bucket-contents")]
        public async Task<IActionResult> EmptyBucketContents(string? s3BucketName = null)
        {
            try
            {
                s3BucketName ??= EnvHelper.Get(CONSTANTS.S3_BUCKET)!;
                var emptyResult = await _amazonApiService.EmptyBucketContents(s3BucketName);

                return Ok(new { Success = true, Message = emptyResult });
            }
            catch (AmazonS3Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpPost("delete-bucket")]
        public async Task<IActionResult> DeleteBucket(string? s3BucketName = null)
        {
            try
            {
                s3BucketName ??= EnvHelper.Get(CONSTANTS.S3_BUCKET)!;
                var deleteResult = await _amazonApiService.DeleteBucketAsync(s3BucketName);

                return Ok(new { Success = true, Message = deleteResult });
            }
            catch (AmazonS3Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }

        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        //[HttpPost("convert-image-to-searchable-pdf")]
        //public async Task<IActionResult> ConvertImageToSearchablePdf(IFormFile imageFile)
        //{
        //    if (imageFile == null || imageFile.Length == 0)
        //    {
        //        return BadRequest("No image file provided.");
        //    }

        //    try
        //    {
        //        var stream = imageFile.OpenReadStream();
        //        var imageOnDiskPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}");
        //        using (var fileStream = new FileStream(imageOnDiskPath, FileMode.Create))
        //        {
        //            await stream.CopyToAsync(fileStream);
        //        }
        //        var pdfBytes = _agenticService.ConvertImageToSearchablePdfBytes(imageOnDiskPath);
        //        return File(pdfBytes, "application/pdf", "converted_document.pdf");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error converting image to searchable PDF: {ex.Message}");
        //        return StatusCode(500, "An error occurred while processing the request.");
        //    }
        //}
    }
}