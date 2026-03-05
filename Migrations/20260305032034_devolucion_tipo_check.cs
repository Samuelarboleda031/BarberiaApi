using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class devolucion_tipo_check : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Devolucion_Tipo",
                table: "Devoluciones",
                sql: "((VentaId IS NOT NULL AND BarberoId IS NULL AND EntregaId IS NULL AND ClienteId IS NOT NULL) OR (VentaId IS NULL AND BarberoId IS NOT NULL AND EntregaId IS NOT NULL AND ClienteId IS NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Devolucion_Tipo",
                table: "Devoluciones");
        }
    }
}
