# ? Sales Returns Handling in Accounting & Sales Chart

## ?? Overview

The accounting system now properly tracks and displays sales returns (refunds) across all financial reports, summaries, and charts.

---

## ? What's Been Updated

### **1. DTOs Enhanced**

#### **FinancialSummaryDto**
```csharp
public class FinancialSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalRefunds { get; set; }  // ? NEW
    public decimal NetProfit { get; set; }      // Now: Income - Expenses - Refunds
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal CashBalance { get; set; }
    public string Period { get; set; }
}
```

#### **DailySalesDto**
```csharp
public class DailySalesDto
{
    public string Date { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalRefunds { get; set; }   // ? NEW
    public decimal NetProfit { get; set; }       // Now: Sales - Expenses - Refunds
    public decimal CashSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal AverageOrderValue { get; set; }
}
```

#### **SalesGraphDto**
```csharp
public class SalesGraphDto
{
    public List<string> Labels { get; set; }
    public List<decimal> SalesData { get; set; }
    public List<decimal> ExpensesData { get; set; }
    public List<decimal> RefundsData { get; set; }  // ? NEW
    public List<decimal> ProfitData { get; set; }    // Now: Sales - Expenses - Refunds
    public List<int> OrdersData { get; set; }
}
```

---

## ?? Updated Methods

### **1. GetFinancialSummaryAsync**

**What Changed:**
- ? Now includes `TotalRefunds` in response
- ? NetProfit calculation: `Income - Expenses - Refunds`

**Example Response:**
```json
GET /api/accounting/summary?startDate=2025-11-01&endDate=2025-11-26
{
  "totalIncome": 15000.00,
  "totalExpenses": 3500.00,
  "totalRefunds": 450.00,     // ? NEW - Total refunds in period
  "netProfit": 11050.00,      // 15000 - 3500 - 450
  "totalSales": 15000.00,
  "totalPurchases": 0.00,
  "cashBalance": 11050.00,
  "period": "2025-11-01 to 2025-11-26"
}
```

---

### **2. GetDailySalesAsync**

**What Changed:**
- ? Queries refunds from `AccountingEntries` where `EntryType = Refund`
- ? Includes `TotalRefunds` per day
- ? NetProfit calculation: `Sales - Expenses - Refunds`

**Example Response:**
```json
GET /api/accounting/daily-sales?days=7
[
  {
    "date": "2025-11-20",
    "totalSales": 1250.00,
    "totalOrders": 15,
    "totalExpenses": 250.00,
    "totalRefunds": 100.00,   // ? NEW - Refunds for this day
    "netProfit": 900.00,       // 1250 - 250 - 100
    "cashSales": 800.00,
    "cardSales": 450.00,
    "averageOrderValue": 83.33
  },
  {
    "date": "2025-11-21",
    "totalSales": 1890.00,
    "totalOrders": 22,
    "totalExpenses": 310.00,
    "totalRefunds": 0.00,      // No refunds this day
    "netProfit": 1580.00,      // 1890 - 310 - 0
    "cashSales": 1200.00,
    "cardSales": 690.00,
    "averageOrderValue": 85.91
  }
]
```

---

### **3. GetSalesGraphAsync**

**What Changed:**
- ? Queries refunds from `AccountingEntries` where `EntryType = Refund`
- ? Includes `RefundsData` array for each day
- ? ProfitData calculation: `Sales - Expenses - Refunds`
- ? All arrays have equal length (includes zeros for days with no data)

**Example Response:**
```json
GET /api/accounting/sales-graph?startDate=2025-11-20&endDate=2025-11-26
{
  "labels": ["Nov 20", "Nov 21", "Nov 22", "Nov 23", "Nov 24", "Nov 25", "Nov 26"],
  "salesData": [1250.00, 1890.00, 1650.00, 2200.00, 1900.00, 2100.00, 2300.00],
  "expensesData": [250.00, 310.00, 280.00, 340.00, 290.00, 320.00, 350.00],
  "refundsData": [100.00, 0.00, 50.00, 120.00, 0.00, 75.00, 0.00],  // ? NEW
  "profitData": [900.00, 1580.00, 1320.00, 1740.00, 1610.00, 1705.00, 1950.00],  // ? Updated
  "ordersData": [15, 22, 20, 28, 24, 26, 30]
}
```

**Calculation Example (Nov 20):**
```
Sales:    1250.00
Expenses:  250.00
Refunds:   100.00
          --------
Profit:    900.00  // 1250 - 250 - 100
```

---

## ?? Frontend Chart Integration

### **Chart Configuration**

The frontend can now display refunds as a separate line/bar:

```typescript
// sales-graph.component.ts
this.chartOptions = {
  series: [
    {
      name: 'Sales',
      data: graphData.salesData,
      color: '#667eea'  // Purple
    },
    {
      name: 'Expenses',
      data: graphData.expensesData,
      color: '#f56565'  // Red
    },
    {
      name: 'Refunds',       // ? NEW
      data: graphData.refundsData,  // ? NEW
      color: '#ed8936'  // Orange
    },
    {
      name: 'Profit',
      data: graphData.profitData,
      color: '#48bb78'  // Green (now includes refunds deduction)
    }
  ],
  xaxis: {
    categories: graphData.labels
  }
};
```

---

## ?? How Refunds Are Recorded

### **Refund Entry Creation**

When a return is approved in the Returns system, an accounting entry is created:

```csharp
// From ReturnService when return is completed
var entry = new AccountingEntry
{
    EntryType = EntryType.Refund,    // ? Tracked as Refund
    Amount = returnAmount,
    Description = $"Return #{returnId} - Invoice #{invoiceId}",
    PaymentMethod = refundMethod,     // Cash, Card, Store Credit
    Category = "Sales Return",
    EntryDate = DateTime.UtcNow,
    CreatedBy = userName,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
```

### **Refund Sources**

Refunds are tracked from two places:

1. **Order Cancellations** (if previously paid)
   ```csharp
   // When order status changes to "Cancelled" and was "Paid"
   await _accountingService.CreateRefundEntryFromOrderAsync(orderId, userName);
   ```

2. **Returns System** (when return is completed)
   ```csharp
   // When return status changes to "Completed"
   _context.AccountingEntries.Add(refundEntry);
   ```

---

## ?? API Endpoints Summary

| Endpoint | Includes Refunds? | How? |
|----------|-------------------|------|
| `GET /api/accounting/summary` | ? Yes | `TotalRefunds` field |
| `GET /api/accounting/daily-sales` | ? Yes | `TotalRefunds` per day |
| `GET /api/accounting/sales-graph` | ? Yes | `RefundsData` array |
| `GET /api/accounting/entries` | ? Yes | Filter by `entryType=Refund` |
| `GET /api/accounting/payment-methods` | ? No | Only sales |
| `GET /api/accounting/top-products` | ? No | Only sales |

---

## ?? Testing Refunds Impact

### **Test Scenario 1: Create a Return**

1. **Create an order:**
   ```json
   POST /api/orders
   {
     "customerFullName": "Test Customer",
     "items": [{"productId": 1, "quantity": 2, "unitPrice": 50}]
   }
   ```
   Result: `totalAmount: 100.00`

2. **Pay the order:**
   ```json
   PUT /api/orders/45/status
   {
     "status": "Paid",
     "orderStatus": "Paid"
   }
   ```
   Result: Creates accounting entry with `EntryType.Sale`, Amount: 100.00

3. **Create a return:**
   ```json
   POST /api/returns/whole
   {
     "invoiceId": 30,
     "returnReason": "Defective",
     "refundMethod": "Cash"
   }
   ```
   Result: Creates accounting entry with `EntryType.Refund`, Amount: 100.00

4. **Check financial summary:**
   ```json
   GET /api/accounting/summary
   {
     "totalSales": 100.00,
     "totalRefunds": 100.00,
     "netProfit": 0.00  // 100 - 0 - 100
   }
   ```

### **Test Scenario 2: Verify Sales Graph**

1. **Before refund:**
   ```json
   GET /api/accounting/sales-graph?startDate=2025-11-26&endDate=2025-11-26
   {
     "salesData": [100.00],
     "expensesData": [0.00],
     "refundsData": [0.00],
     "profitData": [100.00]  // 100 - 0 - 0
   }
   ```

2. **After refund:**
   ```json
   GET /api/accounting/sales-graph?startDate=2025-11-26&endDate=2025-11-26
   {
     "salesData": [100.00],
     "expensesData": [0.00],
     "refundsData": [100.00],  // ? Refund recorded
     "profitData": [0.00]       // 100 - 0 - 100
   }
   ```

---

## ?? Benefits

### **? Accurate Financial Reporting**
- Net profit now correctly accounts for refunds
- Separate tracking of refunds vs expenses
- Complete picture of daily financial performance

### **? Better Business Insights**
- See refund trends over time
- Identify days with high refund rates
- Analyze refund impact on profitability

### **? Audit Trail**
- All refunds recorded in `AccountingEntries`
- Linked to original orders/invoices
- Includes refund method and reason

### **? Chart Visualization**
- Separate refunds line on graphs
- Clear visual of refund impact
- Profit line shows true net profit

---

## ?? Querying Refunds

### **Get All Refunds**
```
GET /api/accounting/entries?entryType=Refund&page=1&limit=50
```

### **Get Refunds for Date Range**
```
GET /api/accounting/entries?entryType=Refund&startDate=2025-11-01&endDate=2025-11-26
```

### **Get Refunds by Payment Method**
```
GET /api/accounting/entries?entryType=Refund&paymentMethod=Cash
```

---

## ?? Database Impact

### **AccountingEntries Table**

Refunds are stored with:
```sql
EntryType = 3  -- EntryType.Refund enum value
Amount = [refund amount]
Description = 'Refund for Order #XX' or 'Return #XX'
PaymentMethod = 'Cash', 'Card', 'Store Credit'
Category = 'Sales Refund'
EntryDate = [date of refund]
```

### **Query Example**
```sql
-- Get total refunds for November 2025
SELECT SUM(Amount) as TotalRefunds
FROM AccountingEntries
WHERE EntryType = 3  -- Refund
AND EntryDate >= '2025-11-01'
AND EntryDate < '2025-12-01';
```

---

## ?? Frontend Updates Needed

### **1. Update Chart Component**

```typescript
// Add refunds series to chart
interface SalesGraphData {
  labels: string[];
  salesData: number[];
  expensesData: number[];
  refundsData: number[];  // ? NEW
  profitData: number[];
  ordersData: number[];
}
```

### **2. Display Refunds Summary**

```html
<!-- In financial summary card -->
<div class="summary-card">
  <div class="label">Total Sales</div>
  <div class="value">{{ summary.totalSales | currency }}</div>
</div>

<div class="summary-card">
  <div class="label">Total Expenses</div>
  <div class="value text-red">{{ summary.totalExpenses | currency }}</div>
</div>

<div class="summary-card refunds">  <!-- ? NEW -->
  <div class="label">Total Refunds</div>
  <div class="value text-orange">{{ summary.totalRefunds | currency }}</div>
</div>

<div class="summary-card">
  <div class="label">Net Profit</div>
  <div class="value text-green">{{ summary.netProfit | currency }}</div>
  <div class="formula">Sales - Expenses - Refunds</div>
</div>
```

---

## ? Implementation Checklist

- [x] **DTOs updated** - Added TotalRefunds and RefundsData fields
- [x] **GetFinancialSummaryAsync** - Includes TotalRefunds
- [x] **GetDailySalesAsync** - Includes TotalRefunds per day
- [x] **GetSalesGraphAsync** - Includes RefundsData array
- [x] **Profit calculations** - Subtract refunds: `Sales - Expenses - Refunds`
- [x] **Build successful** - No compilation errors
- [ ] **Frontend updated** - Add refunds to charts and summaries
- [ ] **Testing complete** - Verify refunds appear correctly

---

## ?? Key Formula

### **Net Profit Calculation**
```
Net Profit = Total Sales - Total Expenses - Total Refunds
```

This formula is now consistently applied across:
- ? Financial Summary
- ? Daily Sales Report
- ? Sales Graph
- ? All accounting reports

---

**Status:** ? **IMPLEMENTED**  
**Build:** ? **SUCCESSFUL**  
**Backend:** ? **READY**  
**Frontend:** ? **NEEDS UPDATE**

?? **Sales returns are now properly tracked and displayed in all accounting reports!** ??
