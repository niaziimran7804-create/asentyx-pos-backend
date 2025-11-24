using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BarCodes",
                columns: table => new
                {
                    BarCodeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarCode1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarCodes", x => x.BarCodeId);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    ExpenseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpenseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.ExpenseId);
                });

            migrationBuilder.CreateTable(
                name: "MainCategories",
                columns: table => new
                {
                    MainCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MainCategoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MainCategoryDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MainCategoryImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainCategories", x => x.MainCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Birthdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HomeTown = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Division = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BloodGroup = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PostalCode = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecondCategories",
                columns: table => new
                {
                    SecondCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MainCategoryId = table.Column<int>(type: "int", nullable: false),
                    SecondCategoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SecondCategoryDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SecondCategoryImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecondCategories", x => x.SecondCategoryId);
                    table.ForeignKey(
                        name: "FK_SecondCategories_MainCategories_MainCategoryId",
                        column: x => x.MainCategoryId,
                        principalTable: "MainCategories",
                        principalColumn: "MainCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThirdCategories",
                columns: table => new
                {
                    ThirdCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecondCategoryId = table.Column<int>(type: "int", nullable: false),
                    ThirdCategoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ThirdCategoryDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThirdCategoryImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThirdCategories", x => x.ThirdCategoryId);
                    table.ForeignKey(
                        name: "FK_ThirdCategories_SecondCategories_SecondCategoryId",
                        column: x => x.SecondCategoryId,
                        principalTable: "SecondCategories",
                        principalColumn: "SecondCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    VendorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorTag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ThirdCategoryId = table.Column<int>(type: "int", nullable: false),
                    VendorDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VendorStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    VendorImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    RegisterDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.VendorId);
                    table.ForeignKey(
                        name: "FK_Vendors_ThirdCategories_ThirdCategoryId",
                        column: x => x.ThirdCategoryId,
                        principalTable: "ThirdCategories",
                        principalColumn: "ThirdCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    BrandId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandTag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    BrandDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BrandStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BrandImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.BrandId);
                    table.ForeignKey(
                        name: "FK_Brands_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductIdTag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProductQuantityPerUnit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductPerUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductMSRP = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProductDiscountRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductUnitStock = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Products_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "BrandId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    BarCodeId = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderQuantity = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductMSRP = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_BarCodes_BarCodeId",
                        column: x => x.BarCodeId,
                        principalTable: "BarCodes",
                        principalColumn: "BarCodeId");
                    table.ForeignKey(
                        name: "FK_Orders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderProductMaps",
                columns: table => new
                {
                    OrderProductMapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderProductMaps", x => x.OrderProductMapId);
                    table.ForeignKey(
                        name: "FK_OrderProductMaps_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderProductMaps_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_VendorId",
                table: "Brands",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductMaps_OrderId",
                table: "OrderProductMaps",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductMaps_ProductId",
                table: "OrderProductMaps",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BarCodeId",
                table: "Orders",
                column: "BarCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Id",
                table: "Orders",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProductId",
                table: "Orders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_SecondCategories_MainCategoryId",
                table: "SecondCategories",
                column: "MainCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ThirdCategories_SecondCategoryId",
                table: "ThirdCategories",
                column: "SecondCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_ThirdCategoryId",
                table: "Vendors",
                column: "ThirdCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "OrderProductMaps");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "BarCodes");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "ThirdCategories");

            migrationBuilder.DropTable(
                name: "SecondCategories");

            migrationBuilder.DropTable(
                name: "MainCategories");
        }
    }
}
