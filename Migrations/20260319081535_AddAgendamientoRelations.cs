using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendamientoRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgendamientoProductos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgendamientoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendamientoProductos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendamientoProductos_Agendamientos_AgendamientoId",
                        column: x => x.AgendamientoId,
                        principalTable: "Agendamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgendamientoProductos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgendamientoServicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgendamientoId = table.Column<int>(type: "int", nullable: false),
                    ServicioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendamientoServicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendamientoServicios_Agendamientos_AgendamientoId",
                        column: x => x.AgendamientoId,
                        principalTable: "Agendamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgendamientoServicios_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamientoProductos_AgendamientoId",
                table: "AgendamientoProductos",
                column: "AgendamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamientoProductos_ProductoId",
                table: "AgendamientoProductos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamientoServicios_AgendamientoId",
                table: "AgendamientoServicios",
                column: "AgendamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamientoServicios_ServicioId",
                table: "AgendamientoServicios",
                column: "ServicioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendamientoProductos");

            migrationBuilder.DropTable(
                name: "AgendamientoServicios");
        }
    }
}
