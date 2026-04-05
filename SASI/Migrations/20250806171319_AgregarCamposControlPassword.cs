using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SASI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposControlPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarPassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoCambioPassword",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebeCambiarPassword",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FechaUltimoCambioPassword",
                table: "AspNetUsers");
        }
    }
}
