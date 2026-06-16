using Microsoft.AspNetCore.Authorization;

namespace PCS_API.Handlers;

public class PermissionRequirementHandler(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public class PermissionHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<PermissionRequirementHandler>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirementHandler requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var branchIdHeader = httpContext?.Request.Headers["X-Branch-Id"].ToString();

        if (!string.IsNullOrEmpty(branchIdHeader) && int.TryParse(branchIdHeader, out var branchId))
        {
            var hasGlobal = context.User.HasClaim("scoped_permission", $"{requirement.Permission}:global");
            var hasBranch = context.User.HasClaim("scoped_permission", $"{requirement.Permission}:branch:{branchId}");

            if (hasGlobal || hasBranch)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }
        else
        {
            if (context.User.HasClaim("permission", requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}