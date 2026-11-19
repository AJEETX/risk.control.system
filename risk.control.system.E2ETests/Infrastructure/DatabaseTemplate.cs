using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace risk.control.system.E2ETests.Infrastructure;

/// <summary>
/// Seeding a brand-new database takes minutes. After the first seeded start, a copy of the pristine database
/// is kept next to the test binaries and copied in on later runs, so the app starts in seconds.
/// The copy is rebuilt automatically when anything that shapes the seeded data changes
/// (Seeds/, Models/, appsettings*.json, the seed environment), or when deleted (E2E_REFRESH_TEMPLATE=true).
/// </summary>
internal sealed class DatabaseTemplate
{
    private readonly string _templateFile;
    private readonly string _hashFile;
    private readonly string _fingerprint;

    public DatabaseTemplate(string projectDir, IReadOnlyDictionary<string, string?> seedEnvironment)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "e2e-db-template");
        Directory.CreateDirectory(dir);
        _templateFile = Path.Combine(dir, "seeded.db");
        _hashFile = Path.Combine(dir, "seeded.db.sha256");
        _fingerprint = Fingerprint(projectDir, seedEnvironment);
    }

    public bool IsValid =>
        !TestSettings.RefreshTemplate
        && File.Exists(_templateFile)
        && File.Exists(_hashFile)
        && File.ReadAllText(_hashFile).Trim() == _fingerprint;

    /// <summary>Copies the template over <paramref name="dbFile"/>. The app sees seeded users and skips seeding.</summary>
    public void CopyTo(string dbFile) => File.Copy(_templateFile, dbFile, overwrite: true);

    /// <summary>Snapshots the freshly seeded, still unused database (safe while the app has it open).</summary>
    public void SaveFrom(string dbFile)
    {
        if (File.Exists(_templateFile))
        {
            File.Delete(_templateFile);
        }

        using (var connection = new SqliteConnection($"Data Source={dbFile};Mode=ReadOnly;Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "VACUUM INTO $path";
            command.Parameters.AddWithValue("$path", _templateFile);
            command.ExecuteNonQuery();
        }

        File.WriteAllText(_hashFile, _fingerprint);
    }

    private static string Fingerprint(string projectDir, IReadOnlyDictionary<string, string?> seedEnvironment)
    {
        using var sha = SHA256.Create();
        var files = new[] { "Seeds", "Models" }
            .Select(d => Path.Combine(projectDir, d))
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(projectDir, "appsettings*.json"))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var name = Encoding.UTF8.GetBytes(Path.GetRelativePath(projectDir, file).ToLowerInvariant());
            sha.TransformBlock(name, 0, name.Length, null, 0);
            var content = File.ReadAllBytes(file);
            sha.TransformBlock(content, 0, content.Length, null, 0);
        }

        foreach (var (key, value) in seedEnvironment.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var bytes = Encoding.UTF8.GetBytes($"{key}={value}\n");
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }
}
