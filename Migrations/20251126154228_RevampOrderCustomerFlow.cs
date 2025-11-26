using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RevampOrderCustomerFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add CustomerId column as nullable first
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Orders",
                type: "int",
                nullable: true);

            // Step 2: Create customers from existing order data
            migrationBuilder.Sql(@"
                -- Create customers from existing orders with unique customer combinations
                INSERT INTO Users (UserId, FirstName, LastName, Password, Email, Phone, CurrentCity, Role, Salary, Age, JoinDate, Birthdate)
                SELECT DISTINCT
                    'CUST_' + CAST(ROW_NUMBER() OVER (ORDER BY CustomerFullName, CustomerPhone, CustomerEmail) AS VARCHAR(50)) AS UserId,
                    CASE 
                        WHEN CHARINDEX(' ', ISNULL(CustomerFullName, 'Guest Customer')) > 0 
                        THEN LEFT(ISNULL(CustomerFullName, 'Guest Customer'), CHARINDEX(' ', ISNULL(CustomerFullName, 'Guest Customer')) - 1)
                        ELSE ISNULL(CustomerFullName, 'Guest')
                    END AS FirstName,
                    CASE 
                        WHEN CHARINDEX(' ', ISNULL(CustomerFullName, 'Guest Customer')) > 0 
                        THEN SUBSTRING(ISNULL(CustomerFullName, 'Guest Customer'), CHARINDEX(' ', ISNULL(CustomerFullName, 'Guest Customer')) + 1, LEN(ISNULL(CustomerFullName, 'Guest Customer')))
                        ELSE 'Customer'
                    END AS LastName,
                    '' AS Password,
                    CustomerEmail AS Email,
                    CustomerPhone AS Phone,
                    CustomerAddress AS CurrentCity,
                    'Customer' AS Role,
                    0 AS Salary,
                    0 AS Age,
                    GETUTCDATE() AS JoinDate,
                    GETUTCDATE() AS Birthdate
                FROM (
                    SELECT DISTINCT
                        ISNULL(CustomerFullName, 'Guest Customer') AS CustomerFullName,
                        CustomerPhone,
                        CustomerEmail,
                        CustomerAddress
                    FROM Orders
                ) AS UniqueCustomers;
            ");

            // Step 3: Update CustomerId for existing orders
            migrationBuilder.Sql(@"
                -- Update CustomerId by matching phone number first (most reliable)
                UPDATE o
                SET o.CustomerId = u.Id
                FROM Orders o
                INNER JOIN Users u ON u.Role = 'Customer' 
                    AND ISNULL(o.CustomerPhone, '') = ISNULL(u.Phone, '')
                    AND o.CustomerPhone IS NOT NULL
                    AND u.Phone IS NOT NULL
                WHERE o.CustomerId IS NULL;

                -- Update remaining orders by matching email
                UPDATE o
                SET o.CustomerId = u.Id
                FROM Orders o
                INNER JOIN Users u ON u.Role = 'Customer' 
                    AND ISNULL(o.CustomerEmail, '') = ISNULL(u.Email, '')
                    AND o.CustomerEmail IS NOT NULL
                    AND u.Email IS NOT NULL
                WHERE o.CustomerId IS NULL;

                -- Update remaining orders by matching full name
                UPDATE o
                SET o.CustomerId = u.Id
                FROM Orders o
                INNER JOIN Users u ON u.Role = 'Customer' 
                    AND ISNULL(o.CustomerFullName, 'Guest Customer') = (u.FirstName + ' ' + u.LastName)
                WHERE o.CustomerId IS NULL;

                -- Set any remaining orders to a generic guest customer
                DECLARE @GuestCustomerId INT;
                
                -- Create or find guest customer
                IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = 'CUST_GUEST' AND Role = 'Customer')
                BEGIN
                    INSERT INTO Users (UserId, FirstName, LastName, Password, Role, Salary, Age, JoinDate, Birthdate, Phone, Email)
                    VALUES ('CUST_GUEST', 'Guest', 'Customer', '', 'Customer', 0, 0, GETUTCDATE(), GETUTCDATE(), NULL, NULL);
                END
                
                SELECT @GuestCustomerId = Id FROM Users WHERE UserId = 'CUST_GUEST' AND Role = 'Customer';

                -- Assign guest customer to any orders without a customer
                UPDATE Orders
                SET CustomerId = @GuestCustomerId
                WHERE CustomerId IS NULL;
            ");

            // Step 4: Make CustomerId NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Step 5: Drop old foreign keys and columns
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Products_ProductId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_Id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ProductId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerFullName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderQuantity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductMSRP",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Orders");

            // Step 6: Add index and foreign key for CustomerId
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_CustomerId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Orders",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                newName: "IX_Orders_ProductId");

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerFullName",
                table: "Orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderQuantity",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductMSRP",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Id",
                table: "Orders",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Products_ProductId",
                table: "Orders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_Id",
                table: "Orders",
                column: "Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
