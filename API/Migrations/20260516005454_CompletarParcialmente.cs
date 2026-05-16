using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class CompletarParcialmente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioFinal",
                table: "Agendamientos",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductosPendientes",
                table: "Agendamientos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductosRealizados",
                table: "Agendamientos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiciosPendientes",
                table: "Agendamientos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiciosRealizados",
                table: "Agendamientos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VentaAsociadaId",
                table: "Agendamientos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agendamientos_VentaAsociadaId",
                table: "Agendamientos",
                column: "VentaAsociadaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamientos_Ventas_VentaAsociadaId",
                table: "Agendamientos",
                column: "VentaAsociadaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamientos_Ventas_VentaAsociadaId",
                table: "Agendamientos");

            migrationBuilder.DropIndex(
                name: "IX_Agendamientos_VentaAsociadaId",
                table: "Agendamientos");

            migrationBuilder.DropColumn(
                name: "PrecioFinal",
                table: "Agendamientos");

            migrationBuilder.DropColumn(
                name: "ProductosPendientes",
                table: "Agendamientos");

            migrationBuilder.DropColumn(
                name: "ProductosRealizados",
                table: "Agendamientos");

            migrationBuilder.DropColumn(
                name: "ServiciosPendientes",
                table: "Agendamientos");

            migrationBuilder.DropColumn(
                name: "ServiciosRealizados",
                table: "Agendamientos");

            migrationBuilder.DropColumn(
                name: "VentaAsociadaId",
                table: "Agendamientos");
        }
    }
}
