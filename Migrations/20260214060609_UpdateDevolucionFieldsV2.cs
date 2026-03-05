using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDevolucionFieldsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devoluciones_Clientes_ClienteId",
                table: "Devoluciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Devoluciones_Servicios_ServicioId",
                table: "Devoluciones");

            migrationBuilder.DropIndex(
                name: "IX_Devoluciones_ServicioId",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "ServicioId",
                table: "Devoluciones");

            migrationBuilder.AddForeignKey(
                name: "FK_Devoluciones_Clientes_ClienteId",
                table: "Devoluciones",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devoluciones_Clientes_ClienteId",
                table: "Devoluciones");

            migrationBuilder.AddColumn<int>(
                name: "ServicioId",
                table: "Devoluciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_ServicioId",
                table: "Devoluciones",
                column: "ServicioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devoluciones_Clientes_ClienteId",
                table: "Devoluciones",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Devoluciones_Servicios_ServicioId",
                table: "Devoluciones",
                column: "ServicioId",
                principalTable: "Servicios",
                principalColumn: "Id");
        }
    }
}
