using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class UnificarStockProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devoluciones_Barberos_BarberoId",
                table: "Devoluciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Devoluciones_EntregasInsumos_EntregaId",
                table: "Devoluciones");

            migrationBuilder.DropTable(
                name: "DetalleEntregasInsumos");

            migrationBuilder.DropTable(
                name: "EntregasInsumos");

            migrationBuilder.DropIndex(
                name: "IX_Devoluciones_BarberoId",
                table: "Devoluciones");

            migrationBuilder.DropIndex(
                name: "IX_Devoluciones_EntregaId",
                table: "Devoluciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Devolucion_Tipo",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "StockInsumos",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "StockTotal",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "BarberoId",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "EntregaId",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "CantidadInsumos",
                table: "DetalleCompras");

            migrationBuilder.DropColumn(
                name: "CantidadVentas",
                table: "DetalleCompras");

            migrationBuilder.RenameColumn(
                name: "StockVentas",
                table: "Productos",
                newName: "Stock");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Stock",
                table: "Productos",
                newName: "StockVentas");

            migrationBuilder.AddColumn<int>(
                name: "StockInsumos",
                table: "Productos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockTotal",
                table: "Productos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BarberoId",
                table: "Devoluciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntregaId",
                table: "Devoluciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CantidadInsumos",
                table: "DetalleCompras",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CantidadVentas",
                table: "DetalleCompras",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EntregasInsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarberoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CantidadTotal = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Entregado"),
                    Fecha = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "GETDATE()"),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregasInsumos_Barberos_BarberoId",
                        column: x => x.BarberoId,
                        principalTable: "Barberos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntregasInsumos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalleEntregasInsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntregaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioHistorico = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleEntregasInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalleEntregasInsumos_EntregasInsumos_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "EntregasInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetalleEntregasInsumos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_BarberoId",
                table: "Devoluciones",
                column: "BarberoId");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_EntregaId",
                table: "Devoluciones",
                column: "EntregaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Devolucion_Tipo",
                table: "Devoluciones",
                sql: "((VentaId IS NOT NULL AND BarberoId IS NULL AND EntregaId IS NULL AND ClienteId IS NOT NULL) OR (VentaId IS NULL AND BarberoId IS NOT NULL AND EntregaId IS NOT NULL AND ClienteId IS NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleEntregasInsumos_EntregaId",
                table: "DetalleEntregasInsumos",
                column: "EntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleEntregasInsumos_ProductoId",
                table: "DetalleEntregasInsumos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasInsumos_BarberoId",
                table: "EntregasInsumos",
                column: "BarberoId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasInsumos_UsuarioId",
                table: "EntregasInsumos",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devoluciones_Barberos_BarberoId",
                table: "Devoluciones",
                column: "BarberoId",
                principalTable: "Barberos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devoluciones_EntregasInsumos_EntregaId",
                table: "Devoluciones",
                column: "EntregaId",
                principalTable: "EntregasInsumos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
