using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredUserInfoToVentasCompras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioApellido",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCorreo",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioNombre",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioApellido",
                table: "Compras",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCorreo",
                table: "Compras",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioNombre",
                table: "Compras",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioApellido",
                table: "Devoluciones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCorreo",
                table: "Devoluciones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioNombre",
                table: "Devoluciones",
                type: "nvarchar(max)",
                nullable: true);

            // Backfill existing Ventas with user info
            migrationBuilder.Sql(@"
                UPDATE v
                SET v.UsuarioNombre = u.Nombre,
                    v.UsuarioApellido = u.Apellido,
                    v.UsuarioCorreo = u.Correo
                FROM Ventas v
                INNER JOIN Usuarios u ON v.UsuarioId = u.Id
            ");

            // Backfill existing Compras with user info
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.UsuarioNombre = u.Nombre,
                    c.UsuarioApellido = u.Apellido,
                    c.UsuarioCorreo = u.Correo
                FROM Compras c
                INNER JOIN Usuarios u ON c.UsuarioId = u.Id
            ");

            // Backfill existing Devoluciones with user info
            migrationBuilder.Sql(@"
                UPDATE d
                SET d.UsuarioNombre = u.Nombre,
                    d.UsuarioApellido = u.Apellido,
                    d.UsuarioCorreo = u.Correo
                FROM Devoluciones d
                INNER JOIN Usuarios u ON d.UsuarioId = u.Id
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioApellido",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioCorreo",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioNombre",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioApellido",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "UsuarioCorreo",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "UsuarioNombre",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "UsuarioApellido",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "UsuarioCorreo",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "UsuarioNombre",
                table: "Devoluciones");
        }
    }
}
