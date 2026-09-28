using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SASI.MigrationsSasi
{
    /// <inheritdoc />
    public partial class AgregarSsoClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthCode",
                columns: table => new
                {
                    IdAuthCode = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SistemaId = table.Column<int>(type: "int", nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeChallenge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeChallengeMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiraUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsadoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthCode", x => x.IdAuthCode);
                });

            migrationBuilder.CreateTable(
                name: "SistemaCliente",
                columns: table => new
                {
                    IdSistemaCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSistema = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientSecretHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequierePkce = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    AuditUsuarioCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditUsuarioModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditFechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemaCliente", x => x.IdSistemaCliente);
                    table.ForeignKey(
                        name: "FK_SistemaCliente_Sistemas_IdSistema",
                        column: x => x.IdSistema,
                        principalTable: "Sistemas",
                        principalColumn: "IdSistema",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthCode_CodeHash",
                table: "AuthCode",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthCode_ExpiraUtc",
                table: "AuthCode",
                column: "ExpiraUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuthCode_UsuarioId",
                table: "AuthCode",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SistemaCliente_ClientId",
                table: "SistemaCliente",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SistemaCliente_IdSistema",
                table: "SistemaCliente",
                column: "IdSistema");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthCode");

            migrationBuilder.DropTable(
                name: "SistemaCliente");
        }
    }
}
