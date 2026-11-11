using Microsoft.AspNetCore.DataProtection;
using risk.control.system.AppConstant;

namespace risk.control.system.Services.Common
{
    public interface IEncryptionService
    {
        string ProtectId(long id);
        string ProtectIdWithExpiry(long caseId, TimeSpan lifetime);
        long UnprotectId(string protectedId);
        long UnprotectIdWithExpiry(string protectedToken);
    }
    internal class EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger) : IEncryptionService
    {
        private readonly ILogger<EncryptionService> _logger = logger;
        private readonly IDataProtector _protector = provider.CreateProtector(Applicationsettings.ENCRYPT_URL_QUERYSTRING);
        private readonly ITimeLimitedDataProtector _timeLimitedProtector = provider.CreateProtector(Applicationsettings.ENCRYPT_URL_QUERYSTRING).ToTimeLimitedDataProtector();
        public string ProtectId(long id)
        {
            return _protector.Protect(id.ToString());
        }

        public string ProtectIdWithExpiry(long caseId, TimeSpan lifetime)
        {
            return _timeLimitedProtector.Protect(caseId.ToString(), lifetime);
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
                return 0;
            }

            return 0;
        }

        public long UnprotectIdWithExpiry(string protectedToken)
        {
            try
            {
                var unprotectedBytes = _timeLimitedProtector.Unprotect(protectedToken);
                return long.TryParse(unprotectedBytes, out var id) ? id : 0;
            }
            catch (Exception)
            {
                _logger.LogError("Failed to unprotect the ID: {protectedToken}", protectedToken);
                return 0;
            }
        }
    }
}
