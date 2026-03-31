using System.Net.Http.Headers;
using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface IAzureAdService
    {
        Task<string> ValidateAzureSignIn(TokenValidatedContext contex);
    }

    internal class AzureAdService : IAzureAdService
    {
        private static string graphApiEndpointWithParams = "https://graph.microsoft.com/v1.0/me?$select=id,displayName,mail,mobilePhone,department,city,state,country,streetAddress,postalCode,jobTitle";
        private readonly IConfiguration config;
        private readonly ILogger<AzureAdService> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public AzureAdService(
            IConfiguration config,
            ILogger<AzureAdService> logger,
            IHttpClientFactory httpClientFactory,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            this.config = config;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            _context = context;
            _userManager = userManager;
            this.signInManager = signInManager;
        }

        public async Task<string> ValidateAzureSignIn(TokenValidatedContext context)
        {
            try
            {
                var claimsIdentity = context.Principal!.Identity as ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    var email = context.Principal.FindFirstValue(ClaimTypes.Email) ?? context.Principal.FindFirstValue("preferred_username");
                    if (string.IsNullOrEmpty(email))
                    {
                        throw new Exception("Email claim not found in the token.");
                    }
                    var user = await _userManager.FindByEmailAsync(email);
                    var accessToken = context.TokenEndpointResponse!.AccessToken;
                    var httpClient = httpClientFactory.CreateClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var response = await httpClient.GetAsync(graphApiEndpointWithParams);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var azureADUserDetail = JsonConvert.DeserializeObject<AzureADUserDetail>(json);
                    user = await CreateOrUpdateExternalUserAsync(context.Principal, azureADUserDetail!);
                    var existing = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                    if (existing != null)
                    {
                        claimsIdentity.RemoveClaim(existing);
                    }
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    if (!claimsIdentity.HasClaim(c => c.Type == ClaimTypes.Email))
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, email));
                    }
                    var claimsToRemove = new[] { "amr", "aio", "uti", "rh", "xms_tcdt" };
                    foreach (var claimType in claimsToRemove)
                    {
                        var claim = claimsIdentity.FindFirst(claimType);
                        if (claim != null) claimsIdentity.RemoveClaim(claim);
                    }
                    context.Principal = await signInManager.CreateUserPrincipalAsync(user);
                    await SignInWithTimeoutAsync(user);
                    return email;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Azure AD sign-in validation.");
            }
            notifyService.Error($"Error during Azure AD sign-in validation.");
            return string.Empty;
        }

        private async Task SignInWithTimeoutAsync(ApplicationUser user)
        {
            var timeout = config["SESSION_TIMEOUT_SEC"] ?? "900";
            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(double.Parse(timeout))
            };
            await signInManager.SignInAsync(user, properties);
        }

        private async Task<ApplicationUser> CreateOrUpdateExternalUserAsync(ClaimsPrincipal principal, AzureADUserDetail azureADUserDetail)
        {
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("preferred_username") ?? principal.FindFirstValue(ClaimTypes.Upn);
            if (string.IsNullOrEmpty(email)) return null!;
            var azureRole = principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("roles");
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!string.IsNullOrEmpty(azureRole) && !currentRoles.Contains(azureRole))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await EnsureRoleExistsAndAssign(user, azureRole);
                }
                return user;
            }

            user = await CreateUser(email, principal, azureADUserDetail, azureRole!);

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"User creation failed: {errors}");
            }
            await EnsureRoleExistsAndAssign(user, azureRole!);
            return user;
        }
        private async Task<ApplicationUser> CreateUser(string email, ClaimsPrincipal principal, AzureADUserDetail azureADUserDetail, string azureRole)
        {
            var countryCode = ExtractCountryCode(principal);
            countryCode = countryCode.ToLower();
            var country = await _context.Country.FirstOrDefaultAsync(c => c.Code.ToLower() == countryCode) ?? throw new Exception($"Location configuration missing for country: {countryCode}");
            var pincode = await _context.PinCode.Include(d => d.District).Include(s => s.State).Include(c => c.Country).FirstOrDefaultAsync(p => p.CountryId == country!.CountryId && p.Code == azureADUserDetail.PostalCode) ?? throw new Exception($"Location configuration missing for country: {countryCode}");
            var cityName = azureADUserDetail.City?.ToLower();
            var district = await _context.District.FirstOrDefaultAsync(d => d.Name.ToLower() == cityName) ?? throw new Exception($"Location configuration missing for cityName: {cityName}");
            ClientCompany? company = RoleGroups.CompanyRoles.Contains(azureRole) ? await _context.ClientCompany.FirstOrDefaultAsync(c => !c.Deleted && c.CountryId == country!.CountryId) : null;
            Vendor? vendor = RoleGroups.AgencyRoles.Contains(azureRole) ? await _context.Vendor.FirstOrDefaultAsync(v => !v.Deleted) : null;
            var appRole = Enum.TryParse<AppRoles>(azureRole, out var parsedRole) ? parsedRole : AppRoles.GUEST;
            return new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? "",
                LastName = principal.FindFirstValue(ClaimTypes.Surname) ?? "",
                PhoneNumber = principal.FindFirstValue(ClaimTypes.MobilePhone) ?? azureADUserDetail.MobilePhone.TrimStart('0'),
                Active = true,
                EmailConfirmed = true,
                CountryId = country!.CountryId,
                DistrictId = district!.DistrictId,
                StateId = pincode.StateId,
                PinCodeId = pincode.PinCodeId,
                VendorId = vendor?.VendorId,
                ClientCompanyId = company?.ClientCompanyId,
                Role = appRole,
                Addressline = azureADUserDetail.StreetAddress,
                Password = Applicationsettings.TestingData
            };
        }
        private string ExtractCountryCode(ClaimsPrincipal principal)
        {
            return principal.FindFirstValue("ctry")
                   ?? principal.FindFirstValue("tenant_ctry")
                   ?? "IN"; // Default fallback
        }

        private async Task EnsureRoleExistsAndAssign(ApplicationUser user, string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return;

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName
                });
            }
            await _userManager.AddToRoleAsync(user, roleName);
        }
    }
}