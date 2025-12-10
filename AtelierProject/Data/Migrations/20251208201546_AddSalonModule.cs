using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelierProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalonAppointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalonAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalonAppointments_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalonServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalonServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalonAppointmentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalonAppointmentId = table.Column<int>(type: "int", nullable: false),
                    SalonServiceId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalonAppointmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalonAppointmentItems_SalonAppointments_SalonAppointmentId",
                        column: x => x.SalonAppointmentId,
                        principalTable: "SalonAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalonAppointmentItems_SalonServices_SalonServiceId",
                        column: x => x.SalonServiceId,
                        principalTable: "SalonServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalonAppointmentItems_SalonAppointmentId",
                table: "SalonAppointmentItems",
                column: "SalonAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalonAppointmentItems_SalonServiceId",
                table: "SalonAppointmentItems",
                column: "SalonServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalonAppointments_ClientId",
                table: "SalonAppointments",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalonAppointmentItems");

            migrationBuilder.DropTable(
                name: "SalonAppointments");

            migrationBuilder.DropTable(
                name: "SalonServices");
        }
    }
}
