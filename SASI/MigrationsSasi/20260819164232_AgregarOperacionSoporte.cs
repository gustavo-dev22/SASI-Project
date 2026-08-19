using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SASI.MigrationsSasi
{
    /// <inheritdoc />
    public partial class AgregarOperacionSoporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoOperativoActual",
                table: "Sistemas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EstadoOperativoSistema",
                columns: table => new
                {
                    IdEstadoOperativo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SistemaId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioRegistro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoOperativoSistema", x => x.IdEstadoOperativo);
                    table.ForeignKey(
                        name: "FK_EstadoOperativoSistema_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "IdSistema",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Incidencia",
                columns: table => new
                {
                    IdIncidencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SistemaId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaReporte = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaAtencion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioReporte = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidencia", x => x.IdIncidencia);
                    table.ForeignKey(
                        name: "FK_Incidencia_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "IdSistema",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudAcceso",
                columns: table => new
                {
                    IdSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SistemaId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    Justificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRespuesta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AprobadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SolicitudAcceso", x => x.IdSolicitud);
                    table.ForeignKey(
                        name: "FK_SolicitudAcceso_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "IdRol",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudAcceso_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "IdSistema",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstadoOperativoSistema_SistemaId",
                table: "EstadoOperativoSistema",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidencia_SistemaId",
                table: "Incidencia",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudAcceso_RolId",
                table: "SolicitudAcceso",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudAcceso_SistemaId",
                table: "SolicitudAcceso",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudAcceso_UsuarioId",
                table: "SolicitudAcceso",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstadoOperativoSistema");

            migrationBuilder.DropTable(
                name: "Incidencia");

            migrationBuilder.DropTable(
                name: "SolicitudAcceso");

            migrationBuilder.DropColumn(
                name: "EstadoOperativoActual",
                table: "Sistemas");
        }
    }
}
