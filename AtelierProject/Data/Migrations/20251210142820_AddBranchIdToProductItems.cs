using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelierProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToProductItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ProductItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductItems_BranchId",
                table: "ProductItems",
                column: "BranchId");

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
                name: "IX_ProductItems_BranchId",
                table: "ProductItems");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ProductItems");
        }
    }
}
