using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationService
    {
        List<ClaimsInvestigation> GetAll();
        Task Create(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument);
        Task Assign(string userEmail, List<string> claimsInvestigations);
    }
    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private readonly ApplicationDbContext _context;

        public ClaimsInvestigationService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task Assign(string userEmail, List<string> claims)
        {
            if (claims is not null && claims.Count > 0)
            {
                var cases2Assign = _context.ClaimsInvestigation.Where(v => claims.Contains(v.ClaimsInvestigationCaseId));
                foreach (var claimsInvestigation in cases2Assign)
                {
                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = userEmail;
                    claimsInvestigation.CurrentUserId = userEmail;
                    claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
                    claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId;
                }
                _context.UpdateRange(cases2Assign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task Create(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument)
        {
            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (claimsInvestigation is not null)
            {
                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = applicationUser.Email;
                claimsInvestigation.CurrentUserId = applicationUser.Email;
                claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId;
                claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId;
                if (claimDocument is not null)
                {
                    var messageDocumentFileName = Path.GetFileNameWithoutExtension(claimDocument.FileName);
                    var extension = Path.GetExtension(claimDocument.FileName);
                    claimsInvestigation.Document = claimDocument;
                    using var dataStream = new MemoryStream();
                    await claimsInvestigation.Document.CopyToAsync(dataStream);
                    claimsInvestigation.DocumentImage = dataStream.ToArray();
                }
                _context.ClaimsInvestigation.Add(claimsInvestigation);
                await _context.SaveChangesAsync();
            }
        }

        public List<ClaimsInvestigation> GetAll()
        {
            throw new NotImplementedException();
        }
    }
}
