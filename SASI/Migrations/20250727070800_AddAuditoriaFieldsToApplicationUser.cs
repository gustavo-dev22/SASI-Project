using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SASI.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaFieldsToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AuditFechaCreacion",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "AuditFechaModificacion",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditUsuarioCreacion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuditUsuarioModificacion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpCreacion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpModificacion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditFechaCreacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AuditFechaModificacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AuditUsuarioCreacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AuditUsuarioModificacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IpCreacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IpModificacion",
                table: "AspNetUsers");
        }
    }
}
