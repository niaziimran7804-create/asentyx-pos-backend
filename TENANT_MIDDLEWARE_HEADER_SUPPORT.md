# Tenant Middleware - Header Support Update

## Problem
The frontend was sending `X-Branch-Id: 3` header, but `_tenantContext.BranchId` was still null because the middleware only read from JWT claims.

## Solution
Updated `TenantMiddleware` to support **both JWT claims AND HTTP headers** with a priority system.

---

## How It Works Now

### Priority Order:
1. **JWT Claims** (Primary) - Read from authenticated user's token
2. **HTTP Headers** (Fallback) - Read from request headers if not in claims

### Updated Middleware Logic:

```csharp
public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
{
    // Priority 1: Try to get from JWT claims (authenticated users)
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var companyIdClaim = context.User.FindFirst("CompanyId");
        var branchIdClaim = context.User.FindFirst("BranchId");
        
        if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
            tenantContext.CompanyId = companyId;
        
        if (branchIdClaim != null && int.TryParse(branchIdClaim.Value, out var branchId))
            tenantContext.BranchId = branchId;
    }

    // Priority 2: Fallback to headers (if not set from claims)
    if (!tenantContext.CompanyId.HasValue && 
        context.Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader))
    {
        if (int.TryParse(companyIdHeader.FirstOrDefault(), out var headerCompanyId))
            tenantContext.CompanyId = headerCompanyId;
    }

    if (!tenantContext.BranchId.HasValue && 
        context.Request.Headers.TryGetValue("X-Branch-Id", out var branchIdHeader))
    {
        if (int.TryParse(branchIdHeader.FirstOrDefault(), out var headerBranchId))
            tenantContext.BranchId = headerBranchId;
    }

    await _next(context);
}
```

---

## Frontend Usage

### Option 1: JWT Token Only (Recommended)
```typescript
// After login, just send the token
const response = await axios.get('/api/categories/main', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

// The middleware will extract BranchId from the token automatically
```

### Option 2: HTTP Headers (Fallback/Override)
```typescript
// Send headers explicitly (useful for testing or special cases)
const response = await axios.get('/api/categories/main', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Company-Id': '1',
    'X-Branch-Id': '3'
  }
});
```

### Option 3: Headers Without Token (Unauthenticated Requests)
```typescript
// For endpoints that don't require authentication
// but need tenant context
const response = await axios.get('/api/public/products', {
  headers: {
    'X-Company-Id': '1',
    'X-Branch-Id': '3'
  }
});
```

---

## Use Cases

### ? Use Case 1: Normal Authenticated Requests
**User logs in ? Token contains BranchId ? Middleware extracts from token**

```http
GET /api/categories/main
Authorization: Bearer eyJhbGc...
```
- Token contains: `{ "BranchId": "2", "CompanyId": "1" }`
- Middleware sets: `_tenantContext.BranchId = 2`

### ? Use Case 2: Token Missing BranchId (Company Admin)
**Company Admin user ? Token has no BranchId ? Sends header as override**

```http
GET /api/categories/main
Authorization: Bearer eyJhbGc...
X-Branch-Id: 3
```
- Token contains: `{ "CompanyId": "1" }` (no BranchId)
- Middleware reads header: `_tenantContext.BranchId = 3`

### ? Use Case 3: Testing/Development
**Developer testing ? Sends headers directly**

```http
GET /api/categories/main
Authorization: Bearer eyJhbGc...
X-Company-Id: 1
X-Branch-Id: 3
```
- Headers override: `_tenantContext.CompanyId = 1`, `_tenantContext.BranchId = 3`

### ? Use Case 4: Public/Anonymous Endpoints
**Public product catalog ? No authentication ? Headers only**

```http
GET /api/public/products
X-Branch-Id: 3
```
- No token needed
- Middleware reads header: `_tenantContext.BranchId = 3`

---

## Security Notes

### ?? Important Considerations:

1. **JWT Claims Take Priority**
   - If both JWT claim and header exist, **JWT claim wins**
   - This prevents header tampering when user is authenticated

2. **Header Fallback Only When Not in Claims**
   - Headers are only used when the value is NOT in JWT claims
   - Prevents unauthorized branch switching for authenticated users

3. **Validation Still Required**
   - Services should still validate tenant access
   - Just because BranchId is set doesn't mean user has access

4. **For Production**
   - Consider adding header validation (e.g., validate CompanyId matches user's token)
   - May want to restrict header-based access to specific endpoints

---

## Testing

### Test 1: JWT Token with BranchId
```bash
# Login first
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userId":"admin","password":"admin123"}'

# Use token (BranchId extracted from token)
curl -X GET http://localhost:5000/api/categories/main \
  -H "Authorization: Bearer YOUR_TOKEN"
```
**Expected:** `_tenantContext.BranchId` = value from token

### Test 2: Header Override
```bash
# Send header along with token
curl -X GET http://localhost:5000/api/categories/main \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "X-Branch-Id: 3"
```
**Expected:** 
- If token has BranchId ? uses token value
- If token has no BranchId ? uses header value (3)

### Test 3: Headers Only (No Token)
```bash
# Send headers without authentication
curl -X GET http://localhost:5000/api/categories/main \
  -H "X-Company-Id: 1" \
  -H "X-Branch-Id: 3"
```
**Expected:** 
- `_tenantContext.CompanyId = 1`
- `_tenantContext.BranchId = 3`
- (May fail if endpoint requires authentication)

---

## Angular/TypeScript Examples

### Setup Axios Interceptor (Recommended)
```typescript
// http.service.ts
import axios from 'axios';

axios.interceptors.request.use(config => {
  const token = localStorage.getItem('token');
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  
  // Optional: Add headers as fallback
  if (user.companyId) {
    config.headers['X-Company-Id'] = user.companyId;
  }
  
  if (user.branchId) {
    config.headers['X-Branch-Id'] = user.branchId;
  }
  
  return config;
});

export default axios;
```

### Manual Header Addition
```typescript
// category.service.ts
export class CategoryService {
  getMainCategories() {
    const branchId = this.authService.getBranchId();
    
    return this.http.get('/api/categories/main', {
      headers: {
        'X-Branch-Id': branchId?.toString() || ''
      }
    });
  }
}
```

---

## Migration Notes

### Before (Old Behavior):
- ? Only JWT claims were read
- ? Headers were ignored
- ? No way to override or provide tenant context via headers

### After (New Behavior):
- ? JWT claims are primary source
- ? Headers provide fallback mechanism
- ? Supports both authenticated and unauthenticated scenarios
- ? Backwards compatible (JWT-based auth still works exactly the same)

---

## Status

- ? **Code Updated:** `Middleware/TenantMiddleware.cs`
- ? **Build Status:** Successful
- ? **Backwards Compatible:** Yes (JWT claims still work as before)
- ? **New Feature:** Header-based tenant context as fallback

---

## Next Steps

1. **Restart the application** to apply changes
2. **Test with your frontend** sending `X-Branch-Id: 3` header
3. **Verify** that `_tenantContext.BranchId` is now populated
4. **Consider** updating login response to inform users about header support

---

## Quick Fix Summary

**Your Issue:** `x-branch-id: 3` in header but `_tenantContext.BranchId` was null

**Root Cause:** Middleware only read from JWT claims, not headers

**Solution:** Updated middleware to check headers as fallback when claims are not present

**Result:** Now supports both JWT claims AND HTTP headers! ??

---

## Example Frontend Code (Your Case)

```typescript
// This should now work!
const response = await fetch('/api/categories/main', {
  headers: {
    'Authorization': `Bearer ${yourToken}`,
    'X-Branch-Id': '3',  // This will now be read by middleware!
    'Content-Type': 'application/json'
  }
});

// _tenantContext.BranchId will now be 3
```
