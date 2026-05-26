using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTipoProducto_AddEstadoAbono : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Productos");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "AbonosCreditoBarbero",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "AbonosCreditoBarbero");

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
