-- =====================================================
-- QUICK FIX: Order 31 Price Inconsistency
-- =====================================================
-- This script fixes the price mismatch in OrderProductMaps
-- for Order 31, Product 13
-- =====================================================

USE AsentyxPOS;
GO

PRINT '===========================================';
PRINT 'Starting Price Fix for Order 31';
PRINT '===========================================';
PRINT '';

-- =====================================================
-- STEP 1: Create Backup
-- =====================================================
PRINT 'STEP 1: Creating backup...';

IF OBJECT_ID('OrderProductMaps_Backup_20251126', 'U') IS NOT NULL
    DROP TABLE OrderProductMaps_Backup_20251126;

SELECT * 
INTO OrderProductMaps_Backup_20251126
FROM OrderProductMaps
WHERE OrderId = 31;

DECLARE @BackupCount INT = (SELECT COUNT(*) FROM OrderProductMaps_Backup_20251126);
PRINT 'Backup created: ' + CAST(@BackupCount AS VARCHAR) + ' rows backed up';
PRINT '';

-- =====================================================
-- STEP 2: Show Current (Incorrect) State
-- =====================================================
PRINT 'STEP 2: Current state (BEFORE FIX):';
PRINT '-------------------------------------------';

SELECT 
    opm.OrderId,
    opm.ProductId,
    p.ProductName,
    opm.UnitPrice AS Current_UnitPrice,
    opm.Quantity,
    opm.TotalPrice AS Current_TotalPrice,
    o.TotalAmount AS Order_TotalAmount,
    o.OrderQuantity AS Order_Quantity,
    CAST(o.TotalAmount / NULLIF(o.OrderQuantity, 0) AS DECIMAL(18,2)) AS Should_Be_UnitPrice,
    CAST(ABS(opm.UnitPrice - (o.TotalAmount / NULLIF(o.OrderQuantity, 0))) AS DECIMAL(18,2)) AS Price_Difference
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
INNER JOIN Products p ON opm.ProductId = p.ProductId
WHERE opm.OrderId = 31;

PRINT '';

-- =====================================================
-- STEP 3: Apply Fix
-- =====================================================
PRINT 'STEP 3: Applying fix...';

UPDATE opm
SET 
    opm.UnitPrice = CAST(o.TotalAmount / NULLIF(o.OrderQuantity, 0) AS DECIMAL(18,2)),
    opm.TotalPrice = opm.Quantity * CAST(o.TotalAmount / NULLIF(o.OrderQuantity, 0) AS DECIMAL(18,2))
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
WHERE opm.OrderId = 31;

DECLARE @RowsUpdated INT = @@ROWCOUNT;
PRINT 'Rows updated: ' + CAST(@RowsUpdated AS VARCHAR);
PRINT '';

-- =====================================================
-- STEP 4: Verify Fix
-- =====================================================
PRINT 'STEP 4: Verification (AFTER FIX):';
PRINT '-------------------------------------------';

SELECT 
    opm.OrderId,
    opm.ProductId,
    p.ProductName,
    opm.UnitPrice AS Fixed_UnitPrice,
    opm.Quantity,
    opm.TotalPrice AS Fixed_TotalPrice,
    o.TotalAmount AS Order_TotalAmount,
    CAST(ABS(opm.TotalPrice - o.TotalAmount) AS DECIMAL(18,2)) AS Remaining_Difference
FROM OrderProductMaps opm
INNER JOIN Orders o ON opm.OrderId = o.OrderId
INNER JOIN Products p ON opm.ProductId = p.ProductId
WHERE opm.OrderId = 31;

PRINT '';

-- =====================================================
-- STEP 5: Check for Other Inconsistencies
-- =====================================================
PRINT 'STEP 5: Checking for other inconsistent orders...';

SELECT 
    COUNT(*) AS Inconsistent_Orders_Count
FROM Orders o
INNER JOIN OrderProductMaps opm ON o.OrderId = opm.OrderId
WHERE ABS((o.TotalAmount / NULLIF(o.OrderQuantity, 0)) - opm.UnitPrice) > 0.01;

PRINT '';
PRINT '===========================================';
PRINT 'Fix Completed Successfully!';
PRINT '===========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Test the API: POST /api/returns/partial';
PRINT '2. Use returnAmount: 286.50 (not 550.00)';
PRINT '3. If it still fails, restart the backend application';
PRINT '';

GO
