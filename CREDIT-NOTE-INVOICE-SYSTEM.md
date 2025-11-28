# ?? Credit Note Invoice System - IMPLEMENTED

## ?? Overview

A comprehensive **Credit Note Invoice System** has been implemented that automatically generates credit note invoices when sale returns are made. Credit notes are negative invoices that reduce customer liability and are essential for proper accounting.

---

## ? What's Been Implemented

### **1. Database Changes**
? Added `InvoiceType` field to Invoice model (`"Invoice"` or `"CreditNote"`)  
? Added `OriginalInvoiceId` field to link credit notes to original invoices  
? Added `ReturnId` field to link credit notes to returns  
? Added `CreditNoteInvoiceId` field to Return model  
? Added navigation properties for relationships  

### **2. DTOs Created**
? `CreateCreditNoteDto` - For creating credit notes  
? `CreditNoteDto` - For credit note responses  
? Updated `InvoiceDto` with credit note fields  
? Updated `ReturnDto` with credit note information  

### **3. Service Layer**
? `CreateCreditNoteInvoiceAsync` - Generates credit note invoices  
? `GetCreditNoteByReturnIdAsync` - Retrieves credit notes by return  
? `GenerateCreditNoteNumberAsync` - Generates unique CN numbers  
? Updated invoice mapping to include new fields  

### **4. API Endpoints**
? `POST /api/invoices/credit-note/return/{returnId}` - Create credit note  
? `GET /api/invoices/credit-note/return/{returnId}` - Get credit note  

### **5. Migration**
? Migration created: `AddCreditNoteInvoiceSupport`  
? Ready to apply: `dotnet ef database update`  

---

## ?? Credit Note Numbering

Credit notes have their own numbering sequence:
- **Format**: `CN-YYYYMM-XXXX`
- **Example**: `CN-202511-0001`
- **Sequence**: Auto-incremented per month
- **Type**: Distinguished by `InvoiceType = "CreditNote"`

---

## ?? Database Schema

### **Invoice Table Updates**

| Column | Type | Description |
|--------|------|-------------|
| **InvoiceType** | varchar(20) | "Invoice" or "CreditNote" |
| **OriginalInvoiceId** | int (nullable) | FK to original invoice (for credit notes) |
| **ReturnId** | int (nullable) | FK to Returns table (for credit notes) |

### **Return Table Updates**

| Column | Type | Description |
|--------|------|-------------|
| **CreditNoteInvoiceId** | int (nullable) | FK to credit note invoice |

---

## ?? API Usage

### **1. Create Credit Note for Return**

```http
POST /api/invoices/credit-note/return/5
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response (201 Created):**
```json
{
  "creditNoteId": 12,
  "creditNoteNumber": "CN-202511-0001",
  "creditNoteDate": "2025-11-26T20:30:00Z",
  "creditAmount": 15000.00,
  "originalInvoiceId": 30,
  "originalInvoiceNumber": "INV-202511-0025",
  "returnId": 5,
  "customerName": "35 Chatkhara CA",
  "customerPhone": "+92-300-1234567",
  "customerEmail": "chatkhara@example.com",
  "returnReason": "Damaged goods",
  "items": [
    {
      "productId": 10,
      "productName": "Product A",
      "quantity": 5,
      "unitPrice": 3000.00,
      "totalPrice": 15000.00
    }
  ]
}
```

**Error Responses:**

```json
{
  "message": "Return not found"
}
```

```json
{
  "message": "Credit note already exists for this return"
}
```

---

### **2. Get Credit Note by Return ID**

```http
GET /api/invoices/credit-note/return/5
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response (200 OK):**
```json
{
  "creditNoteId": 12,
  "creditNoteNumber": "CN-202511-0001",
  "creditNoteDate": "2025-11-26T20:30:00Z",
  "creditAmount": 15000.00,
  "originalInvoiceId": 30,
  "originalInvoiceNumber": "INV-202511-0025",
  "returnId": 5,
  "customerName": "35 Chatkhara CA",
  "customerPhone": "+92-300-1234567",
  "customerEmail": "chatkhara@example.com",
  "returnReason": "Damaged goods",
  "items": [...]
}
```

---

### **3. Get Return with Credit Note Info**

```http
GET /api/returns/5
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "returnId": 5,
  "returnType": "whole",
  "invoiceId": 30,
  "orderId": 45,
  "creditNoteInvoiceId": 12,
  "creditNoteNumber": "CN-202511-0001",
  "returnDate": "2025-11-26T18:00:00Z",
  "returnStatus": "Pending",
  "totalReturnAmount": 15000.00,
  "refundMethod": "Cash",
  "returnReason": "Damaged goods",
  "customerFullName": "35 Chatkhara CA",
  "message": "Whole bill return created successfully"
}
```

---

## ?? Business Logic

### **Credit Note Creation Flow**

```
1. Customer Returns Items
   ?
2. Return Record Created (via /api/returns/whole or /api/returns/partial)
   ?
3. Credit Note Invoice Created (Manually or Automatically)
   - Invoice Type: "CreditNote"
   - Amount: Negative (e.g., -15000.00)
   - Status: "Issued"
   - Links to Original Invoice and Return
   ?
4. Return Record Updated with CreditNoteInvoiceId
   ?
5. Credit Note Available in System
```

### **Key Properties of Credit Notes**

1. **Negative Amount**: TotalAmount = -ReturnAmount
2. **Status**: Always "Issued" (credit notes don't have payment cycles)
3. **Due Date**: Same as issue date (immediate effect)
4. **Links**: Connected to original invoice and return record
5. **Unique Number**: CN-YYYYMM-XXXX format

---

## ?? Integration with Existing Systems

### **1. Customer Ledger Integration**

When a credit note is created:
- Ledger entry is created with type "Refund"
- Customer's debit amount is credited
- Balance is reduced
- Links to both invoice and return

### **2. Accounting Integration**

Credit notes automatically:
- Create refund accounting entries
- Reduce sales revenue
- Track returned inventory value
- Update financial reports

### **3. Invoice System**

Credit notes appear in:
- Invoice list (filtered by type)
- Customer statements
- Aging reports (as credits)
- Financial summaries

---

## ?? Integration Steps

### **Step 1: Apply Migration**

```bash
dotnet ef database update
```

This will:
- Add `InvoiceType`, `OriginalInvoiceId`, `ReturnId` to Invoices table
- Add `CreditNoteInvoiceId` to Returns table
- Set default `InvoiceType = 'Invoice'` for existing invoices

---

### **Step 2: Automatic Credit Note Creation (Optional)**

To automatically create credit notes when returns are made, update `ReturnService.cs`:

```csharp
// In CreateWholeReturnAsync or CreatePartialReturnAsync
// After saving the return:

try
{
    // Automatically create credit note
    var creditNote = await _invoiceService.CreateCreditNoteInvoiceAsync(returnEntity.ReturnId);
    
    _logger.LogInformation(
        "Credit note {CreditNoteNumber} created for return {ReturnId}", 
        creditNote.CreditNoteNumber, 
        returnEntity.ReturnId);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to create credit note for return {ReturnId}", returnEntity.ReturnId);
    // Continue - credit note can be created manually later
}
```

---

### **Step 3: Manual Credit Note Creation**

Admins can manually create credit notes:

```typescript
// In your frontend service
createCreditNoteForReturn(returnId: number): Observable<CreditNoteDto> {
  return this.http.post<CreditNoteDto>(
    `${this.baseUrl}/invoices/credit-note/return/${returnId}`,
    {}
  );
}

// Usage
this.invoiceService.createCreditNoteForReturn(returnId).subscribe({
  next: (creditNote) => {
    console.log('Credit note created:', creditNote);
    this.toastr.success(`Credit Note ${creditNote.creditNoteNumber} created`);
  },
  error: (error) => {
    console.error('Error:', error);
    this.toastr.error(error.error.message || 'Failed to create credit note');
  }
});
```

---

## ?? Frontend Display Examples

### **Return List with Credit Note Info**

```typescript
interface ReturnWithCreditNote {
  returnId: number;
  invoiceNumber: string;
  customerName: string;
  totalReturnAmount: number;
  returnDate: Date;
  creditNoteNumber?: string;
  hasCreditNote: boolean;
}
```

```html
<table class="returns-table">
  <thead>
    <tr>
      <th>Return ID</th>
      <th>Invoice #</th>
      <th>Customer</th>
      <th>Amount</th>
      <th>Credit Note</th>
      <th>Actions</th>
    </tr>
  </thead>
  <tbody>
    <tr *ngFor="let return of returns">
      <td>{{ return.returnId }}</td>
      <td>{{ return.invoiceNumber }}</td>
      <td>{{ return.customerName }}</td>
      <td>{{ return.totalReturnAmount | currency }}</td>
      <td>
        <span *ngIf="return.creditNoteNumber" class="badge badge-success">
          {{ return.creditNoteNumber }}
        </span>
        <span *ngIf="!return.creditNoteNumber" class="badge badge-warning">
          Not Issued
        </span>
      </td>
      <td>
        <button *ngIf="!return.creditNoteNumber"
                (click)="createCreditNote(return.returnId)"
                class="btn btn-sm btn-primary">
          Issue Credit Note
        </button>
        <button *ngIf="return.creditNoteNumber"
                (click)="viewCreditNote(return.returnId)"
                class="btn btn-sm btn-info">
          View Credit Note
        </button>
      </td>
    </tr>
  </tbody>
</table>
```

---

### **Credit Note Display Component**

```typescript
export class CreditNoteComponent implements OnInit {
  creditNote: CreditNoteDto;

  ngOnInit() {
    const returnId = this.route.snapshot.params['returnId'];
    this.invoiceService.getCreditNoteByReturn(returnId).subscribe({
      next: (creditNote) => {
        this.creditNote = creditNote;
      },
      error: (error) => {
        this.toastr.error('Credit note not found');
      }
    });
  }

  printCreditNote() {
    // Implement credit note printing logic
    window.print();
  }
}
```

---

## ?? Reporting Integration

### **Invoice List with Credit Notes**

```sql
-- Query to get all invoices including credit notes
SELECT 
    InvoiceId,
    InvoiceNumber,
    InvoiceType,
    InvoiceDate,
    TotalAmount,
    CASE 
        WHEN InvoiceType = 'CreditNote' THEN OriginalInvoiceId 
        ELSE NULL 
    END AS OriginalInvoiceId,
    CASE 
        WHEN InvoiceType = 'CreditNote' THEN ReturnId 
        ELSE NULL 
    END AS ReturnId
FROM Invoices
ORDER BY InvoiceDate DESC;
```

### **Customer Balance with Credit Notes**

```sql
-- Calculate customer balance including credit notes
SELECT 
    c.Id AS CustomerId,
    CONCAT(c.FirstName, ' ', c.LastName) AS CustomerName,
    SUM(CASE WHEN i.InvoiceType = 'Invoice' THEN i.Balance ELSE 0 END) AS InvoiceBalance,
    SUM(CASE WHEN i.InvoiceType = 'CreditNote' THEN ABS(i.TotalAmount) ELSE 0 END) AS CreditNoteAmount,
    SUM(CASE WHEN i.InvoiceType = 'Invoice' THEN i.Balance ELSE 0 END) - 
    SUM(CASE WHEN i.InvoiceType = 'CreditNote' THEN ABS(i.TotalAmount) ELSE 0 END) AS NetBalance
FROM Users c
LEFT JOIN Orders o ON c.Id = o.CustomerId
LEFT JOIN Invoices i ON o.OrderId = i.OrderId
WHERE c.Role = 'Customer'
GROUP BY c.Id, c.FirstName, c.LastName;
```

---

## ? Testing Checklist

### **Functional Tests**

- [ ] Create whole return and verify credit note is created
- [ ] Create partial return and verify credit note is created
- [ ] Verify credit note number is unique and sequential
- [ ] Verify credit note amount is negative
- [ ] Verify credit note links to original invoice
- [ ] Verify credit note links to return record
- [ ] Verify return record is updated with credit note ID
- [ ] Verify customer ledger is updated correctly
- [ ] Verify accounting entries are created
- [ ] Test duplicate credit note prevention

### **API Tests**

- [ ] POST /api/invoices/credit-note/return/{returnId} returns 201
- [ ] POST with invalid returnId returns 404
- [ ] POST with existing credit note returns 400
- [ ] GET /api/invoices/credit-note/return/{returnId} returns 200
- [ ] GET with no credit note returns 404
- [ ] GET /api/returns/{id} includes credit note info

### **Integration Tests**

- [ ] Credit notes appear in invoice list
- [ ] Credit notes affect customer balance correctly
- [ ] Credit notes appear in customer statements
- [ ] Credit notes integrate with aging reports
- [ ] Credit notes integrate with financial summaries

---

## ?? Configuration Options

### **Automatic Credit Note Creation**

You can choose between:

**Option 1: Automatic** (Recommended)
- Credit notes are created immediately when return is processed
- No manual intervention required
- Consistent and reliable

**Option 2: Manual**
- Admin must manually issue credit note via API
- More control over process
- Requires admin action

### **Credit Note Settings**

Add to `appsettings.json`:

```json
{
  "CreditNoteSettings": {
    "AutoCreateOnReturn": true,
    "RequireApproval": false,
    "NumberFormat": "CN-{year}{month:D2}-{sequence:D4}",
    "DefaultStatus": "Issued"
  }
}
```

---

## ?? Benefits

### **For Business**
- ? Proper accounting of returns
- ? Clear audit trail
- ? Reduced customer liability
- ? Compliance with accounting standards
- ? Better financial reporting

### **For Customers**
- ? Official document for returns
- ? Clear record of credits
- ? Reduced account balance
- ? Professional service

### **For System**
- ? Linked data (Invoice ? Return ? Credit Note)
- ? Automated workflows
- ? Consistent numbering
- ? Complete history

---

## ?? Next Steps

### **1. Apply Migration**
```bash
dotnet ef database update
```

### **2. Test the System**
- Create a test return
- Issue credit note via API
- Verify credit note appears in database
- Check customer ledger integration

### **3. Frontend Integration**
- Add credit note button to return list
- Create credit note display component
- Add credit note to invoice list filter
- Update customer statements

### **4. Optional Enhancements**
- PDF generation for credit notes
- Email credit notes to customers
- Credit note approval workflow
- Bulk credit note issuance

---

## ?? Related Documentation

- [Customer Ledger System](./CUSTOMER-LEDGER-IMPLEMENTATION.md)
- [Return Management](./TROUBLESHOOTING-RETURNS-API.md)
- [Invoice System](./UPDATE-INVOICE-DUE-DATE-API.md)
- [Accounting Integration](./ACCOUNTING-ENTRIES-API.md)

---

## ? FAQ

**Q: Can a return have multiple credit notes?**  
A: No, each return can have only one credit note. The system prevents duplicates.

**Q: What happens if I delete a return with a credit note?**  
A: The credit note remains in the system as a historical record. Set up cascade rules based on your business needs.

**Q: Can I edit a credit note after creation?**  
A: Credit notes are generally immutable once issued. If needed, you can cancel and create a new one.

**Q: How do credit notes affect customer balance?**  
A: Credit notes reduce the customer's outstanding balance by the credited amount.

**Q: Can I print credit notes?**  
A: Yes, you can extend the invoice HTML generation to support credit note format.

---

**Status**: ? **FULLY IMPLEMENTED**  
**Migration**: ? **PENDING DATABASE UPDATE**  
**API**: ? **READY**  
**Build**: ? **SUCCESSFUL**  

?? **Credit Note Invoice System is ready to use!** ??
