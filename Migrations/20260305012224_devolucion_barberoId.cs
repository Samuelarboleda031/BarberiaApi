using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class devolucion_barberoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BarberoId",
                table: "Devoluciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_BarberoId",
                table: "Devoluciones",
                column: "BarberoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devoluciones_Barberos_BarberoId",
                table: "Devoluciones",
                column: "BarberoId",
                principalTable: "Barberos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devoluciones_Barberos_BarberoId",
                table: "Devoluciones");

            migrationBuilder.DropIndex(
                name: "IX_Devoluciones_BarberoId",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "BarberoId",
                table: "Devoluciones");
        }
    }
}
