# ? InvoiceService - Strict Branch Isolation Enforced

## ?? Overview

Updated **InvoiceService** to enforce **strict branch-level isolation**. All invoice operations now require a valid `BranchId` in `TenantContext`. If no BranchId is present, the service returns empty data or error responses.

---

## ?? Security Enhancement

### **Before**
- Users without branch assignment could see company-wide invoice data
- Company Admins could view invoices across all branches

### **After** ?
- **No BranchId = No Invoice Data**
- All users MUST be assigned to a specific branch
- Complete branch-level invoice isolation

---

## ?? Updated Methods (14 Total)

### **1. CreateInvoiceAsync()**
```csharp
// Throws exception if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    throw new InvalidOperationException("Cannot create invoice without branch context. User must be assigned to a branch.");
}

// Only fetches orders from user's branch
var order = await _context.Orders
    .FirstOrDefaultAsync(o => o.OrderId == createInvoiceDto.OrderId && o.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Cannot create invoices without branch assignment. Order must belong to user's branch.

---

### **2. GetInvoiceByIdAsync()**
```csharp
// Returns null if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    return null;
}

// Only returns invoice if order belongs to user's branch
var invoice = await _context.Invoices
    .FirstOrDefaultAsync(i => i.InvoiceId == id && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Can only view invoices from assigned branch.

---

### **3. GetInvoiceByOrderIdAsync()**
```csharp
// Returns null if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    return null;
}

// Only returns invoice if order belongs to user's branch
var invoice = await _context.Invoices
    .Include(i => i.Order)
    .FirstOrDefaultAsync(i => i.OrderId == orderId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Can only retrieve invoices for orders in assigned branch.

---

### **4. GetAllInvoicesAsync()**
```csharp
// Returns empty list if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    return Enumerable.Empty<InvoiceDto>();
}

// Only returns invoices from user's branch (last 14 days)
var query = _context.Invoices
    .Where(i => i.InvoiceDate >= fourteenDaysAgo && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Invoice lists are branch-specific. Empty list if no branch.

---

### **5. GetFilteredInvoicesAsync()**
```csharp
// Returns empty list if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    return Enumerable.Empty<InvoiceDto>();
}

// Only returns filtered invoices from user's branch
var query = _context.Invoices
    .Where(i => i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Filtered invoice searches are branch-specific.

---

### **6. GenerateInvoiceHtmlAsync()**
```csharp
// Throws exception if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    throw new InvalidOperationException("Cannot generate invoice HTML without branch context.");
}

// Gets invoice using branch-filtered method
var invoice = await GetInvoiceByIdAsync(invoiceId);
if (invoice == null)
    throw new ArgumentException("Invoice not found in your branch");
```

**Impact**: Cannot generate HTML for invoices without branch assignment.

---

### **7. GenerateBulkInvoiceHtmlAsync()**
```csharp
// Throws exception if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    throw new InvalidOperationException("Cannot generate bulk invoice HTML without branch context.");
}

// Uses branch-filtered GetInvoiceByIdAsync for each invoice
```

**Impact**: Cannot generate bulk invoice HTML without branch assignment.

---

### **8. UpdateInvoiceStatusByOrderIdAsync()**
```csharp
// Returns false if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    return false;
}

// Only updates if order belongs to user's branch
var invoice = await _context.Invoices
    .Include(i => i.Order)
    .FirstOrDefaultAsync(i => i.OrderId == orderId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Can only update invoice status for orders in assigned branch.

---

### **9. AddPaymentAsync()**
```csharp
// Throws exception if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    throw new InvalidOperationException("Cannot add payment without branch context.");
}

// Only allows payment if invoice belongs to user's branch
var invoice = await _context.Invoices
    .Include(i => i.Order)
    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Cannot add payments to invoices from other branches.

---

### **10. GetInvoicePaymentsAsync()**
```csharp
// Throws exception if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    throw new InvalidOperationException("Cannot view invoice payments without branch context.");
}

// Only returns payments if invoice belongs to user's branch
var invoice = await _context.Invoices
    .Include(i => i.Payments)
    .Include(i => i.Order)
    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Can only view payment summary for branch invoices.

---

### **11. GetAllPaymentsAsync()**
```csharp
// Returns empty list if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    return new List<InvoicePaymentDto>();
}

// Verifies invoice belongs to user's branch before returning payments
var invoiceExists = await _context.Invoices
    .Include(i => i.Order)
    .AnyAsync(i => i.InvoiceId == invoiceId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Can only view all payments for branch invoices.

---

### **12. UpdateDueDateAsync()**
```csharp
// Returns false if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    return false;
}

// Only updates if invoice belongs to user's branch
var invoice = await _context.Invoices
    .Include(i => i.Order)
    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Can only update due dates for branch invoices.

---

### **13. CreateCreditNoteInvoiceAsync()**
```csharp
// Throws exception if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    throw new InvalidOperationException("Cannot create credit note without branch context.");
}

// Only creates credit note if return belongs to user's branch
var returnEntity = await _context.Returns
    .FirstOrDefaultAsync(r => r.ReturnId == returnId && r.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Cannot create credit notes for returns from other branches.

---

### **14. GetCreditNoteByReturnIdAsync()**
```csharp
// Returns null if no BranchId
if (!_tenantContext.BranchId.HasValue)
{
    return null;
}

// Only returns credit note if return belongs to user's branch
var returnEntity = await _context.Returns
    .FirstOrDefaultAsync(r => r.ReturnId == returnId && r.BranchId == _tenantContext.BranchId.Value);
```

**Impact**: Can only view credit notes for branch returns.

---

### **Helper Methods (No Changes Needed)**
- `GenerateInvoiceNumberAsync()` - Global counter
- `GetShopConfigurationAsync()` - Global configuration
- `UpdateShopConfigurationAsync()` - Global configuration
- `GenerateInvoiceBody()` - HTML generation helper
- `GetInvoiceStyles()` - CSS helper
- `GenerateCreditNoteNumberAsync()` - Global counter

---

## ?? Behavior Matrix

| User Type | BranchId | Get Invoices | Create Invoice | Add Payment | Generate HTML | Credit Note |
|-----------|----------|--------------|----------------|-------------|---------------|-------------|
| **No Branch** | ? null | ?? Empty list | ?? Error | ?? Error | ?? Error | ?? Error |
| **Branch User** | ? 5 | ? Branch 5 only | ? For Branch 5 orders | ? Branch 5 invoices | ? Branch 5 invoices | ? Branch 5 returns |
| **Company Admin** | ? null | ?? Empty list | ?? Error | ?? Error | ?? Error | ?? Error |
| **Super Admin** | ? null | ?? Empty list | ?? Error | ?? Error | ?? Error | ?? Error |

**Note**: Even Super Admins need branch assignment to access invoices.

---

## ?? Test Scenarios

### **Scenario 1: User Without Branch Assignment**
```typescript
// User has no BranchId in token
// TenantContext.BranchId = null

GET /api/invoices
// Result: []
// Status: 200 OK

GET /api/invoices/1
// Result: null
// Status: 404 Not Found

POST /api/invoices
// Result: Error "Cannot create invoice without branch context"
// Status: 400 Bad Request

POST /api/invoices/1/payments
// Result: Error "Cannot add payment without branch context"
// Status: 400 Bad Request
```

### **Scenario 2: User With Branch Assignment**
```typescript
// User has BranchId = 5 in token
// TenantContext.BranchId = 5

GET /api/invoices
// Result: All invoices from Branch 5 (last 14 days)
// Status: 200 OK

GET /api/invoices/10
// Result: Invoice 10 (only if order is in Branch 5)
// Status: 200 OK or 404 Not Found

POST /api/invoices { orderId: 20 }
// Result: Invoice created (only if Order 20 is in Branch 5)
// Status: 201 Created or 400 Bad Request

POST /api/invoices/10/payments
// Result: Payment added (only if Invoice 10 is in Branch 5)
// Status: 201 Created or 400 Bad Request
```

### **Scenario 3: Cross-Branch Access Attempt**
```typescript
// User has BranchId = 5
// Invoice 100 belongs to Order in Branch 10

GET /api/invoices/100
// Result: null (invoice filtered out)
// Status: 404 Not Found

POST /api/invoices/100/payments
// Result: Error "Invoice not found in your branch"
// Status: 400 Bad Request

PUT /api/invoices/100/due-date
// Result: false
// Status: 404 Not Found
```

---

## ?? Security Benefits

1. **? Complete Branch Isolation**
   - All invoice data is branch-specific
   - No cross-branch access possible
   - Invoice payments are protected

2. **? Order Validation**
   - Invoices can only be created for orders in same branch
   - Cross-branch order access prevented
   - Data consistency maintained

3. **? Credit Note Protection**
   - Credit notes only for branch returns
   - Complete refund tracking per branch
   - No cross-branch credit manipulation

---

## ?? Important Implications

### **For All Users (Including Admins)**
- **Must be assigned to a branch** to access invoice data
- Cannot view or manage invoices from other branches
- Branch assignment is mandatory for all invoice operations

### **Response Behaviors**

#### **Empty Responses (No Error)**
These methods return empty data structures when no BranchId:
- `GetAllInvoicesAsync()` - Empty list
- `GetFilteredInvoicesAsync()` - Empty list
- `GetAllPaymentsAsync()` - Empty list
- `GetInvoiceByIdAsync()` - null
- `GetInvoiceByOrderIdAsync()` - null
- `GetCreditNoteByReturnIdAsync()` - null

#### **Error Responses (Throws Exception)**
These methods throw `InvalidOperationException` when no BranchId:
- `CreateInvoiceAsync()` - "Cannot create invoice without branch context"
- `GenerateInvoiceHtmlAsync()` - "Cannot generate invoice HTML without branch context"
- `GenerateBulkInvoiceHtmlAsync()` - "Cannot generate bulk invoice HTML without branch context"
- `AddPaymentAsync()` - "Cannot add payment without branch context"
- `GetInvoicePaymentsAsync()` - "Cannot view invoice payments without branch context"
- `CreateCreditNoteInvoiceAsync()` - "Cannot create credit note without branch context"

#### **False Responses (Silent Fail)**
These methods return `false` when no BranchId:
- `UpdateInvoiceStatusByOrderIdAsync()` - false
- `UpdateDueDateAsync()` - false

---

## ?? API Response Examples

### **Without BranchId**

```http
GET /api/invoices
Authorization: Bearer {token-without-branchId}
```

**Response 200 OK:**
```json
[]
```

---

```http
POST /api/invoices
Authorization: Bearer {token-without-branchId}
Content-Type: application/json

{
  "orderId": 123,
  "dueDate": "2024-12-31"
}
```

**Response 400 Bad Request:**
```json
{
  "message": "Cannot create invoice without branch context. User must be assigned to a branch."
}
```

---

```http
POST /api/invoices/10/payments
Authorization: Bearer {token-without-branchId}
Content-Type: application/json

{
  "amount": 100.00,
  "paymentMethod": "Cash"
}
```

**Response 400 Bad Request:**
```json
{
  "message": "Cannot add payment without branch context."
}
```

---

### **With BranchId**

```http
GET /api/invoices
Authorization: Bearer {token-with-branchId-5}
```

**Response 200 OK:**
```json
[
  {
    "invoiceId": 1,
    "invoiceNumber": "INV-202411-0001",
    "totalAmount": 150.00,
    "status": "Pending",
    "order": {
      "orderId": 123,
      "customerName": "John Doe"
    }
  }
]
```

---

```http
POST /api/invoices
Authorization: Bearer {token-with-branchId-5}
Content-Type: application/json

{
  "orderId": 123,
  "dueDate": "2024-12-31"
}
```

**Response 201 Created:**
```json
{
  "invoiceId": 2,
  "invoiceNumber": "INV-202411-0002",
  "orderId": 123,
  "totalAmount": 150.00,
  "status": "Pending"
}
```

---

## ?? Recommendations

### **1. User Assignment**
? **Always assign users to a branch**
```sql
UPDATE Users
SET BranchId = (
    SELECT TOP 1 BranchId FROM Branches 
    WHERE Branches.CompanyId = Users.CompanyId AND IsHeadOffice = 1
)
WHERE BranchId IS NULL;
```

### **2. Frontend Handling**
Update frontend to handle invoice restrictions:
```typescript
// Check if user has branch assignment
if (!currentUser.branchId) {
  // Show message: "You must be assigned to a branch to view invoices"
  return;
}

// Handle empty invoice lists
const invoices = await invoiceService.getAllInvoices();
if (invoices.length === 0) {
  // Show: "No invoices found for your branch"
}
```

### **3. Order Validation**
Ensure orders exist in branch before creating invoices:
```typescript
// Before creating invoice
const order = await orderService.getOrderById(orderId);
if (!order) {
  // Show: "Order not found or not accessible"
  return;
}

// Proceed with invoice creation
const invoice = await invoiceService.createInvoice({ orderId });
```

---

## ? Build Status

**Status**: ? No Errors  
**Hot Reload**: Available (debugging mode)  
**Methods Updated**: 14/14 in InvoiceService  

---

## ?? Related Files

- ? `Services/InvoiceService.cs` - Updated (14 methods)
- ? `Services/ProductService.cs` - Has strict branch isolation
- ? `Services/AccountingService.cs` - Has strict branch isolation
- ? `Services/UserService.cs` - Has company-level filtering
- ? `Services/BranchService.cs` - Has company-level filtering
- ?? `Services/OrderService.cs` - Needs update
- ?? `Services/ExpenseService.cs` - Needs update

---

## ?? Services Status Summary

| Service | Branch Isolation Status | Level |
|---------|------------------------|-------|
| ProductService | ? **Complete** | Branch |
| AccountingService | ? **Complete** | Branch |
| InvoiceService | ? **Complete** | Branch |
| UserService | ? **Complete** | Company |
| BranchService | ? **Complete** | Company |
| OrderService | ?? **Pending** | - |
| ExpenseService | ?? **Pending** | - |
| ReturnService | ?? **Pending** | - |

---

## ?? Summary

**InvoiceService now enforces strict branch-level isolation:**
- ? All users must have branch assignment for invoice access
- ? Empty responses for queries without branch (no errors)
- ? Error responses for create/modify operations without branch
- ? Complete branch-level invoice data isolation
- ? Payment operations are branch-protected
- ? Credit notes are branch-protected
- ? No cross-branch invoice access possible

**Security Level**: ?? **MAXIMUM SECURITY**

**All invoice operations are now branch-specific with complete data isolation!** ???

---

**Last Updated**: November 2024  
**Status**: ? Ready for Testing  
**Hot Reload**: Available for immediate testing
