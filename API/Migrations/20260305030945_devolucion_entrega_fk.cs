using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class devolucion_entrega_fk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EntregaId",
                table: "Devoluciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_EntregaId",
                table: "Devoluciones",
                column: "EntregaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devoluciones_EntregasInsumos_EntregaId",
                table: "Devoluciones",
                column: "EntregaId",
                principalTable: "EntregasInsumos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devoluciones_EntregasInsumos_EntregaId",
                table: "Devoluciones");

            migrationBuilder.DropIndex(
                name: "IX_Devoluciones_EntregaId",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "EntregaId",
                table: "Devoluciones");
        }
    }
}
