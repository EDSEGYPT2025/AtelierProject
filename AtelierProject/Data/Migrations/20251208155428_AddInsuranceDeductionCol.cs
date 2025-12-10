using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelierProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuranceDeductionCol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceDeduction",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceDeduction",
                table: "Bookings");
        }
    }
}
