using Microsoft.AspNetCore.Authorization;
using risk.control.system.Seeds;
using System.Linq;
using System.Threading.Tasks;

namespace risk.control.system.Permission
{
internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
        
    public PermissionAuthorizationHandler()
    {

    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User == null)
        {
            return;
        }
            var permissionss = context.User.Claims.Where(x => x.Type == ApplicationOption.PERMISSION &&
                                                            x.Value == requirement.Permission &&
                                                            x.Issuer == "LOCAL AUTHORITY");
        if (permissionss.Any())
        {
            context.Succeed(requirement);
            return;
        }
    }
}
}