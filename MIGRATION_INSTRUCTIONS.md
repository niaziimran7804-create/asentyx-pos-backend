# Database Migration Instructions

## Problem
The Orders table is missing customer information columns (CustomerFullName, CustomerPhone, CustomerAddress, CustomerEmail).

## Solution Options

### Option 1: Restart the API (Recommended)
The migration will be applied automatically when the API starts because `DbInitializer.InitializeAsync()` calls `context.Database.MigrateAsync()`.

1. Stop the running API (Ctrl+C or close the terminal)
2. Restart the API: `dotnet run`
3. The migration will be applied automatically on startup

### Option 2: Run SQL Script Directly
If you can't restart the API right now, run the SQL script directly on your database:

1. Open SQL Server Management Studio (SSMS) or Azure Data Studio
2. Connect to your database server
3. Select your database (AsentyxPOS or POSDb)
4. Open and run the file: `Scripts/AddCustomerFields.sql`

### Option 3: Use Entity Framework CLI
If you have access to the terminal:

```powershell
cd "D:\my work place\POS\.NET-Point-of-Sale-POS--Csharp\Modernized\POS.Api"
dotnet ef database update
```

## Verification
After applying the migration, verify the columns exist:

```sql
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Orders'
AND COLUMN_NAME LIKE 'Customer%'
```

You should see:
- CustomerFullName (nvarchar, 255)
- CustomerPhone (nvarchar, 20)
- CustomerAddress (nvarchar, 500)
- CustomerEmail (nvarchar, 255)

