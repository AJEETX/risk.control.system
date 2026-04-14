using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Creator
{
    public interface ICsvFileReaderService
    {
        Task<(byte[] ZipFileData, List<UploadCase> ValidRecords, List<string> Errors)>
            ReadPipeDelimitedCsvFromZip(FileOnFileSystemModel uploadFileData);
    }

    internal sealed class CsvFileReaderService(IWebHostEnvironment env) : ICsvFileReaderService
    {
        private readonly IWebHostEnvironment _env = env;

        public async Task<(byte[] ZipFileData, List<UploadCase> ValidRecords, List<string> Errors)> ReadPipeDelimitedCsvFromZip(FileOnFileSystemModel uploadFileData)
        {
            var validRecords = new List<UploadCase>();
            var errors = new List<string>();
            var filePath = Path.Combine(_env.ContentRootPath, uploadFileData.FilePath ?? string.Empty);
            if (!File.Exists(filePath))
            {
                errors.Add("Uploaded ZIP file not found.");
                return (Array.Empty<byte>(), validRecords, errors);
            }
            var zipFileData = await File.ReadAllBytesAsync(filePath);
            await using var archive = new ZipArchive(new MemoryStream(zipFileData), ZipArchiveMode.Read);
            var csvEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            if (csvEntry == null)
            {
                errors.Add("No CSV file found in ZIP.");
                return (zipFileData, validRecords, errors);
            }
            using var reader = new StreamReader(csvEntry.Open());
            using var csv = new CsvReader(reader, GetCsvConfiguration(errors));
            try
            {
                if (!await csv.ReadAsync() || !csv.ReadHeader())
                {
                    errors.Add("CSV file is empty or has no header.");
                    return (zipFileData, validRecords, errors);
                }
                if (csv.HeaderRecord?.Length < 27) // 26 pipes = 27 columns
                {
                    errors.Add("The CSV file has fewer columns than expected.");
                    return (zipFileData, validRecords, errors);
                }
                while (await csv.ReadAsync())
                {
                    try
                    {
                        var record = csv.GetRecord<UploadCase>();
                        if (record != null) validRecords.Add(record);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {csv.Context.Parser!.Row!}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Critical error reading CSV: {ex.Message}");
            }
            return (zipFileData, validRecords, errors);
        }

        private static CsvConfiguration GetCsvConfiguration(List<string> errors) => new(CultureInfo.InvariantCulture)
        {
            Delimiter = "|",
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = ctx => errors.Add($"Bad data at row {ctx.Field}: {ctx.RawRecord}")
        };
    }
}