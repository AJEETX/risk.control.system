using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Creator
{
    public interface IExtractorService
    {
        Task<PinCode?> GetPinCodeAsync(int code, string district, long countryId);

        Task<BeneficiaryRelation> GetRelationAsync(string code);
    }

    internal class ExtractorService : IExtractorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExtractorService> logger;

        public ExtractorService(ApplicationDbContext context, ILogger<ExtractorService> logger)
        {
            _context = context;
            this.logger = logger;
        }

        public async Task<PinCode?> GetPinCodeAsync(int code, string district, long countryId)
        {
            try
            {
                return await _context.PinCode
                .Include(p => p.District).Include(p => p.State).Include(p => p.Country)
                .FirstOrDefaultAsync(p => p.Code == code &&
                    p.District.Name.ToLower().Contains(district.ToLower()) &&
                    p.CountryId == countryId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching PinCode for Code: {Code}, District: {District}, CountryId: {CountryId}", code, district, countryId);
                return null;
            }
        }

        public async Task<BeneficiaryRelation> GetRelationAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code)) return await _context.BeneficiaryRelation.FirstOrDefaultAsync();
                return await _context.BeneficiaryRelation.FirstOrDefaultAsync(b => b.Code.ToLower() == code.ToLower())
                       ?? await _context.BeneficiaryRelation.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching BeneficiaryRelation for Code: {Code}", code);
                return null;
            }
        }
    }
}