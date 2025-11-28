# ?? Quick Guide: Automatic Credit Note Creation

## ? Feature Overview

When a return is marked as **"Completed"**, the system **automatically creates a credit note invoice**.

---

## ?? Simple Workflow

```
1. Create Return
   POST /api/returns/whole or /api/returns/partial
   Status: "Pending"
   
2. Admin Marks Return as Completed
   PUT /api/returns/{id}/status
   Body: { "returnStatus": "Completed" }
   
3. System Automatically:
   ? Creates Credit Note Invoice (CN-YYYYMM-XXXX)
   ? Links Credit Note to Return
   ? Updates Return record
   ? Logs all operations
   
4. Done! ?
   Return has credit note
   Credit note available in system
```

---

## ?? API Examples

### **1. Create a Return**
```http
POST https://localhost:7000/api/returns/whole
Authorization: Bearer YOUR_TOKEN

{
  "returnType": "whole",
  "invoiceId": 30,
  "orderId": 45,
  "returnReason": "Damaged goods",
  "refundMethod": "Cash",
  "totalReturnAmount": 15000.00
}
```

### **2. Complete the Return (Credit Note Auto-Created)**
```http
PUT https://localhost:7000/api/returns/5/status
Authorization: Bearer YOUR_TOKEN

{
  "returnStatus": "Completed"
}
```

### **3. View Return with Credit Note**
```http
GET https://localhost:7000/api/returns/5
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "returnId": 5,
  "creditNoteInvoiceId": 12,
  "creditNoteNumber": "CN-202511-0001",
  "returnStatus": "Completed",
  "message": "Return completed and credit note created"
}
```

---

## ?? Frontend Example

```typescript
// Complete return with automatic credit note creation
completeReturn(returnId: number) {
  this.returnService.updateReturnStatus(returnId, { 
    returnStatus: 'Completed' 
  }).subscribe({
    next: () => {
      this.toastr.success('Return completed! Credit note created automatically.');
      this.refreshReturns();
    }
  });
}
```

---

## ??? Error Handling

**If credit note creation fails:**
- ? Return status still updates to "Completed"
- ? Credit note creation is logged as error
- ?? Admin can manually create credit note later via:
  ```http
  POST /api/invoices/credit-note/return/{returnId}
  ```

---

## ? Benefits

? **Zero Manual Work** - One click completes return and creates credit note  
? **Consistent** - Never forget to create credit notes  
? **Reliable** - Handles errors gracefully  
? **Auditable** - Full logging of all operations  

---

## ?? Ready to Use!

The feature is **live** and **ready** to use. Simply:

1. Apply the migration: `dotnet ef database update`
2. Restart your application
3. Complete a return - credit note will be created automatically! ??

---

**Questions?** Check the full documentation: `AUTOMATIC-CREDIT-NOTE-ON-RETURN-COMPLETE.md`
