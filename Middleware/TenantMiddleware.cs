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
            // Priority 1: Try to get from JWT claims (authenticated users)
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

            // Priority 2: Fallback to headers (if not set from claims)
            // This allows overriding or providing tenant context via headers
            if (!tenantContext.CompanyId.HasValue && context.Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader))
            {
                if (int.TryParse(companyIdHeader.FirstOrDefault(), out var headerCompanyId))
                {
                    tenantContext.CompanyId = headerCompanyId;
                }
            }

            if (!tenantContext.BranchId.HasValue && context.Request.Headers.TryGetValue("X-Branch-Id", out var branchIdHeader))
            {
                if (int.TryParse(branchIdHeader.FirstOrDefault(), out var headerBranchId))
                {
                    tenantContext.BranchId = headerBranchId;
                }
            }

            await _next(context);
        }
    }
}
