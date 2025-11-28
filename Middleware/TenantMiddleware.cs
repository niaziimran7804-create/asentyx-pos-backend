using System.Security.Claims;

namespace POS.Api.Middleware
{
    public class TenantContext
    {
        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
    }

    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var companyIdClaim = context.User.FindFirst("CompanyId");
                var branchIdClaim = context.User.FindFirst("BranchId");
                var userIdClaim = context.User.FindFirst("UserId");
                var roleClaim = context.User.FindFirst(ClaimTypes.Role);

                if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
                {
                    tenantContext.CompanyId = companyId;
                }

                if (branchIdClaim != null && int.TryParse(branchIdClaim.Value, out var branchId))
                {
                    tenantContext.BranchId = branchId;
                }

                tenantContext.UserId = userIdClaim?.Value;
                tenantContext.Role = roleClaim?.Value;
            }

            await _next(context);
        }
    }
}
