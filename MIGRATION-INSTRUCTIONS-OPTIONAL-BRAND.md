# Migration Instructions: Make BrandId Optional in Products

## Step 1: Create the Migration
Run this command in your terminal:

```bash
dotnet ef migrations add MakeBrandIdOptionalInProducts
```

## Step 2: Review the Migration
The migration will modify the `Products` table to make the `BrandId` column nullable.

Expected migration content:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AlterColumn<int>(
        name: "BrandId",
        table: "Products",
        type: "int",
        nullable: true,
        oldClrType: typeof(int),
        oldType: "int",
        oldNullable: false);
}
```

## Step 3: Apply the Migration
Run this command to update the database:

```bash
dotnet ef database update
```

## What This Does:
- ? Makes `BrandId` column nullable (allows NULL values)
- ? Existing products with BrandId will keep their values
- ? New products can be created without a Brand
- ? Foreign key constraint remains (but allows NULL)

## After Migration:
You'll be able to create products without specifying a brand:

```json
POST /api/products
{
  "productName": "Generic Product",
  "brandId": null,  // ? This is now valid
  "productPerUnitPrice": 100.00,
  ...
}
```

## Rollback (if needed):
```bash
dotnet ef database update <PreviousMigrationName>
dotnet ef migrations remove
```
