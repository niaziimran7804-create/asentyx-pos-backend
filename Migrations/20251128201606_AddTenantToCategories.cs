using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Vendors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Vendors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ThirdCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ThirdCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SecondCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SecondCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "MainCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "MainCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Brands",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Brands",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_BranchId",
                table: "Vendors",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_CompanyId",
                table: "Vendors",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ThirdCategories_BranchId",
                table: "ThirdCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ThirdCategories_CompanyId",
                table: "ThirdCategories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SecondCategories_BranchId",
                table: "SecondCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SecondCategories_CompanyId",
                table: "SecondCategories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MainCategories_BranchId",
                table: "MainCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MainCategories_CompanyId",
                table: "MainCategories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_BranchId",
                table: "Brands",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_CompanyId",
                table: "Brands",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Brands_Branches_BranchId",
                table: "Brands",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Brands_Companies_CompanyId",
                table: "Brands",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_MainCategories_Branches_BranchId",
                table: "MainCategories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_MainCategories_Companies_CompanyId",
                table: "MainCategories",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SecondCategories_Branches_BranchId",
                table: "SecondCategories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_SecondCategories_Companies_CompanyId",
                table: "SecondCategories",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ThirdCategories_Branches_BranchId",
                table: "ThirdCategories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_ThirdCategories_Companies_CompanyId",
                table: "ThirdCategories",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_Branches_BranchId",
                table: "Vendors",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_Companies_CompanyId",
                table: "Vendors",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Brands_Branches_BranchId",
                table: "Brands");

            migrationBuilder.DropForeignKey(
                name: "FK_Brands_Companies_CompanyId",
                table: "Brands");

            migrationBuilder.DropForeignKey(
                name: "FK_MainCategories_Branches_BranchId",
                table: "MainCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_MainCategories_Companies_CompanyId",
                table: "MainCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_SecondCategories_Branches_BranchId",
                table: "SecondCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_SecondCategories_Companies_CompanyId",
                table: "SecondCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ThirdCategories_Branches_BranchId",
                table: "ThirdCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ThirdCategories_Companies_CompanyId",
                table: "ThirdCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_Branches_BranchId",
                table: "Vendors");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_Companies_CompanyId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_BranchId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_CompanyId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_ThirdCategories_BranchId",
                table: "ThirdCategories");

            migrationBuilder.DropIndex(
                name: "IX_ThirdCategories_CompanyId",
                table: "ThirdCategories");

            migrationBuilder.DropIndex(
                name: "IX_SecondCategories_BranchId",
                table: "SecondCategories");

            migrationBuilder.DropIndex(
                name: "IX_SecondCategories_CompanyId",
                table: "SecondCategories");

            migrationBuilder.DropIndex(
                name: "IX_MainCategories_BranchId",
                table: "MainCategories");

            migrationBuilder.DropIndex(
                name: "IX_MainCategories_CompanyId",
                table: "MainCategories");

            migrationBuilder.DropIndex(
                name: "IX_Brands_BranchId",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_CompanyId",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ThirdCategories");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ThirdCategories");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SecondCategories");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SecondCategories");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "MainCategories");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "MainCategories");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Brands");
        }
    }
}
