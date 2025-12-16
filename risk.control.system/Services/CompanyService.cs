using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;

namespace risk.control.system.Services
{
    public interface ICompanyService
    {
        Task<object[]> GetCompanies();
    }
    internal class CompanyService : ICompanyService
    {
        private readonly IWebHostEnvironment env;
        private readonly ApplicationDbContext context;

        public CompanyService(IWebHostEnvironment env, ApplicationDbContext context)
        {
            this.env = env;
            this.context = context;
        }
        public async Task<object[]> GetCompanies()
        {
            var companies = context.ClientCompany.
                 Where(v => !v.Deleted)
                 .Include(v => v.Country)
                 .Include(v => v.PinCode)
                 .Include(v => v.District)
                 .Include(v => v.State).OrderBy(o => o.Name);

            var result =
                companies.Select(u =>
                new
                {
                    Id = u.ClientCompanyId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, u.DocumentUrl)))),
                    Domain = $"<a href='/ClientCompany/Details?Id={u.ClientCompanyId}'>" + u.Email + "</a>",
                    Name = u.Name,
                    //Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Code,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    Active = u.Status.GetEnumDisplayName(),
                    UpdatedBy = u.UpdatedBy,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();
            companies.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(null, false);
            return result;
        }
    }
}
