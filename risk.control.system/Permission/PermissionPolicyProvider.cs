using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using risk.control.system.AppConstant;

namespace risk.control.system.Permission
{
    internal class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        // Use the interface rather than the concrete implementation for better abstraction
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
            _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() =>
            _fallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // 1. Add a null check for safety
            if (string.IsNullOrEmpty(policyName))
            {
                return _fallbackPolicyProvider.GetPolicyAsync(policyName);
            }

            // 2. Custom permission logic
            if (policyName.StartsWith(Applicationsettings.PERMISSION, StringComparison.OrdinalIgnoreCase))
            {
                var policy = new AuthorizationPolicyBuilder();
                policy.AddRequirements(new PermissionRequirement(policyName));
                return Task.FromResult<AuthorizationPolicy?>(policy.Build());
            }

            // 3. Fallback to default behavior
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}