using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelierProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Branches_BranchId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductItems_Branches_BranchId",
                table: "ProductItems");

            migrationBuilder.DropIndex(
                name: "IX_ProductItems_BranchId",
                table: "ProductItems");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BranchId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ProductItems");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Bookings");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Branches",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Branches",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "Branches");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ProductItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Branches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Branches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductItems_BranchId",
                table: "ProductItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BranchId",
                table: "Bookings",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Branches_BranchId",
                table: "Bookings",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductItems_Branches_BranchId",
                table: "ProductItems",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }
    }
}
