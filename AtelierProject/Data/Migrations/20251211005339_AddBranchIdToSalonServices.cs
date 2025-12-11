using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelierProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToSalonServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SalonServices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalonServices_BranchId",
                table: "SalonServices",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalonServices_Branches_BranchId",
                table: "SalonServices",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalonServices_Branches_BranchId",
                table: "SalonServices");

            migrationBuilder.DropIndex(
                name: "IX_SalonServices_BranchId",
                table: "SalonServices");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SalonServices");
        }
    }
}
