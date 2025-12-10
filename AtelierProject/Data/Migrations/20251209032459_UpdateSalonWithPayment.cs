using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelierProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalonWithPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "SalonAppointments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "SalonAppointments");
        }
    }
}
