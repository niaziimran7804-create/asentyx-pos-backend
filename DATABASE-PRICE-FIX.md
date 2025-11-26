# Database Price Inconsistency Fix

## ?? **Issue Identified**

**Error Message:**
```
Invalid return amount for product 13. 
Expected: 550.00 (Unit Price: 550.00 × Quantity: 1), 
Received: 286.50, 
Difference: 263.50
```

**Root Cause:** 
The `OrderProductMaps` table has `UnitPrice = 550.00` but the actual order amount suggests the price should be `286.50`.

---

## ?? **Diagnostic Queries**

### **Step 1: Check Order 31 Details**

```sql
-- Check the main order
SELECT 
    OrderId,
    ProductId,
    OrderQuantity,
    ProductMSRP,
    TotalAmount,
    TotalAmount / NULLIF(OrderQuantity, 0) AS CalculatedUnitPrice,
    Status,
    OrderStatus,
    PaymentMethod,
    Date
FROM Orders
WHERE OrderId = 31;
```

### **Step 2: Check OrderProductMaps for Order 31**

```sql
-- Check what's in OrderProductMaps
SELECT 
    opm.OrderProductMapId,
    opm.OrderId,
    opm.ProductId,
    opm.Quantity,
    opm.UnitPrice,
    opm.TotalPrice,
    p.ProductName,
    p.ProductMSRP AS CurrentMSRP
FROM OrderProductMaps opm
INNER JOIN Products p ON opm.ProductId = p.ProductId
WHERE opm.OrderId = 31;
```

### **Step 3: Check Invoice for Order 31**

```sql
-- Check invoice details
SELECT 
    i.InvoiceId,
    i.OrderId,
    i.InvoiceNumber,
    i.TotalAmount,
    i.Status,
    i.InvoiceDate
FROM Invoices i
WHERE i.OrderId = 31;
```

### **Step 4: Find ALL Inconsistencies**

```sql
-- Find all orders with price mismatches
SELECT 
    o.OrderId,
    o.TotalAmount AS OrderTotal,
    o.OrderQuantity,
    ROUND(o.TotalAmount / NULLIF(o.OrderQuantity, 0), 2) AS OrderCalcUnitPrice,
    opm.ProductId,
    opm.UnitPrice AS OrderMapUnitPrice,
    opm.Quantity AS OrderMapQty,
    opm.TotalPrice AS OrderMapTotal,
    ROUND(ABS((o.TotalAmount / NULLIF(o.OrderQuantity, 0)) - opm.UnitPrice), 2) AS PriceDifference
FROM Orders o
INNER JOIN OrderProductMaps opm ON o.OrderId = opm.OrderId
WHERE ABS((o.TotalAmount / NULLIF(o.OrderQuantity, 0)) - opm.UnitPrice) > 0.01
ORDER BY o.OrderId DESC;
```

---

## ?? **Fix Queries**

### **Option 1: Fix Order 31 Specifically**

```sql
-- First, check what the correct price should be
SELECT 
    OrderId,
    TotalAmount,
    OrderQuantity,
    TotalAmount / NULLIF(OrderQuantity, 0) AS CorrectUnitPrice
FROM Orders
WHERE OrderId = 31;

-- Then update OrderProductMaps to match the actual order total
UPDATE opm
SET 
    opm.UnitPrice = o.TotalAmount / NULLIF(o.OrderQuantity, 0),
    opm.TotalPrice = opm.Quantity * (o.TotalAmount / NULLIF(o.OrderQuantity, 0))
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
WHERE o.OrderId = 31;

-- Verify the fix
SELECT 
    opm.OrderId,
    opm.ProductId,
    opm.Quantity,
    opm.UnitPrice,
    opm.TotalPrice,
    o.TotalAmount AS OrderTotal
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
WHERE opm.OrderId = 31;
```

### **Option 2: Fix ALL Inconsistent Orders**

```sql
-- Backup first!
SELECT * INTO OrderProductMaps_Backup_20251126
FROM OrderProductMaps;

-- Fix all orders where unit prices don't match
UPDATE opm
SET 
    opm.UnitPrice = o.TotalAmount / NULLIF(o.OrderQuantity, 0),
    opm.TotalPrice = opm.Quantity * (o.TotalAmount / NULLIF(o.OrderQuantity, 0))
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
WHERE ABS((o.TotalAmount / NULLIF(o.OrderQuantity, 0)) - opm.UnitPrice) > 0.01;

-- Check how many rows were updated
SELECT @@ROWCOUNT AS RowsUpdated;
```

---

## ?? **Verification Steps**

### **After Running Fix:**

```sql
-- 1. Verify Order 31 is fixed
SELECT 
    opm.OrderId,
    opm.ProductId,
    opm.UnitPrice,
    opm.Quantity,
    opm.TotalPrice,
    o.TotalAmount,
    o.OrderQuantity
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
WHERE opm.OrderId = 31;

-- Expected Result:
-- UnitPrice should be 286.50 (not 550.00)
-- TotalPrice should be 286.50 (UnitPrice × Quantity)
-- Should match OrderTotal

-- 2. Check no more inconsistencies exist
SELECT COUNT(*) AS InconsistentOrders
FROM Orders o
INNER JOIN OrderProductMaps opm ON o.OrderId = opm.OrderId
WHERE ABS((o.TotalAmount / NULLIF(o.OrderQuantity, 0)) - opm.UnitPrice) > 0.01;

-- Expected Result: 0
```

---

## ?? **Step-by-Step Fix Process**

### **Step 1: Backup**
```sql
-- Create backup of OrderProductMaps table
SELECT * INTO OrderProductMaps_Backup_20251126
FROM OrderProductMaps;

-- Verify backup
SELECT COUNT(*) FROM OrderProductMaps_Backup_20251126;
```

### **Step 2: Diagnose Order 31**
```sql
-- Check what the prices should be
SELECT 
    o.OrderId,
    o.TotalAmount,
    o.OrderQuantity,
    o.TotalAmount / NULLIF(o.OrderQuantity, 0) AS CorrectUnitPrice,
    opm.UnitPrice AS CurrentUnitPrice,
    opm.UnitPrice - (o.TotalAmount / NULLIF(o.OrderQuantity, 0)) AS Difference
FROM Orders o
INNER JOIN OrderProductMaps opm ON o.OrderId = opm.OrderId
WHERE o.OrderId = 31;
```

**Expected Output:**
```
OrderId | TotalAmount | OrderQuantity | CorrectUnitPrice | CurrentUnitPrice | Difference
31      | 286.50      | 1             | 286.50           | 550.00           | 263.50
```

### **Step 3: Fix Order 31**
```sql
-- Fix the specific order
UPDATE opm
SET 
    opm.UnitPrice = 286.50,  -- The correct price
    opm.TotalPrice = opm.Quantity * 286.50
FROM OrderProductMaps opm
WHERE opm.OrderId = 31 AND opm.ProductId = 13;
```

### **Step 4: Verify Fix**
```sql
-- Confirm the fix worked
SELECT 
    opm.OrderId,
    opm.ProductId,
    opm.UnitPrice,
    opm.Quantity,
    opm.TotalPrice
FROM OrderProductMaps opm
WHERE opm.OrderId = 31 AND opm.ProductId = 13;
```

**Expected Output:**
```
OrderId | ProductId | UnitPrice | Quantity | TotalPrice
31      | 13        | 286.50    | 1        | 286.50
```

---

## ?? **Test Partial Return After Fix**

Once the database is fixed, test the partial return:

```http
POST https://localhost:7000/api/returns/partial
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "returnType": "partial",
  "invoiceId": 20,
  "orderId": 31,
  "returnReason": "Testing after price fix",
  "refundMethod": "Cash",
  "notes": "Database prices corrected",
  "items": [
    {
      "productId": 13,
      "returnQuantity": 1,
      "returnAmount": 286.50
    }
  ],
  "totalReturnAmount": 286.50
}
```

**Expected:** 201 Created ?

---

## ?? **Why Did This Happen?**

Possible reasons for the price mismatch:

1. **Order was created with wrong price in OrderProductMaps**
   - Frontend sent 286.50 but OrderService saved 550.00
   - Could be due to discount/tax calculation issue

2. **Product MSRP changed after order was created**
   - Order was created when MSRP was 286.50
   - Later MSRP changed to 550.00
   - OrderProductMaps was updated incorrectly

3. **Manual database edit**
   - Someone manually updated OrderProductMaps
   - But didn't update Order.TotalAmount

4. **Bug in order creation code**
   - OrderService might be using wrong price source
   - Should use `item.UnitPrice` from request
   - But using `Product.MSRP` instead

---

## ??? **Prevent Future Issues**

### **Add Database Constraint**

```sql
-- Add check constraint to ensure consistency
ALTER TABLE OrderProductMaps
ADD CONSTRAINT CHK_TotalPrice_Matches_UnitPrice_Times_Quantity
CHECK (ABS(TotalPrice - (UnitPrice * Quantity)) < 0.01);
```

### **Add Validation in OrderService**

Check the `OrderService.CreateOrderAsync` method:

```csharp
// In OrderService.cs - CreateOrderAsync
foreach (var item in createOrderDto.Items)
{
    var orderProductMap = new Models.OrderProductMap
    {
        OrderId = order.OrderId,
        ProductId = item.ProductId,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,  // ? Use price from request, not Product.MSRP
        TotalPrice = item.Quantity * item.UnitPrice
    };
    _context.OrderProductMaps.Add(orderProductMap);
}
```

---

## ?? **Quick Fix Script (Run This)**

```sql
-- Complete fix script for Order 31
USE POSDb;
GO

-- Step 1: Backup
SELECT * INTO OrderProductMaps_Backup_Before_Fix
FROM OrderProductMaps
WHERE OrderId = 31;

-- Step 2: Show current state
PRINT 'BEFORE FIX:';
SELECT 
    opm.OrderId,
    opm.ProductId,
    opm.UnitPrice AS CurrentUnitPrice,
    o.TotalAmount / NULLIF(o.OrderQuantity, 0) AS ShouldBeUnitPrice
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
WHERE opm.OrderId = 31;

-- Step 3: Fix the price
UPDATE opm
SET 
    opm.UnitPrice = o.TotalAmount / NULLIF(o.OrderQuantity, 0),
    opm.TotalPrice = opm.Quantity * (o.TotalAmount / NULLIF(o.OrderQuantity, 0))
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
WHERE opm.OrderId = 31;

-- Step 4: Show fixed state
PRINT 'AFTER FIX:';
SELECT 
    opm.OrderId,
    opm.ProductId,
    opm.UnitPrice,
    opm.Quantity,
    opm.TotalPrice,
    o.TotalAmount
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
WHERE opm.OrderId = 31;

PRINT 'Fix completed successfully!';
```

---

## ? **Success Checklist**

After running the fix:

- [ ] Backup created (OrderProductMaps_Backup_20251126)
- [ ] Diagnostic query shows UnitPrice was 550.00
- [ ] Fix query updated UnitPrice to 286.50
- [ ] Verification query confirms UnitPrice is now 286.50
- [ ] TotalPrice matches UnitPrice × Quantity
- [ ] No more inconsistent orders (query returns 0)
- [ ] Partial return API test returns 201 Created
- [ ] Frontend shows correct prices in invoice
- [ ] Backend logs show validation passing

---

## ?? **If Fix Doesn't Work**

If after fixing the database, returns still fail:

1. **Restart the backend application**
   - EF Core might have cached the old values
   - Restarting clears the cache

2. **Check if there are multiple OrderProductMaps entries**
   ```sql
   SELECT COUNT(*) 
   FROM OrderProductMaps 
   WHERE OrderId = 31 AND ProductId = 13;
   -- Should be 1, if more than 1, there are duplicates
   ```

3. **Verify Invoice is returning updated prices**
   ```http
   GET /api/invoices/20
   ```
   Check response `items[0].unitPrice` should be 286.50

4. **Check backend logs**
   - Should show: `OrderProductMap UnitPrice=286.50`
   - Not: `OrderProductMap UnitPrice=550.00`

---

**Status:** ?? **DATABASE FIX REQUIRED**  
**Impact:** Blocks partial returns for Order 31  
**Fix Time:** 5 minutes  
**Risk:** Low (we're backing up first)

**Action Required:** Run the SQL scripts above to fix the price inconsistency!
