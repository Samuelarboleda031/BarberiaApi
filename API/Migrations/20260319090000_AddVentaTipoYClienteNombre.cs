using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVentaTipoYClienteNombre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoVenta",
                table: "Ventas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "Venta Invitado");

            migrationBuilder.AddColumn<string>(
                name: "ClienteNombre",
                table: "Ventas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoVenta",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ClienteNombre",
                table: "Ventas");
        }
    }
}
