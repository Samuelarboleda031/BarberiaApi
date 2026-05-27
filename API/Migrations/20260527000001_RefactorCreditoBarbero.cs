using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCreditoBarbero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --------------------------------------------------------
            // CreditosBarbero: renombrar columnas
            // --------------------------------------------------------
            migrationBuilder.RenameColumn(
                name: "CupoMaximo",
                table: "CreditosBarbero",
                newName: "LimiteCredito");

            migrationBuilder.RenameColumn(
                name: "SaldoDeuda",
                table: "CreditosBarbero",
                newName: "SaldoPendiente");

            // --------------------------------------------------------
            // CreditosBarbero: eliminar índice único en BarberoId
            // (un barbero ahora puede tener múltiples ciclos)
            // --------------------------------------------------------
            migrationBuilder.DropIndex(
                name: "IX_CreditosBarbero_BarberoId",
                table: "CreditosBarbero");

            // --------------------------------------------------------
            // CreditosBarbero: eliminar FechaActualizacion
            // (reemplazada por HistorialEstadoCredito)
            // --------------------------------------------------------
            migrationBuilder.DropColumn(
                name: "FechaActualizacion",
                table: "CreditosBarbero");

            // --------------------------------------------------------
            // CreditosBarbero: expandir Estado a nvarchar(50)
            // --------------------------------------------------------
            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "CreditosBarbero",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Activo",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Activo");

            // --------------------------------------------------------
            // CreditosBarbero: agregar nuevas columnas
            // --------------------------------------------------------
            migrationBuilder.AddColumn<int>(
                name: "PlazoDias",
                table: "CreditosBarbero",
                type: "int",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicio",
                table: "CreditosBarbero",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "CreditosBarbero",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCierre",
                table: "CreditosBarbero",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExtensionUsada",
                table: "CreditosBarbero",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CreadoPor",
                table: "CreditosBarbero",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // --------------------------------------------------------
            // CreditosBarbero: poblar datos en registros existentes
            // --------------------------------------------------------
            migrationBuilder.Sql(@"
                UPDATE [CreditosBarbero] SET
                    [FechaInicio]      = [FechaCreacion],
                    [FechaVencimiento] = DATEADD(SECOND, -1, DATEADD(DAY, 8, CAST(CAST([FechaCreacion] AS date) AS datetime))),
                    [PlazoDias]        = 7,
                    [ExtensionUsada]   = 0,
                    [CreadoPor]        = (SELECT TOP 1 [Id] FROM [Usuarios] ORDER BY [Id])
            ");

            // --------------------------------------------------------
            // CreditosBarbero: FK hacia Usuarios para CreadoPor
            // --------------------------------------------------------
            migrationBuilder.AddForeignKey(
                name: "FK_CreditosBarbero_Usuarios_CreadoPor",
                table: "CreditosBarbero",
                column: "CreadoPor",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // --------------------------------------------------------
            // Ventas: agregar BarberoPrestadorId
            // --------------------------------------------------------
            migrationBuilder.AddColumn<int>(
                name: "BarberoPrestadorId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_BarberoPrestadorId",
                table: "Ventas",
                column: "BarberoPrestadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Barberos_BarberoPrestadorId",
                table: "Ventas",
                column: "BarberoPrestadorId",
                principalTable: "Barberos",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            // --------------------------------------------------------
            // AbonosCreditoBarbero: agregar VentaId
            // --------------------------------------------------------
            migrationBuilder.AddColumn<int>(
                name: "VentaId",
                table: "AbonosCreditoBarbero",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbonosCreditoBarbero_VentaId",
                table: "AbonosCreditoBarbero",
                column: "VentaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AbonosCreditoBarbero_Ventas_VentaId",
                table: "AbonosCreditoBarbero",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // --------------------------------------------------------
            // AbonosCreditoBarbero: ajustar Estado (default + maxlength)
            // --------------------------------------------------------
            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "AbonosCreditoBarbero",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Completado",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);

            // Migrar registros existentes: Activo → Completado
            migrationBuilder.Sql(@"
                UPDATE [AbonosCreditoBarbero]
                SET [Estado] = 'Completado'
                WHERE [Estado] = 'Activo'
            ");

            // --------------------------------------------------------
            // Crear tabla HistorialEstadoCredito
            // --------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "HistorialEstadoCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditoBarberoId = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    ResponsableId = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstadoCredito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEstadoCredito_CreditosBarbero_CreditoBarberoId",
                        column: x => x.CreditoBarberoId,
                        principalTable: "CreditosBarbero",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialEstadoCredito_Usuarios_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadoCredito_CreditoBarberoId",
                table: "HistorialEstadoCredito",
                column: "CreditoBarberoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadoCredito_ResponsableId",
                table: "HistorialEstadoCredito",
                column: "ResponsableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HistorialEstadoCredito");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Barberos_BarberoPrestadorId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_BarberoPrestadorId",
                table: "Ventas");

            migrationBuilder.DropColumn(name: "BarberoPrestadorId", table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_AbonosCreditoBarbero_Ventas_VentaId",
                table: "AbonosCreditoBarbero");

            migrationBuilder.DropIndex(
                name: "IX_AbonosCreditoBarbero_VentaId",
                table: "AbonosCreditoBarbero");

            migrationBuilder.DropColumn(name: "VentaId", table: "AbonosCreditoBarbero");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditosBarbero_Usuarios_CreadoPor",
                table: "CreditosBarbero");

            migrationBuilder.DropColumn(name: "PlazoDias",       table: "CreditosBarbero");
            migrationBuilder.DropColumn(name: "FechaInicio",     table: "CreditosBarbero");
            migrationBuilder.DropColumn(name: "FechaVencimiento",table: "CreditosBarbero");
            migrationBuilder.DropColumn(name: "FechaCierre",     table: "CreditosBarbero");
            migrationBuilder.DropColumn(name: "ExtensionUsada",  table: "CreditosBarbero");
            migrationBuilder.DropColumn(name: "CreadoPor",       table: "CreditosBarbero");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaActualizacion",
                table: "CreditosBarbero",
                type: "datetime",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "CreditosBarbero",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Activo",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_CreditosBarbero_BarberoId",
                table: "CreditosBarbero",
                column: "BarberoId",
                unique: true);

            migrationBuilder.RenameColumn(
                name: "LimiteCredito",
                table: "CreditosBarbero",
                newName: "CupoMaximo");

            migrationBuilder.RenameColumn(
                name: "SaldoPendiente",
                table: "CreditosBarbero",
                newName: "SaldoDeuda");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "AbonosCreditoBarbero",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Completado");
        }
    }
}
