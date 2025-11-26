# ? Customer Address Added to Aging Report

## ?? What Changed

The customer aging report now includes the **customer's address** in the response.

---

## ? Updated Response

### **Get Customer Aging Report**
```
GET /api/ledger/customer/{customerId}/aging?asOfDate=2025-08-31
```

**Response (Updated):**
```json
{
  "customerId": 15,
  "customerName": "35 Chatkhara CA",
  "customerPhone": "+92-300-1234567",
  "customerEmail": "chatkhara@example.com",
  "customerAddress": "Islamabad",          // ? NEW - Customer's address
  "currentBalance": 642144.00,
  "days0To30": 167526.00,
  "days31To60": 154433.00,
  "days61To90": 147615.00,
  "days91Plus": 172670.00,
  "totalOutstanding": 642144.00,
  "lastTransactionDate": "2025-08-15",
  "totalInvoices": 12,
  "unpaidInvoices": 8
}
```

---

### **Get Complete Aging Report (All Customers)**
```
GET /api/ledger/aging-report?asOfDate=2025-08-31
```

**Response (Updated):**
```json
{
  "reportDate": "2025-11-26",
  "asOfDate": "2025-08-31",
  "customers": [
    {
      "customerId": 15,
      "customerName": "35 Chatkhara CA",
      "customerPhone": "+92-300-1234567",
      "customerEmail": "chatkhara@example.com",
      "customerAddress": "Islamabad",        // ? NEW
      "currentBalance": 642144.00,
      "days0To30": 167526.00,
      "days31To60": 154433.00,
      "days61To90": 147615.00,
      "days91Plus": 172670.00,
      "totalOutstanding": 642144.00
    },
    {
      "customerId": 17,
      "customerName": "B.M. Sweet",
      "customerPhone": "+92-301-9876543",
      "customerEmail": "bmsweet@example.com",
      "customerAddress": "Karachi",          // ? NEW
      "currentBalance": 1053700.00,
      "days0To30": 575800.00,
      "days31To60": 497900.00,
      "days61To90": 0.00,
      "days91Plus": 0.00,
      "totalOutstanding": 1053700.00
    }
  ],
  "totalDays0To30": 2839480.50,
  "totalDays31To60": 1007543.50,
  "totalDays61To90": 160114.00,
  "totalDays91Plus": 393034.00,
  "grandTotal": 4378172.00,
  "totalCustomers": 45,
  "customersWithBalance": 32
}
```

---

## ?? Updated Sample Aging Report Table

```
Customer Account Balance                                         8/31/2025

Customer # | Customer Name          | Address     | Phone            | 0-30 Days  | 31-60 Days | 61-90 Days | 91+ Days   | Acct Balance
-----------|------------------------|-------------|------------------|------------|------------|------------|------------|-------------
15         | 35 Chatkhara CA        | Islamabad   | +92-300-1234567  | 167,526.00 | 154,433.00 | 147,615.00 | 172,670.00 | 642,144.00
17         | B.M. Sweet             | Karachi     | +92-301-9876543  | 575,800.00 | 497,900.00 | 0.00       | 0.00       | 1,053,700.00
370        | Bin Sultan (C.A Chock) | Lahore      | +92-311-2222222  | 0.00       | 0.00       | 0.00       | 7,470.00   | 7,470.00
137        | Cafe Blue (Kachak H.)  | Islamabad   | +92-321-3333333  | 8,000.00   | 0.00       | 0.00       | 0.00       | 8,000.00
...        | ...                    | ...         | ...              | ...        | ...        | ...        | ...        | ...
-----------|------------------------|-------------|------------------|------------|------------|------------|------------|-------------
TOTAL      |                        |             |                  | 2,839,480  | 1,007,543  | 160,114.00 | 393,034.00 | 4,378,172.00
```

---

## ?? Frontend Display Example

### **TypeScript Interface (Updated):**
```typescript
interface CustomerAging {
  customerId: number;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  customerAddress: string;      // ? NEW
  currentBalance: number;
  days0To30: number;
  days31To60: number;
  days61To90: number;
  days91Plus: number;
  totalOutstanding: number;
  lastTransactionDate?: string;
  totalInvoices: number;
  unpaidInvoices: number;
}
```

### **HTML Table Display:**
```html
<table class="aging-report">
  <thead>
    <tr>
      <th>Customer #</th>
      <th>Customer Name</th>
      <th>Address</th>              <!-- ? NEW -->
      <th>Phone</th>
      <th>0-30 Days</th>
      <th>31-60 Days</th>
      <th>61-90 Days</th>
      <th>91+ Days</th>
      <th>Total Outstanding</th>
    </tr>
  </thead>
  <tbody>
    <tr *ngFor="let customer of agingReport.customers">
      <td>{{ customer.customerId }}</td>
      <td>{{ customer.customerName }}</td>
      <td>{{ customer.customerAddress }}</td>   <!-- ? NEW -->
      <td>{{ customer.customerPhone }}</td>
      <td>{{ customer.days0To30 | currency }}</td>
      <td>{{ customer.days31To60 | currency }}</td>
      <td>{{ customer.days61To90 | currency }}</td>
      <td>{{ customer.days91Plus | currency }}</td>
      <td>{{ customer.totalOutstanding | currency }}</td>
    </tr>
  </tbody>
</table>
```

---

## ?? Address Source

The address is populated from the **Customer's CurrentCity** field in the Users table:
- Field: `Users.CurrentCity`
- Maps to: `CustomerAddress` in aging report
- Example values: "Islamabad", "Karachi", "Lahore", "Rawalpindi", etc.

---

## ? Changes Summary

### **Files Modified:**
1. ? `DTOs/CustomerLedgerDto.cs` - Added `CustomerAddress` property to `CustomerAgingDto`
2. ? `Services/LedgerService.cs` - Updated `GetCustomerAgingAsync` to populate address

### **API Endpoints Affected:**
- ? `GET /api/ledger/customer/{id}/aging` - Returns address
- ? `GET /api/ledger/aging-report` - Returns address for all customers
- ? `GET /api/ledger/outstanding` - Returns address for customers with balances

---

## ?? Testing

### **Test with cURL:**
```bash
# Get aging report for specific customer
curl -X GET "https://localhost:7001/api/ledger/customer/15/aging" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get complete aging report
curl -X GET "https://localhost:7001/api/ledger/aging-report?asOfDate=2025-08-31" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### **Expected Response:**
```json
{
  "customerName": "35 Chatkhara CA",
  "customerAddress": "Islamabad",    // ? Address should be present
  "customerPhone": "+92-300-1234567",
  "days0To30": 167526.00,
  ...
}
```

---

## ?? Benefits

1. **? Complete Customer Info** - Address visible in aging report
2. **? Better Identification** - Helps identify customer location
3. **? Collection Priority** - Can prioritize by geographic area
4. **? Report Completeness** - Matches paper form format
5. **? Export Ready** - Address included in Excel/PDF exports

---

**Status:** ? **IMPLEMENTED**  
**Build:** ? **SUCCESSFUL**  
**API:** ? **UPDATED**  
**Ready:** ? **FOR TESTING**

?? **Customer address is now included in all aging reports!** ??
