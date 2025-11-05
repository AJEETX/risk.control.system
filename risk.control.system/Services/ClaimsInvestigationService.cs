using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationService
    {
        Task<bool> SubmitNotes(string userEmail, long claimId, string notes);
    }

    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private readonly ApplicationDbContext _context;

        public ClaimsInvestigationService(ApplicationDbContext context)
        {
            this._context = context;
        }
        public async Task<bool> SubmitNotes(string userEmail, long claimId, string notes)
        {
            var claim = _context.Investigations
               .Include(c => c.CaseNotes)
               .FirstOrDefault(c => c.Id == claimId);
            claim.CaseNotes.Add(new CaseNote
            {
                Comment = notes,
                Sender = userEmail,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                UpdatedBy = userEmail
            });
            _context.Investigations.Update(claim);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}