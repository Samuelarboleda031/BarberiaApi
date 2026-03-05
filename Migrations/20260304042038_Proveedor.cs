using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class Proveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_NIT",
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
                name: "Estado",
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

            migrationBuilder.RenameColumn(
                name: "NIT",
                table: "Proveedores",
                newName: "ContactoTelefono");

            migrationBuilder.RenameColumn(
                name: "Contacto",
                table: "Proveedores",
                newName: "ContactoNombre");

            migrationBuilder.AlterColumn<string>(
                name: "TipoProveedor",
                table: "Proveedores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ContactoCorreo",
                table: "Proveedores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "Proveedores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Proveedores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Documento",
                table: "Proveedores",
                column: "Documento",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_Documento",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "ContactoCorreo",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Proveedores");

            migrationBuilder.RenameColumn(
                name: "ContactoTelefono",
                table: "Proveedores",
                newName: "NIT");

            migrationBuilder.RenameColumn(
                name: "ContactoNombre",
                table: "Proveedores",
                newName: "Contacto");

            migrationBuilder.AlterColumn<string>(
                name: "TipoProveedor",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

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

            migrationBuilder.AddColumn<bool>(
                name: "Estado",
                table: "Proveedores",
                type: "bit",
                nullable: true,
                defaultValue: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NIT",
                table: "Proveedores",
                column: "NIT",
                unique: true,
                filter: "[NIT] IS NOT NULL");
        }
    }
}
