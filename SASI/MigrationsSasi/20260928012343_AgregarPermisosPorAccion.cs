using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SASI.MigrationsSasi
{
    /// <inheritdoc />
    public partial class AgregarPermisosPorAccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accion",
                columns: table => new
                {
                    IdAccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Accion", x => x.IdAccion);
                });

            migrationBuilder.CreateTable(
                name: "RolObjetoAccion",
                columns: table => new
                {
                    IdRolObjetoAccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRol = table.Column<int>(type: "int", nullable: false),
                    IdObjeto = table.Column<int>(type: "int", nullable: false),
                    IdAccion = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RolObjetoAccion", x => x.IdRolObjetoAccion);
                    table.ForeignKey(
                        name: "FK_RolObjetoAccion_Accion_IdAccion",
                        column: x => x.IdAccion,
                        principalTable: "Accion",
                        principalColumn: "IdAccion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolObjetoAccion_Objeto_IdObjeto",
                        column: x => x.IdObjeto,
                        principalTable: "Objeto",
                        principalColumn: "IdObjeto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolObjetoAccion_Roles_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "IdRol",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Accion",
                columns: new[] { "IdAccion", "Activo", "AuditFechaCreacion", "AuditFechaModificacion", "AuditUsuarioCreacion", "AuditUsuarioModificacion", "Codigo", "Descripcion", "IpCreacion", "IpModificacion", "Nombre", "Orden" },
                values: new object[,]
                {
                    { 1, true, null, null, null, null, "LISTAR", null, null, null, "Listar", 1 },
                    { 2, true, null, null, null, null, "CREAR", null, null, null, "Crear", 2 },
                    { 3, true, null, null, null, null, "EDITAR", null, null, null, "Editar", 3 },
                    { 4, true, null, null, null, null, "ELIMINAR", null, null, null, "Eliminar", 4 },
                    { 5, true, null, null, null, null, "BLOQUEAR", null, null, null, "Bloquear", 5 },
                    { 6, true, null, null, null, null, "DESBLOQUEAR", null, null, null, "Desbloquear", 6 },
                    { 7, true, null, null, null, null, "EXPORTAR", null, null, null, "Exportar", 7 },
                    { 8, true, null, null, null, null, "APROBAR", null, null, null, "Aprobar", 8 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accion_Codigo",
                table: "Accion",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolObjetoAccion_IdAccion",
                table: "RolObjetoAccion",
                column: "IdAccion");

            migrationBuilder.CreateIndex(
                name: "IX_RolObjetoAccion_IdObjeto",
                table: "RolObjetoAccion",
                column: "IdObjeto");

            migrationBuilder.CreateIndex(
                name: "IX_RolObjetoAccion_IdRol",
                table: "RolObjetoAccion",
                column: "IdRol");

            migrationBuilder.CreateIndex(
                name: "IX_RolObjetoAccion_IdRol_IdObjeto_IdAccion",
                table: "RolObjetoAccion",
                columns: new[] { "IdRol", "IdObjeto", "IdAccion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolObjetoAccion");

            migrationBuilder.DropTable(
                name: "Accion");
        }
    }
}
