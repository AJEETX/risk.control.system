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
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ExtractorService> logger;

        public ExtractorService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ExtractorService> logger)
        {
            _contextFactory = contextFactory;
            this.logger = logger;
        }

        public async Task<PinCode?> GetPinCodeAsync(int code, string district, long countryId)
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();
                var pincodeDetils = await _context.PinCode.AsNoTracking()
                .Include(p => p.District).Include(p => p.State).Include(p => p.Country)
                .FirstOrDefaultAsync(p => p.Code == code &&
                    p.District.Name.ToLower().Contains(district.ToLower()) &&
                    p.CountryId == countryId);
                return pincodeDetils;
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
                using var _context = await _contextFactory.CreateDbContextAsync();

                var allRelations = await _context.BeneficiaryRelation.AsNoTracking().ToListAsync();
                var relations = allRelations.FirstOrDefault(b => b.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                       ?? allRelations.FirstOrDefault();
                return relations;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching BeneficiaryRelation for Code: {Code}", code);
                return null;
            }
        }
    }
}