using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDescuentoDia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTA: la columna Compras.NumeroRecibo ya existe en la BD (cambio pendiente
            // del snapshot, en uso desde CompraService). No se recrea aquí para no romper
            // bases de datos existentes. Esta migración solo introduce DescuentosDia.
            migrationBuilder.CreateTable(
                name: "DescuentosDia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescuentosDia", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DescuentosDia_Fecha",
                table: "DescuentosDia",
                column: "Fecha",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DescuentosDia");
        }
    }
}
