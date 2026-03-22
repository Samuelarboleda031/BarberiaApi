using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProveedorCampos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CargoRepLegal' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [CargoRepLegal];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'Ciudad' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [Ciudad];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'Departamento' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [Departamento];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'NumeroIdentificacionRepLegal' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [NumeroIdentificacionRepLegal];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'RazonSocial' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [RazonSocial];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'RepresentanteLegal' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [RepresentanteLegal];
");

            migrationBuilder.AlterColumn<string>(
                name: "TipoProveedor",
                table: "Proveedores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TipoIdentificacion",
                table: "Proveedores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroIdentificacion",
                table: "Proveedores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CorreoContacto' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] ADD [CorreoContacto] nvarchar(150) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TelefonoContacto' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] ADD [TelefonoContacto] nvarchar(50) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorreoContacto",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TelefonoContacto",
                table: "Proveedores");

            migrationBuilder.AlterColumn<string>(
                name: "TipoProveedor",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "TipoIdentificacion",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroIdentificacion",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoRepLegal",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroIdentificacionRepLegal",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazonSocial",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentanteLegal",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
