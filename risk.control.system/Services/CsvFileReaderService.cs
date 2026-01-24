using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICsvFileReaderService
    {
        Task<(byte[] zipFileByteData,List<UploadCase> ValidRecords, List<string> Errors)> ReadPipeDelimitedCsvFromZip(FileOnFileSystemModel uploadFileData);
    }
    internal class CsvFileReaderService: ICsvFileReaderService
    {
        private readonly IWebHostEnvironment webHostEnvironment;

        public CsvFileReaderService(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<(byte[] zipFileByteData, List<UploadCase> ValidRecords, List<string> Errors)> ReadPipeDelimitedCsvFromZip(FileOnFileSystemModel uploadFileData)
        {
            var filePath = Path.Combine(webHostEnvironment.ContentRootPath, uploadFileData.FilePath);

            var zipFileByteData = await File.ReadAllBytesAsync(filePath);
            var validRecords = new List<UploadCase>();
            var errors = new List<string>();

            await using var zipStream = new MemoryStream(zipFileByteData);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var csvEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            if (csvEntry == null)
            {
                errors.Add("No CSV file found in ZIP.");
                return (zipFileByteData, validRecords, errors);
            }
            string? firstLine;
            using (var detectReader = new StreamReader(csvEntry.Open()))
            {
                firstLine =await detectReader.ReadLineAsync();
            }
            if (string.IsNullOrWhiteSpace(firstLine))
            {
                errors.Add("CSV file is empty or has no header.");
                return (zipFileByteData, validRecords, errors);
            }
            int pipeCount = firstLine.Count(c => c == '|');

            if (pipeCount < 26)
            {
                errors.Add("The file is has less than expected columns.");
                return (zipFileByteData, validRecords, errors);
            }
            using var reader = new StreamReader(csvEntry.Open());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "|", // 👈 Pipe-delimited
                TrimOptions = TrimOptions.Trim,
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = context =>
                {
                    errors.Add($"Row {context.Field}: Bad data - {context.RawRecord}");
                }
            });

            int rowNumber = 1;
            try
            {
                csv.Read();
                csv.ReadHeader();
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading header: {ex.Message}");
                return (zipFileByteData, validRecords, errors);
            }

            while (csv.Read())
            {
                rowNumber++;
                try
                {
                    var record = csv.GetRecord<UploadCase>();
                    validRecords.Add(record);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNumber}: {ex.Message}");
                }
            }

            return (zipFileByteData, validRecords, errors);
        }
    }
}
