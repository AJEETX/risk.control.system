using Microsoft.AspNetCore.DataProtection;
using risk.control.system.AppConstant;

namespace risk.control.system.Services.Common
{
    public interface IEncryptionService
    {
        string ProtectId(long id);
        string ProtectData(string id);
        long UnprotectId(string protectedId);
        string UnprotectData(string protectedData);
    }
    internal class EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger) : IEncryptionService
    {
        private readonly ILogger<EncryptionService> _logger = logger;
        private readonly IDataProtector _protector = provider.CreateProtector(Applicationsettings.ENCRYPT_URL_QUERYSTRING);
        public string ProtectId(long id)
        {
            return _protector.Protect(id.ToString());
        }

        public string ProtectData(string id)
        {
            return _protector.Protect(id);
        }

        public long UnprotectId(string protectedId)
        {
            if (string.IsNullOrEmpty(protectedId)) return 0;

            try
            {
                string decrypted = _protector.Unprotect(protectedId);

                if (long.TryParse(decrypted, out long id))
                {
                    return id;
                }
            }
            catch
            {
                _logger.LogError("Failed to unprotect the ID: {ProtectedId}", protectedId);
                // Tampered or invalid token
            }

            return 0;
        }

        public string UnprotectData(string protectedData)
        {
            if (string.IsNullOrEmpty(protectedData)) return string.Empty;

            try
            {
                return _protector.Unprotect(protectedData);
            }
            catch
            {
                _logger.LogError("Failed to unprotect the data: {ProtectedData}", protectedData);
                // Tampered or invalid token
            }

            return string.Empty;
        }
    }
}
