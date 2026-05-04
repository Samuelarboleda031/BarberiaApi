using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class EliminarCamposProveedorNoUsados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop seguro - solo si las columnas existen
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'AnosOperacion' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [AnosOperacion];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CargoRepresentante' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [CargoRepresentante];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'PaginaWeb' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [PaginaWeb];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'RazonSocial' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [RazonSocial];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'SectorEconomico' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [SectorEconomico];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnosOperacion",
                table: "Proveedores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoRepresentante",
                table: "Proveedores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaginaWeb",
                table: "Proveedores",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazonSocial",
                table: "Proveedores",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectorEconomico",
                table: "Proveedores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
