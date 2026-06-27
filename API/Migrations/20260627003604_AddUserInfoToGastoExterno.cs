using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserInfoToGastoExterno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioApellido",
                table: "GastosExternos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCorreo",
                table: "GastosExternos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioDocumento",
                table: "GastosExternos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioNombre",
                table: "GastosExternos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioTipoDocumento",
                table: "GastosExternos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioApellido",
                table: "GastosExternos");

            migrationBuilder.DropColumn(
                name: "UsuarioCorreo",
                table: "GastosExternos");

            migrationBuilder.DropColumn(
                name: "UsuarioDocumento",
                table: "GastosExternos");

            migrationBuilder.DropColumn(
                name: "UsuarioNombre",
                table: "GastosExternos");

            migrationBuilder.DropColumn(
                name: "UsuarioTipoDocumento",
                table: "GastosExternos");
        }
    }
}
