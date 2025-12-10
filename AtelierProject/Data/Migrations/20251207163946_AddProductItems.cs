using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelierProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductItems_Branches_BranchId",
                table: "ProductItems");

            migrationBuilder.DropIndex(
                name: "IX_ProductItems_Barcode",
                table: "ProductItems");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "ProductItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ProductItems_Barcode",
                table: "ProductItems",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductItems_Branches_BranchId",
                table: "ProductItems",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductItems_Branches_BranchId",
                table: "ProductItems");

            migrationBuilder.DropIndex(
                name: "IX_ProductItems_Barcode",
                table: "ProductItems");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "ProductItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductItems_Barcode",
                table: "ProductItems",
                column: "Barcode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductItems_Branches_BranchId",
                table: "ProductItems",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
