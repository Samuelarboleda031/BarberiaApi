using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class proveeedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

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
                name: "NumeroIdentificacion",
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

            migrationBuilder.AddColumn<string>(
                name: "TipoIdentificacion",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoProveedor",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImagenProduc",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Contrasena",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Contrasena",
                table: "Barberos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "CargoRepLegal",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "NumeroIdentificacion",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "NumeroIdentificacionRepLegal",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "RazonSocial",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "RepresentanteLegal",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TipoIdentificacion",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TipoProveedor",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "ImagenProduc",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Contrasena",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Contrasena",
                table: "Barberos");
        }
    }
}
