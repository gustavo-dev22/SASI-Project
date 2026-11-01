using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SASI.MigrationsSasi
{
    /// <inheritdoc />
    public partial class AgregarSolicitudRestablecimientoPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudRestablecimientoPassword",
                columns: table => new
                {
                    IdSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Identificador = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SistemaId = table.Column<int>(type: "int", nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRespuesta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AtendidoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComentarioRespuesta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudRestablecimientoPassword", x => x.IdSolicitud);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudRestablecimientoPassword_Estado",
                table: "SolicitudRestablecimientoPassword",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudRestablecimientoPassword_FechaSolicitud",
                table: "SolicitudRestablecimientoPassword",
                column: "FechaSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudRestablecimientoPassword_UsuarioId",
                table: "SolicitudRestablecimientoPassword",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudRestablecimientoPassword");
        }
    }
}
