using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Common
{
    public interface ICaseNotesService
    {
        Task<bool> SubmitNotes(string userEmail, long claimId, string notes);
    }

    internal class CaseNotesService : ICaseNotesService
    {
        private readonly ApplicationDbContext _context;

        public CaseNotesService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<bool> SubmitNotes(string userEmail, long claimId, string notes)
        {
            var caseTask = await _context.Investigations
               .Include(c => c.CaseNotes)
               .FirstOrDefaultAsync(c => c.Id == claimId);
            caseTask.CaseNotes.Add(new CaseNote
            {
                Comment = notes,
                SenderEmail = userEmail,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                UpdatedBy = userEmail
            });
            _context.Investigations.Update(caseTask);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}