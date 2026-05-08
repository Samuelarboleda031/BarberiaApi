using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarCamposProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Quitar el índice único antiguo sobre NIT (si existe)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proveedores_NIT' AND object_id = OBJECT_ID(N'[Proveedores]'))
    DROP INDEX [IX_Proveedores_NIT] ON [Proveedores];
");

            // 2. Renombrar columnas existentes
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'NIT' AND Object_ID = Object_ID(N'[Proveedores]'))
    EXEC sp_rename '[Proveedores].[NIT]', 'Identificacion', 'COLUMN';
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'NumeroIdentificacion' AND Object_ID = Object_ID(N'[Proveedores]'))
    EXEC sp_rename '[Proveedores].[NumeroIdentificacion]', 'IdentificacionRepresentante', 'COLUMN';
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TipoIdentificacion' AND Object_ID = Object_ID(N'[Proveedores]'))
    EXEC sp_rename '[Proveedores].[TipoIdentificacion]', 'TipoIdentificacionRepresentante', 'COLUMN';
");

            // 3. Ajustar tamaño de TipoIdentificacionRepresentante a 40
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TipoIdentificacionRepresentante' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] ALTER COLUMN [TipoIdentificacionRepresentante] nvarchar(40) NULL;
");

            // 4. Agregar nueva columna TipoIdentificacionProveedor
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TipoIdentificacionProveedor' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] ADD [TipoIdentificacionProveedor] nvarchar(40) NULL;
");

            // 5. Backfill: si TipoIdentificacionProveedor está NULL, asignar valor por defecto según TipoProveedor
            migrationBuilder.Sql(@"
UPDATE [Proveedores]
SET [TipoIdentificacionProveedor] = CASE WHEN [TipoProveedor] = 'Juridico' THEN N'NIT' ELSE N'CC' END
WHERE [TipoIdentificacionProveedor] IS NULL;
");

            // 6. Crear índice único sobre Identificacion
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proveedores_Identificacion' AND object_id = OBJECT_ID(N'[Proveedores]'))
    CREATE UNIQUE INDEX [IX_Proveedores_Identificacion] ON [Proveedores] ([Identificacion]) WHERE [Identificacion] IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir índice
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proveedores_Identificacion' AND object_id = OBJECT_ID(N'[Proveedores]'))
    DROP INDEX [IX_Proveedores_Identificacion] ON [Proveedores];
");

            // Eliminar columna nueva
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TipoIdentificacionProveedor' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] DROP COLUMN [TipoIdentificacionProveedor];
");

            // Renombrar de vuelta
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TipoIdentificacionRepresentante' AND Object_ID = Object_ID(N'[Proveedores]'))
    EXEC sp_rename '[Proveedores].[TipoIdentificacionRepresentante]', 'TipoIdentificacion', 'COLUMN';
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IdentificacionRepresentante' AND Object_ID = Object_ID(N'[Proveedores]'))
    EXEC sp_rename '[Proveedores].[IdentificacionRepresentante]', 'NumeroIdentificacion', 'COLUMN';
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'Identificacion' AND Object_ID = Object_ID(N'[Proveedores]'))
    EXEC sp_rename '[Proveedores].[Identificacion]', 'NIT', 'COLUMN';
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TipoIdentificacion' AND Object_ID = Object_ID(N'[Proveedores]'))
    ALTER TABLE [Proveedores] ALTER COLUMN [TipoIdentificacion] nvarchar(20) NULL;
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proveedores_NIT' AND object_id = OBJECT_ID(N'[Proveedores]'))
    CREATE UNIQUE INDEX [IX_Proveedores_NIT] ON [Proveedores] ([NIT]) WHERE [NIT] IS NOT NULL;
");
        }
    }
}
