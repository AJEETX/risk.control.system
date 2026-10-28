using Microsoft.AspNetCore.DataProtection;
using risk.control.system.AppConstant;

namespace risk.control.system.Services.Common
{
    public interface IEncryptionService
    {
        string ProtectId(long id);
        long UnprotectId(string protectedId);
    }
    internal class EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger) : IEncryptionService
    {
        private readonly ILogger<EncryptionService> _logger = logger;
        private readonly IDataProtector _protector = provider.CreateProtector(Applicationsettings.ENCRYPT_URL_QUERYSTRING);
        public string ProtectId(long id)
        {
            return _protector.Protect(id.ToString());
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
    }
}
