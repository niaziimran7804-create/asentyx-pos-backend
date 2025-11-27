# ?? Fix: Type Mismatch Error in Products Table

## ? Error
```
System.InvalidCastException: Unable to cast object of type 'System.Decimal' to type 'System.Int32'
```

## ?? Root Cause
Your database `Products` table has columns with `DECIMAL` type, but your C# `Product` model is trying to read them as `INT`. This creates a type mismatch.

---

## ? Solution

### **Step 1: Check Your Database Schema**

Run this SQL query to see your actual table structure:

```sql
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    NUMERIC_PRECISION,
    NUMERIC_SCALE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Products'
ORDER BY ORDINAL_POSITION;
```

Look for columns that are `decimal` but mapped as `int` in your C# model.

---

### **Step 2: Update Your Product Model**

Replace the content of `Models/Product.cs` with this updated version:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [StringLength(100)]
        public string? ProductIdTag { get; set; }

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        public int? BrandId { get; set; }

        [StringLength(1000)]
        public string? ProductDescription { get; set; }

        public int ProductQuantityPerUnit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductPerUnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductMSRP { get; set; }

        [StringLength(10)]
        public string ProductStatus { get; set; } = "YES";

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductDiscountRate { get; set; }

        [StringLength(50)]
        public string? ProductSize { get; set; }          // ? ADD THIS

        [StringLength(50)]
        public string? ProductColor { get; set; }         // ? ADD THIS

        [Column(TypeName = "decimal(10,2)")]
        public decimal ProductWeight { get; set; }        // ? ADD THIS

        public int ProductUnitStock { get; set; }

        public int StockThreshold { get; set; } = 10;

        public byte[]? ProductImage { get; set; }

        // Navigation properties
        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }
    }
}
```

**What was missing:**
- `ProductSize` property
- `ProductColor` property
- `ProductWeight` property

---

### **Step 3: Build the Project**

```bash
dotnet build
```

If you get build errors, it means these columns might not exist in your database yet.

---

### **Step 4A: If Columns DON'T Exist in Database**

Create a migration to add them:

```bash
dotnet ef migrations add AddSizeColorWeightToProducts
dotnet ef database update
```

---

### **Step 4B: If Columns EXIST but Have Wrong Type**

You might need to manually fix the database schema. Check if any column is `decimal` but should be `int` or vice versa.

**Common issues:**

1. **ProductQuantityPerUnit is DECIMAL in DB but INT in model:**
   ```sql
   ALTER TABLE Products
   ALTER COLUMN ProductQuantityPerUnit INT;
   ```

2. **ProductWeight doesn't exist:**
   ```sql
   ALTER TABLE Products
   ADD ProductWeight DECIMAL(10,2) NOT NULL DEFAULT 0;
   
   ALTER TABLE Products
   ADD ProductSize NVARCHAR(50) NULL;
   
   ALTER TABLE Products
   ADD ProductColor NVARCHAR(50) NULL;
   ```

---

### **Step 5: Test the API**

```bash
GET http://localhost:7000/api/products
```

Should now work without errors.

---

## ?? Debugging Steps

### **Check Which Column is Causing the Issue**

The error happens at line 32 in `ProductService.cs`. The issue is during the mapping in `GetAllProductsAsync`:

```csharp
return products.Select(p => new ProductDto
{
    ProductId = p.ProductId,
    ProductIdTag = p.ProductIdTag,
    ProductName = p.ProductName,
    BrandId = p.BrandId,
    ProductDescription = p.ProductDescription,
    ProductQuantityPerUnit = p.ProductQuantityPerUnit,  // Could be this
    ProductPerUnitPrice = p.ProductPerUnitPrice,
    ProductMSRP = p.ProductMSRP,
    ProductStatus = p.ProductStatus,
    ProductDiscountRate = p.ProductDiscountRate,
    ProductSize = p.ProductSize,              // Or this
    ProductColor = p.ProductColor,            // Or this
    ProductWeight = p.ProductWeight,          // Or this
    ProductUnitStock = p.ProductUnitStock,
    StockThreshold = p.StockThreshold,
    BrandName = p.Brand?.BrandName,
    ProductImageBase64 = p.ProductImage != null ? Convert.ToBase64String(p.ProductImage) : null
});
```

---

## ?? Quick Fix (If you just want it working NOW)

**Option 1: Add missing properties to Product model** (recommended)
- Copy the code from Step 2 above

**Option 2: Remove references from DTOs and Service** (not recommended)
- Remove `ProductSize`, `ProductColor`, `ProductWeight` from all DTOs
- Remove mappings from ProductService

---

## ?? Checklist

- [ ] Run SQL query to check database schema
- [ ] Update `Models/Product.cs` with missing properties
- [ ] Run `dotnet build`
- [ ] If build succeeds, test API
- [ ] If build fails, create migration: `dotnet ef migrations add AddMissingProductColumns`
- [ ] Apply migration: `dotnet ef database update`
- [ ] Test API again

---

## ?? Common Causes

1. **Database and Model Out of Sync**
   - You manually altered the database
   - You ran migrations on one environment but not another
   - You're connecting to an old database

2. **Missing Columns**
   - DTOs reference properties that don't exist in the model
   - Model references properties that don't exist in database

3. **Type Mismatches**
   - Database column is `DECIMAL` but model property is `INT`
   - Database column is `NVARCHAR` but model property is `INT`

---

## ?? Need More Help?

If the error persists after following these steps:

1. Share the output of the SQL query from Step 1
2. Share any migration errors you encounter
3. Check if there are any pending migrations: `dotnet ef migrations list`

---

**Status:** ? **AWAITING FIX**  
**Action Required:** Update `Models/Product.cs` with the three missing properties  
**Expected Time:** 2 minutes

?? **Copy the code from Step 2 and replace your Product.cs file!**
