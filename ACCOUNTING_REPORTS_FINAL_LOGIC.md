# ?? Accounting Reports - Final Logic Implementation

## ? Changes Applied

All accounting reports now consistently use the correct logic:
- **Total Sales** = From Orders table (includes pending/partial payments)
- **Cash Balance** = From Income entries only (actual cash received)
- **Net Profit** = Based on Total Sales (business profitability)

---

## ?? Updated Methods

### 1. GetFinancialSummaryAsync ?

**Changes:**
- Added `cashInHand` variable that ONLY includes `EntryType.Income`
- `cashBalance` now uses `cashInHand` instead of `totalIncome`
- `totalIncome` still includes both Income and Sale (for reporting)

**Code:**
```csharp
// Total Income = Includes Income + Sale entries (for reporting)
var totalIncome = await accountingQuery
    .Where(e => e.EntryType == EntryType.Income || e.EntryType == EntryType.Sale)
    .SumAsync(e => (decimal?)e.Amount) ?? 0;

// Cash In Hand = ONLY Income entries (actual payments received)
var cashInHand = await accountingQuery
    .Where(e => e.EntryType == EntryType.Income)
    .SumAsync(e => (decimal?)e.Amount) ?? 0;

// Total Sales = From Orders table (includes pending/partial)
var totalSales = await ordersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

// Cash Balance = Cash received - Expenses - Refunds
var cashBalance = cashInHand - totalExpenses - totalRefunds;

// Net Profit = Total Sales - Expenses - Refunds
var netProfit = totalSales - totalExpenses - totalRefunds;
```

---

### 2. GetDailySalesAsync ?

**Changes:**
- Now gets `TotalSalesAmount` from Orders table
- Gets cash received from `EntryType.Income` only
- `TotalSales` = Order amounts (includes pending)
- `CashSales`/`CardSales` = Actual cash received
- `NetProfit` = Based on total sales

**Code:**
```csharp
// Get order count and total sales amounts from Orders
var ordersData = await ordersQuery
    .GroupBy(o => o.Date.Date)
    .Select(g => new
    {
        Date = g.Key,
        TotalOrders = g.Count(),
        TotalSalesAmount = g.Sum(o => o.TotalAmount)  // ? All order amounts
    })
    .ToListAsync();

// Get actual cash received from Income entries only
var incomeQuery = _context.AccountingEntries
    .Where(e => e.EntryDate >= startDate && 
           e.EntryType == EntryType.Income &&  // ? Income only
           e.BranchId == _tenantContext.BranchId.Value);

var incomeData = await incomeQuery
    .GroupBy(e => e.EntryDate.Date)
    .Select(g => new
    {
        Date = g.Key,
        TotalIncome = g.Sum(e => e.Amount),
        CashIncome = g.Where(e => e.PaymentMethod == "Cash").Sum(e => e.Amount),
        CardIncome = g.Where(e => e.PaymentMethod != "Cash" && e.PaymentMethod != null).Sum(e => e.Amount)
    })
    .ToListAsync();

// Build result
result.Add(new DailySalesDto
{
    Date = date.ToString("yyyy-MM-dd"),
    TotalSales = totalSales,              // ? From orders (includes pending)
    TotalOrders = totalOrders,
    TotalExpenses = totalExpenses,
    TotalRefunds = totalRefunds,
    NetProfit = totalSales - totalExpenses - totalRefunds,  // ? Based on sales
    CashSales = income?.CashIncome ?? 0,  // ? Actual cash received
    CardSales = income?.CardIncome ?? 0,  // ? Actual card received
    AverageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0
});
```

---

### 3. GetSalesGraphAsync ?

**Changes:**
- Now includes `PartiallyPaid` orders in query
- Sales data shows total order amounts (includes pending)

**Code:**
```csharp
// Get sales data from Orders (includes all orders - pending/partial)
var salesQueryGraph = _context.Orders
    .Where(o => o.Date >= startDate && o.Date <= endDate.AddDays(1).AddTicks(-1) && 
           (o.OrderStatus == "Completed" || 
            o.OrderStatus == "Paid" || 
            o.OrderStatus == "PartiallyPaid") &&  // ? Added PartiallyPaid
           o.BranchId == _tenantContext.BranchId.Value);

var salesData = await salesQueryGraph
    .GroupBy(o => o.Date.Date)
    .Select(g => new
    {
        Date = g.Key,
        TotalSales = g.Sum(o => o.TotalAmount),  // ? Total order amounts
        TotalOrders = g.Count()
    })
    .ToDictionaryAsync(x => x.Date, x => x);
```

---

## ?? Data Flow

### Financial Summary:

```
Orders Table:
?? Order 1: $1,500 (Paid)
?? Order 2: $2,000 (PartiallyPaid $500)
?? Order 3: $800 (Completed, not paid)
    ?
Total Sales = $4,300

AccountingEntries (Income only):
?? Payment 1: $1,500
?? Payment 2: $500
    ?
Cash In Hand = $2,000

Expenses = $400
Refunds = $50

Results:
?? Total Sales: $4,300 (all orders)
?? Total Income: $2,000 (Income entries only)
?? Cash Balance: $1,550 ($2,000 - $400 - $50)
?? Net Profit: $3,850 ($4,300 - $400 - $50)
```

---

### Daily Sales:

```
Day: 2025-01-29

Orders on this day:
?? Order 1: $1,000 (Paid)
?? Order 2: $1,500 (PartiallyPaid $300)
?? Order 3: $500 (Completed, not paid)
    ?
Total Sales = $3,000

Income entries on this day:
?? Payment 1: $1,000 (Cash)
?? Payment 2: $300 (Card)
    ?
Total Income = $1,300

Results:
?? Total Sales: $3,000 (all orders)
?? Cash Sales: $1,000 (actual cash received)
?? Card Sales: $300 (actual card received)
?? Net Profit: $2,700 ($3,000 - $200 - $100)
?? Average Order Value: $1,000 ($3,000 / 3 orders)
```

---

### Sales Graph:

```
Date Range: Jan 27 - Jan 29

Orders by day:
?? Jan 27: $2,000 (2 orders)
?? Jan 28: $3,500 (3 orders)
?? Jan 29: $3,000 (3 orders)

Graph Data:
?? Labels: ["Jan 27", "Jan 28", "Jan 29"]
?? SalesData: [2000, 3500, 3000]  ? Total order amounts
?? ExpensesData: [300, 400, 200]
?? RefundsData: [50, 0, 100]
?? ProfitData: [1650, 3100, 2700]  ? Sales - Expenses - Refunds
```

---

## ?? Key Differences

### Before vs After:

| Metric | Before | After |
|--------|--------|-------|
| **Total Sales** | From accounting entries | ? From Orders table (includes pending) |
| **Cash Balance** | Income + Sale entries | ? Income entries only (actual cash) |
| **Cash/Card Breakdown** | Sale + Income entries | ? Income entries only |
| **Net Profit** | Based on income | ? Based on total sales |
| **Daily Sales** | From accounting only | ? From Orders (pending included) |
| **Partial Orders** | Not in sales graph | ? Included in sales graph |

---

## ?? Example Scenario

### Setup:
- **Day 1:**
  - Order 1: $1,500 (paid in full) ? Income: $1,500
  - Order 2: $2,000 (paid $500) ? Income: $500
  - Order 3: $800 (not paid yet) ? Income: $0
  - Expenses: $400
  - Refunds: $50

### Financial Summary Result:

```json
{
  "totalSales": 4300,       // ? All 3 orders
  "totalIncome": 2000,      // ? Income entries only ($1,500 + $500)
  "totalExpenses": 400,
  "totalRefunds": 50,
  "netProfit": 3850,        // ? $4,300 - $400 - $50
  "cashBalance": 1550,      // ? $2,000 - $400 - $50
  "period": "2025-01-29"
}
```

### Daily Sales Result:

```json
{
  "date": "2025-01-29",
  "totalSales": 4300,       // ? All 3 orders
  "totalOrders": 3,         // ? Count all orders
  "totalExpenses": 400,
  "totalRefunds": 50,
  "netProfit": 3850,        // ? Based on sales
  "cashSales": 1500,        // ? Actual cash received
  "cardSales": 500,         // ? Actual card received
  "averageOrderValue": 1433.33  // ? $4,300 / 3
}
```

### Sales Graph Result:

```json
{
  "labels": ["Jan 29"],
  "salesData": [4300],      // ? Total order amounts
  "expensesData": [400],
  "refundsData": [50],
  "profitData": [3850],     // ? $4,300 - $400 - $50
  "ordersData": [3]
}
```

---

## ? Benefits

### 1. Accurate Business View
- **Total Sales** shows true business volume (including pending)
- **Net Profit** shows business profitability (accrual basis)
- **Outstanding** can be calculated: Sales - Income

### 2. Clear Cash Position
- **Cash Balance** shows actual cash on hand
- **Cash/Card Sales** shows actual money received
- No confusion with pending amounts

### 3. Consistent Reporting
- All reports use same logic
- Financial summary matches daily sales
- Sales graph matches financial summary

### 4. Better Decision Making
- Can see total business done vs cash received
- Can identify collection issues (if gap is large)
- Can plan for outstanding receivables

---

## ?? Testing

### Test Case 1: Fully Paid Orders

```
Setup:
- 3 orders × $1,000 = $3,000
- All paid in full
- Expenses: $200

Expected:
{
  "totalSales": 3000,      // All orders
  "totalIncome": 3000,     // All received
  "cashBalance": 2800,     // 3000 - 200
  "netProfit": 2800,       // 3000 - 200
  "outstanding": 0         // 3000 - 3000
}
```

### Test Case 2: Partial Payments

```
Setup:
- Order 1: $1,500 (paid $1,500)
- Order 2: $2,000 (paid $500)
- Order 3: $800 (paid $0)
- Expenses: $400

Expected:
{
  "totalSales": 4300,      // All orders
  "totalIncome": 2000,     // 1500 + 500
  "cashBalance": 1600,     // 2000 - 400
  "netProfit": 3900,       // 4300 - 400
  "outstanding": 2300      // 4300 - 2000
}
```

### Test Case 3: Daily Sales with Mixed Payments

```
Setup (Day 1):
- Order 1: $1,000 (paid cash)
- Order 2: $1,500 (paid $500 card)
- Expenses: $200

Expected:
{
  "date": "2025-01-29",
  "totalSales": 2500,      // Both orders
  "cashSales": 1000,       // Actual cash
  "cardSales": 500,        // Actual card
  "totalOrders": 2,
  "netProfit": 2300,       // 2500 - 200
  "averageOrderValue": 1250  // 2500 / 2
}
```

---

## ?? SQL Verification

### Check Total Sales:
```sql
SELECT 
    SUM(TotalAmount) AS TotalSales,
    COUNT(*) AS OrderCount
FROM Orders
WHERE BranchId = @BranchId
  AND OrderStatus IN ('Completed', 'Paid', 'PartiallyPaid')
  AND Date >= @StartDate;
```

### Check Cash In Hand:
```sql
SELECT 
    SUM(Amount) AS CashInHand
FROM AccountingEntries
WHERE BranchId = @BranchId
  AND EntryType = 0  -- Income only
  AND EntryDate >= @StartDate;
```

### Check Outstanding:
```sql
SELECT 
    (SELECT SUM(TotalAmount) FROM Orders 
     WHERE BranchId = @BranchId 
       AND OrderStatus IN ('Completed', 'Paid', 'PartiallyPaid')) -
    (SELECT SUM(Amount) FROM AccountingEntries 
     WHERE BranchId = @BranchId 
       AND EntryType = 0) AS Outstanding;
```

---

## ?? Summary

### What Changed:

1. ? **GetFinancialSummaryAsync**
   - Added `cashInHand` (Income only)
   - `cashBalance` uses `cashInHand` instead of `totalIncome`
   - `netProfit` based on `totalSales`

2. ? **GetDailySalesAsync**
   - Gets `TotalSalesAmount` from Orders
   - Gets cash breakdown from Income entries only
   - `NetProfit` based on total sales

3. ? **GetSalesGraphAsync**
   - Includes `PartiallyPaid` orders
   - Shows total order amounts

### Key Formulas:

```
Total Sales = SUM(Orders.TotalAmount)
Cash In Hand = SUM(AccountingEntries WHERE EntryType = Income)
Cash Balance = Cash In Hand - Expenses - Refunds
Net Profit = Total Sales - Expenses - Refunds
Outstanding = Total Sales - Cash In Hand
```

---

## ?? Status

- ? **Code Modified:** All 3 methods updated
- ? **Build:** Successful
- ? **Logic:** Consistent across all reports
- ? **Ready:** For testing & deployment

---

**Last Updated:** January 29, 2025  
**Status:** ? **FINAL IMPLEMENTATION**  
**Build Status:** ? **SUCCESSFUL**
