using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToHorarioSemanal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosBarberos");

            migrationBuilder.CreateTable(
                name: "HorariosSemanales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarberoId = table.Column<int>(type: "int", nullable: false),
                    FechaInicioSemana = table.Column<DateTime>(type: "date", nullable: false),
                    FechaFinSemana = table.Column<DateTime>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Activo")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosSemanales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosSemanales_Barberos_BarberoId",
                        column: x => x.BarberoId,
                        principalTable: "Barberos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetalleHorarioDias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HorarioSemanalId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleHorarioDias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalleHorarioDias_HorariosSemanales_HorarioSemanalId",
                        column: x => x.HorarioSemanalId,
                        principalTable: "HorariosSemanales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetalleHorarioDias_HorarioSemanalId",
                table: "DetalleHorarioDias",
                column: "HorarioSemanalId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosSemanales_BarberoId",
                table: "HorariosSemanales",
                column: "BarberoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetalleHorarioDias");

            migrationBuilder.DropTable(
                name: "HorariosSemanales");

            migrationBuilder.CreateTable(
                name: "HorariosBarberos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarberoId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosBarberos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosBarberos_Barberos_BarberoId",
                        column: x => x.BarberoId,
                        principalTable: "Barberos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosBarberos_BarberoId",
                table: "HorariosBarberos",
                column: "BarberoId");
        }
    }
}
