using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICsvFileReaderService
    {
        Task<(byte[] ZipFileData, List<UploadCase> ValidRecords, List<string> Errors)>
            ReadPipeDelimitedCsvFromZip(FileOnFileSystemModel uploadFileData);
    }

    internal sealed class CsvFileReaderService : ICsvFileReaderService
    {
        private readonly IWebHostEnvironment _environment;

        public CsvFileReaderService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<(byte[] ZipFileData, List<UploadCase> ValidRecords, List<string> Errors)>
            ReadPipeDelimitedCsvFromZip(FileOnFileSystemModel uploadFileData)
        {
            var validRecords = new List<UploadCase>();
            var errors = new List<string>();

            var filePath = Path.Combine(_environment.ContentRootPath, uploadFileData.FilePath);

            if (!File.Exists(filePath))
            {
                errors.Add("Uploaded ZIP file not found.");
                return (Array.Empty<byte>(), validRecords, errors);
            }

            var zipFileData = await File.ReadAllBytesAsync(filePath);

            using var zipStream = new MemoryStream(zipFileData);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var csvEntry = archive.Entries
                .FirstOrDefault(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

            if (csvEntry == null)
            {
                errors.Add("No CSV file found in ZIP.");
                return (zipFileData, validRecords, errors);
            }

            // -----------------------------
            // 1️⃣ Header validation
            // -----------------------------
            using (var headerStream = csvEntry.Open())
            using (var headerReader = new StreamReader(headerStream))
            {
                var headerLine = await headerReader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    errors.Add("CSV file is empty or has no header.");
                    return (zipFileData, validRecords, errors);
                }

                if (headerLine.Count(c => c == '|') < 26)
                {
                    errors.Add("The CSV file has fewer columns than expected.");
                    return (zipFileData, validRecords, errors);
                }
            }

            // -----------------------------
            // 2️⃣ CSV parsing
            // -----------------------------
            using (var dataStream = csvEntry.Open())
            using (var reader = new StreamReader(dataStream))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "|",
                TrimOptions = TrimOptions.Trim,
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = ctx =>
                {
                    errors.Add($"Bad data at row {ctx.Field}: {ctx.RawRecord}");
                }
            }))
            {
                try
                {
                    await csv.ReadAsync();
                    csv.ReadHeader();

                    while (await csv.ReadAsync())
                    {
                        try
                        {
                            var record = csv.GetRecord<UploadCase>();
                            validRecords.Add(record);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Row {csv.Context.Reader}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error reading CSV: {ex.Message}");
                }
            }

            return (zipFileData, validRecords, errors);
        }
    }
}