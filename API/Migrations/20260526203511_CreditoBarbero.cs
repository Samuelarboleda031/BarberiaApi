using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class CreditoBarbero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditoBarberoId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditoBarberoUsado",
                table: "Ventas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CreditosBarbero",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarberoId = table.Column<int>(type: "int", nullable: false),
                    CupoMaximo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 200000m),
                    SaldoDeuda = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Activo"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditosBarbero", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditosBarbero_Barberos_BarberoId",
                        column: x => x.BarberoId,
                        principalTable: "Barberos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AbonosCreditoBarbero",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditoBarberoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbonosCreditoBarbero", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbonosCreditoBarbero_CreditosBarbero_CreditoBarberoId",
                        column: x => x.CreditoBarberoId,
                        principalTable: "CreditosBarbero",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbonosCreditoBarbero_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_CreditoBarberoId",
                table: "Ventas",
                column: "CreditoBarberoId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonosCreditoBarbero_CreditoBarberoId",
                table: "AbonosCreditoBarbero",
                column: "CreditoBarberoId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonosCreditoBarbero_UsuarioId",
                table: "AbonosCreditoBarbero",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditosBarbero_BarberoId",
                table: "CreditosBarbero",
                column: "BarberoId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_CreditosBarbero_CreditoBarberoId",
                table: "Ventas",
                column: "CreditoBarberoId",
                principalTable: "CreditosBarbero",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_CreditosBarbero_CreditoBarberoId",
                table: "Ventas");

            migrationBuilder.DropTable(
                name: "AbonosCreditoBarbero");

            migrationBuilder.DropTable(
                name: "CreditosBarbero");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_CreditoBarberoId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CreditoBarberoId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CreditoBarberoUsado",
                table: "Ventas");
        }
    }
}
