using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class Usuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Documento",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Barberos_Documento",
                table: "Barberos");

            migrationBuilder.DropColumn(
                name: "Barrio",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Apellido",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "FotoPerfil",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Apellido",
                table: "Barberos");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "Barberos");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "Barberos");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Barberos");

            migrationBuilder.RenameColumn(
                name: "FotoPerfil",
                table: "Barberos",
                newName: "Direccion");

            migrationBuilder.AlterColumn<string>(
                name: "FotoPerfil",
                table: "Usuarios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barrio",
                table: "Barberos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Barberos",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barrio",
                table: "Barberos");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Barberos");

            migrationBuilder.RenameColumn(
                name: "Direccion",
                table: "Barberos",
                newName: "FotoPerfil");

            migrationBuilder.AlterColumn<string>(
                name: "FotoPerfil",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barrio",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Usuarios",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Usuarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Usuarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Apellido",
                table: "Clientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Clientes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "Clientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FotoPerfil",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Clientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Apellido",
                table: "Barberos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Barberos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "Barberos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Barberos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Documento",
                table: "Clientes",
                column: "Documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Barberos_Documento",
                table: "Barberos",
                column: "Documento",
                unique: true);
        }
    }
}
