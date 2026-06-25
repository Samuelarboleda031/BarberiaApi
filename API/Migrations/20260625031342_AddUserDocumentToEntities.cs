using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDocumentToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioDocumento",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioTipoDocumento",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioDocumento",
                table: "Devoluciones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioTipoDocumento",
                table: "Devoluciones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioDocumento",
                table: "Compras",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioTipoDocumento",
                table: "Compras",
                type: "nvarchar(max)",
                nullable: true);

            // Backfill Ventas with document info
            migrationBuilder.Sql(@"
                UPDATE v
                SET v.UsuarioDocumento = u.Documento,
                    v.UsuarioTipoDocumento = u.TipoDocumento
                FROM Ventas v
                INNER JOIN Usuarios u ON v.UsuarioId = u.Id
            ");

            // Backfill Compras with document info
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.UsuarioDocumento = u.Documento,
                    c.UsuarioTipoDocumento = u.TipoDocumento
                FROM Compras c
                INNER JOIN Usuarios u ON c.UsuarioId = u.Id
            ");

            // Backfill Devoluciones with document info
            migrationBuilder.Sql(@"
                UPDATE d
                SET d.UsuarioDocumento = u.Documento,
                    d.UsuarioTipoDocumento = u.TipoDocumento
                FROM Devoluciones d
                INNER JOIN Usuarios u ON d.UsuarioId = u.Id
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioDocumento",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioTipoDocumento",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioDocumento",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "UsuarioTipoDocumento",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "UsuarioDocumento",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "UsuarioTipoDocumento",
                table: "Compras");
        }
    }
}
