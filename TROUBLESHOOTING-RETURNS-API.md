# Sale Returns - Troubleshooting "API Not Implemented" Error

## ?? Issue: Getting 404 "API Not Implemented" Error

**Error Message:** "API Not Implemented"  
**Your Payload:**
```json
{
  "returnType": "whole",
  "invoiceId": 1,
  "orderId": 1,
  "returnReason": "sdfg",
  "refundMethod": "Cash",
  "notes": "sdfg",
  "totalReturnAmount": 0.4
}
```

---

## ? **Solutions**

### **Solution 1: Check Your Frontend URL**

Make sure you're sending to the **correct endpoint**:

**? WRONG:**
```typescript
POST http://localhost:7000/api/returns  // Missing '/whole'
POST http://localhost:7000/api/return/whole  // Missing 's' in returns
```

**? CORRECT:**
```typescript
POST https://localhost:7000/api/returns/whole  // Correct!
```

---

### **Solution 2: Update Your Angular Service**

Make sure your `return.service.ts` has the correct method:

```typescript
// In return.service.ts
createWholeReturn(payload: any): Observable<any> {
  // ? CORRECT - includes '/whole'
  return this.http.post(`${this.baseUrl}/returns/whole`, payload);
  
  // ? WRONG - missing '/whole'
  // return this.http.post(`${this.baseUrl}/returns`, payload);
}
```

---

### **Solution 3: Check Your Component Code**

In your `returns.component.ts`:

```typescript
submitWholeReturn() {
  const payload = {
    returnType: 'whole',
    invoiceId: this.selectedInvoice.invoiceId,
    orderId: this.selectedInvoice.orderId,
    returnReason: this.returnForm.value.returnReason,
    refundMethod: this.returnForm.value.refundMethod,
    notes: this.returnForm.value.notes,
    totalReturnAmount: this.selectedInvoice.totalAmount
  };

  console.log('Whole Return Payload:', payload);

  // ? Use the correct service method
  this.returnService.createWholeReturn(payload).subscribe({
    next: (response) => {
      console.log('Whole return created:', response);
      this.toastr.success('Return created successfully');
      this.loadReturns();
      this.closeModal();
    },
    error: (error) => {
      console.error('Error creating whole return:', error);
      this.toastr.error(error.error?.error || 'Failed to create return');
    }
  });
}
```

---

## ?? **Testing the Endpoint**

### **Test with Postman or cURL:**

```http
POST https://localhost:7000/api/returns/whole
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "returnType": "whole",
  "invoiceId": 1,
  "orderId": 1,
  "returnReason": "Customer not satisfied",
  "refundMethod": "Cash",
  "notes": "Full refund requested",
  "totalReturnAmount": 150.00
}
```

**Expected Success Response (201 Created):**
```json
{
  "returnId": 1,
  "returnType": "whole",
  "invoiceId": 1,
  "orderId": 1,
  "returnDate": "2025-11-26T12:30:00Z",
  "returnStatus": "Pending",
  "totalReturnAmount": 150.00,
  "refundMethod": "Cash",
  "returnReason": "Customer not satisfied",
  "notes": "Full refund requested",
  "customerFullName": "John Doe",
  "processedBy": null,
  "processedDate": null,
  "message": "Whole bill return created successfully"
}
```

---

## ?? **Common Validation Errors**

### **Error 1: Invoice Not Found**
```json
{
  "error": "Invoice with ID 1 not found"
}
```
**Fix:** Make sure the invoice exists in the database

### **Error 2: Invoice Too Old**
```json
{
  "error": "Invoice is older than 14 days and cannot be returned"
}
```
**Fix:** Only invoices from the last 14 days can be returned

### **Error 3: Amount Mismatch**
```json
{
  "error": "Return amount must equal invoice total for whole returns"
}
```
**Fix:** `totalReturnAmount` must match the invoice total exactly

### **Error 4: Already Returned**
```json
{
  "error": "Invoice has already been fully returned"
}
```
**Fix:** You cannot create multiple whole returns for the same invoice

---

## ?? **Your Specific Issue**

Looking at your payload:
```json
{
  "totalReturnAmount": 0.4  // ?? This seems very low
}
```

**Check:**
1. Is the actual invoice total really `0.4`?
2. Make sure you're getting the correct `totalAmount` from the invoice
3. The return amount must **exactly match** the invoice total for whole returns

---

## ?? **Complete Angular Example**

### **return.service.ts**
```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReturnService {
  private baseUrl = `${environment.apiUrl}/returns`;

  constructor(private http: HttpClient) {}

  // ? Whole bill return
  createWholeReturn(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/whole`, payload);
  }

  // ? Partial return
  createPartialReturn(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/partial`, payload);
  }

  // Get all returns
  getAllReturns(): Observable<any[]> {
    return this.http.get<any[]>(this.baseUrl);
  }

  // Get return summary
  getReturnSummary(): Observable<any> {
    return this.http.get(`${this.baseUrl}/summary`);
  }
}
```

### **returns.component.ts** (excerpt)
```typescript
submitWholeReturn() {
  // Validate form
  if (!this.returnForm.valid) {
    this.toastr.error('Please fill all required fields');
    return;
  }

  // Build payload
  const payload = {
    returnType: 'whole',
    invoiceId: this.selectedInvoice.invoiceId,
    orderId: this.selectedInvoice.orderId,
    returnReason: this.returnForm.value.returnReason,
    refundMethod: this.returnForm.value.refundMethod,
    notes: this.returnForm.value.notes || '',
    totalReturnAmount: this.selectedInvoice.totalAmount  // Must match invoice total!
  };

  console.log('Submitting whole return:', payload);

  // Submit to API
  this.returnService.createWholeReturn(payload).subscribe({
    next: (response) => {
      console.log('Success response:', response);
      this.toastr.success(`Return #${response.returnId} created successfully`);
      this.closeModal();
      this.loadReturns();
    },
    error: (error) => {
      console.error('Error response:', error);
      const errorMessage = error.error?.error || 'Failed to create return';
      this.toastr.error(errorMessage, 'Error', { timeOut: 5000 });
    }
  });
}
```

---

## ?? **Quick Checklist**

Before submitting a return, verify:

- [ ] URL is `https://localhost:7000/api/returns/whole` (note the `/whole`)
- [ ] Authorization header is included with valid JWT token
- [ ] `invoiceId` exists and is from last 14 days
- [ ] `orderId` matches the invoice
- [ ] `totalReturnAmount` equals invoice total exactly
- [ ] `returnReason` is at least 5 characters
- [ ] `refundMethod` is one of: "Cash", "Card", "Store Credit"
- [ ] Invoice hasn't been returned already

---

## ?? **Debug Steps**

### **1. Check Browser Network Tab**

Open Developer Tools (F12) ? Network tab:
- Look for the request to `/api/returns/whole`
- Check the Request URL (should be `.../api/returns/whole`)
- Check Status Code (404 means wrong URL, 400 means validation error)
- Check Response body for error message

### **2. Check Backend Logs**

Look at Visual Studio Output window ? Debug:
- Should see: `"Received whole return request: InvoiceId=1, OrderId=1, Amount=0.4"`
- If you don't see this log, the request isn't reaching the endpoint

### **3. Verify Service Registration**

Make sure in `Program.cs`:
```csharp
builder.Services.AddScoped<IReturnService, ReturnService>();
```

---

## ? **Expected Flow**

1. **Frontend:** User clicks "Return Full Invoice"
2. **Frontend:** Builds payload with invoice details
3. **Frontend:** Sends `POST` to `https://localhost:7000/api/returns/whole`
4. **Backend:** Receives request in `ReturnsController.CreateWholeReturn`
5. **Backend:** Validates invoice exists and is eligible
6. **Backend:** Creates return record, restores inventory, creates accounting entry
7. **Backend:** Returns 201 Created with return details
8. **Frontend:** Shows success message and refreshes list

---

## ?? **Still Having Issues?**

If you're still getting "API Not Implemented":

1. **Restart the backend** - Stop and start the debugging session
2. **Clear browser cache** - Hard refresh (Ctrl+Shift+R)
3. **Check the URL carefully** - Make sure it ends with `/whole`
4. **Verify migration was applied** - Check database has `Returns` table
5. **Check CORS settings** - Make sure frontend URL is allowed

---

## ?? **Success Indicators**

You'll know it's working when:
- ? Network tab shows `201 Created` status
- ? Response contains `returnId`
- ? Success toast message appears
- ? Return appears in the returns list
- ? Backend logs show: "Whole return created successfully with ID X"

---

**Need more help?** Check the backend logs in Visual Studio Output window for detailed error messages!
