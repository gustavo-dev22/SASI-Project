using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SASI.MigrationsSasi
{
    /// <inheritdoc />
    public partial class AgregarGobernanzaTISistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AreaDuenaId",
                table: "Sistemas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstadoCicloVida",
                table: "Sistemas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPuestaProduccion",
                table: "Sistemas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimaPruebaRestauracion",
                table: "Sistemas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoliticaRespaldo",
                table: "Sistemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsableFuncional",
                table: "Sistemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsableTecnico",
                table: "Sistemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RpoHoras",
                table: "Sistemas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RtoHoras",
                table: "Sistemas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionActual",
                table: "Sistemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SistemaContrato",
                columns: table => new
                {
                    IdSistemaContrato = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SistemaId = table.Column<int>(type: "int", nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NroContrato = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostoAnual = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SLA_Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemaContrato", x => x.IdSistemaContrato);
                    table.ForeignKey(
                        name: "FK_SistemaContrato_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "IdSistema",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SistemaDocumento",
                columns: table => new
                {
                    IdSistemaDocumento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SistemaId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoDoc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioSubida = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemaDocumento", x => x.IdSistemaDocumento);
                    table.ForeignKey(
                        name: "FK_SistemaDocumento_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "IdSistema",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SistemaVersion",
                columns: table => new
                {
                    IdSistemaVersion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SistemaId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Changelog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Entorno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaDespliegue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioDespliegue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemaVersion", x => x.IdSistemaVersion);
                    table.ForeignKey(
                        name: "FK_SistemaVersion_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "IdSistema",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sistemas_AreaDuenaId",
                table: "Sistemas",
                column: "AreaDuenaId");

            migrationBuilder.CreateIndex(
                name: "IX_SistemaContrato_SistemaId",
                table: "SistemaContrato",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_SistemaDocumento_SistemaId",
                table: "SistemaDocumento",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_SistemaVersion_SistemaId",
                table: "SistemaVersion",
                column: "SistemaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sistemas_Oficina_AreaDuenaId",
                table: "Sistemas",
                column: "AreaDuenaId",
                principalTable: "Oficina",
                principalColumn: "IdOficina",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sistemas_Oficina_AreaDuenaId",
                table: "Sistemas");

            migrationBuilder.DropTable(
                name: "SistemaContrato");

            migrationBuilder.DropTable(
                name: "SistemaDocumento");

            migrationBuilder.DropTable(
                name: "SistemaVersion");

            migrationBuilder.DropIndex(
                name: "IX_Sistemas_AreaDuenaId",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "AreaDuenaId",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "EstadoCicloVida",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "FechaPuestaProduccion",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "FechaUltimaPruebaRestauracion",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "PoliticaRespaldo",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "ResponsableFuncional",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "ResponsableTecnico",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "RpoHoras",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "RtoHoras",
                table: "Sistemas");

            migrationBuilder.DropColumn(
                name: "VersionActual",
                table: "Sistemas");
        }
    }
}
