using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class modificacionesmodelos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamientos_Paquetes_PaqueteId",
                table: "Agendamientos");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePaquetes_Productos_ProductoId",
                table: "DetallePaquetes");

            migrationBuilder.AlterColumn<int>(
                name: "ServicioId",
                table: "DetallePaquetes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Cantidad",
                table: "DetallePaquetes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ServicioId",
                table: "Agendamientos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "HorariosBarberia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barbero = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Servicios = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "#007bff"),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaModificacion = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosBarberia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BloquesHorario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    HorarioBarberiaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloquesHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloquesHorario_HorariosBarberia_HorarioBarberiaId",
                        column: x => x.HorarioBarberiaId,
                        principalTable: "HorariosBarberia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloquesHorario_HorarioBarberiaId",
                table: "BloquesHorario",
                column: "HorarioBarberiaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamientos_Paquetes_PaqueteId",
                table: "Agendamientos",
                column: "PaqueteId",
                principalTable: "Paquetes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePaquetes_Productos_ProductoId",
                table: "DetallePaquetes",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamientos_Paquetes_PaqueteId",
                table: "Agendamientos");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePaquetes_Productos_ProductoId",
                table: "DetallePaquetes");

            migrationBuilder.DropTable(
                name: "BloquesHorario");

            migrationBuilder.DropTable(
                name: "HorariosBarberia");

            migrationBuilder.AlterColumn<int>(
                name: "ServicioId",
                table: "DetallePaquetes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Cantidad",
                table: "DetallePaquetes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ServicioId",
                table: "Agendamientos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamientos_Paquetes_PaqueteId",
                table: "Agendamientos",
                column: "PaqueteId",
                principalTable: "Paquetes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePaquetes_Productos_ProductoId",
                table: "DetallePaquetes",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
