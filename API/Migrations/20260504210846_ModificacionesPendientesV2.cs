using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class ModificacionesPendientesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renombrado semántico: Contacto -> RepresentanteLegal (preserva datos legados)
            migrationBuilder.RenameColumn(
                name: "Contacto",
                table: "Proveedores",
                newName: "RepresentanteLegal");

            migrationBuilder.RenameColumn(
                name: "CorreoContacto",
                table: "Proveedores",
                newName: "CorreoRepresentante");

            migrationBuilder.RenameColumn(
                name: "TelefonoContacto",
                table: "Proveedores",
                newName: "TelefonoRepresentante");

            // Cambia max length de RepresentanteLegal: era 100 (Contacto), ahora 150
            migrationBuilder.AlterColumn<string>(
                name: "RepresentanteLegal",
                table: "Proveedores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroRecibo",
                table: "Ventas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "Proveedores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Proveedores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SolicitudesCambioHorario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarberoId = table.Column<int>(type: "int", nullable: false),
                    MotivoCategoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MotivoDetalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaReferencia = table.Column<DateTime>(type: "datetime", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    ObservacionAdmin = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaResolucion = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioResolucionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCambioHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesCambioHorario_Barberos_BarberoId",
                        column: x => x.BarberoId,
                        principalTable: "Barberos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCambioHorario_Usuarios_UsuarioResolucionId",
                        column: x => x.UsuarioResolucionId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SugerenciasCambioHorario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitudId = table.Column<int>(type: "int", nullable: false),
                    DiaSugerido = table.Column<DateTime>(type: "datetime", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Barbero")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SugerenciasCambioHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SugerenciasCambioHorario_SolicitudesCambioHorario_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "SolicitudesCambioHorario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_NumeroRecibo",
                table: "Ventas",
                column: "NumeroRecibo",
                unique: true,
                filter: "[NumeroRecibo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioHorario_BarberoId",
                table: "SolicitudesCambioHorario",
                column: "BarberoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioHorario_UsuarioResolucionId",
                table: "SolicitudesCambioHorario",
                column: "UsuarioResolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_SugerenciasCambioHorario_SolicitudId",
                table: "SugerenciasCambioHorario",
                column: "SolicitudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SugerenciasCambioHorario");

            migrationBuilder.DropTable(
                name: "SolicitudesCambioHorario");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_NumeroRecibo",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "NumeroRecibo",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Proveedores");

            migrationBuilder.AlterColumn<string>(
                name: "RepresentanteLegal",
                table: "Proveedores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "TelefonoRepresentante",
                table: "Proveedores",
                newName: "TelefonoContacto");

            migrationBuilder.RenameColumn(
                name: "CorreoRepresentante",
                table: "Proveedores",
                newName: "CorreoContacto");

            migrationBuilder.RenameColumn(
                name: "RepresentanteLegal",
                table: "Proveedores",
                newName: "Contacto");
        }
    }
}
