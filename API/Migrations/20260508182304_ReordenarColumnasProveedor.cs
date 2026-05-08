using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class ReordenarColumnasProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 0: Clean up any previous failed attempts
            migrationBuilder.Sql(@"
IF OBJECT_ID('Proveedores_old', 'U') IS NOT NULL
    DROP TABLE [Proveedores_old];
");

            // Step 1: Delete index and rename table
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proveedores_Identificacion' AND object_id = OBJECT_ID(N'[Proveedores]'))
    DROP INDEX [IX_Proveedores_Identificacion] ON [Proveedores];
EXEC sp_rename 'Proveedores', 'Proveedores_old';
");

            // Step 2: Create new table with columns in correct order (without explicit PK constraint name)
            migrationBuilder.Sql(@"
CREATE TABLE [Proveedores] (
    [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [TipoProveedor] nvarchar(20) NOT NULL,
    [Nombre] nvarchar(150) NOT NULL,
    [TipoIdentificacionProveedor] nvarchar(40) NULL,
    [Identificacion] nvarchar(50) NULL,
    [Correo] nvarchar(150) NULL,
    [Telefono] nvarchar(50) NULL,
    [Direccion] nvarchar(200) NULL,
    [Ciudad] nvarchar(100) NULL,
    [Departamento] nvarchar(100) NULL,
    [RepresentanteLegal] nvarchar(150) NULL,
    [TipoIdentificacionRepresentante] nvarchar(40) NULL,
    [IdentificacionRepresentante] nvarchar(50) NULL,
    [CorreoRepresentante] nvarchar(150) NULL,
    [TelefonoRepresentante] nvarchar(50) NULL,
    [Estado] bit NULL DEFAULT 1
);
");

            // Step 3: Copy data from old table to new table
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [Proveedores] ON;
INSERT INTO [Proveedores]
    ([Id], [TipoProveedor], [Nombre], [TipoIdentificacionProveedor], [Identificacion], [Correo], [Telefono],
     [Direccion], [Ciudad], [Departamento], [RepresentanteLegal], [TipoIdentificacionRepresentante],
     [IdentificacionRepresentante], [CorreoRepresentante], [TelefonoRepresentante], [Estado])
SELECT
    [Id], [TipoProveedor], [Nombre], [TipoIdentificacionProveedor], [Identificacion], [Correo], [Telefono],
    [Direccion], [Ciudad], [Departamento], [RepresentanteLegal], [TipoIdentificacionRepresentante],
    [IdentificacionRepresentante], [CorreoRepresentante], [TelefonoRepresentante], [Estado]
FROM [Proveedores_old]
ORDER BY [Id];
SET IDENTITY_INSERT [Proveedores] OFF;
");

            // Step 4: Recreate index and foreign key
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX [IX_Proveedores_Identificacion] ON [Proveedores] ([Identificacion]) WHERE [Identificacion] IS NOT NULL;

-- Drop existing FK if it exists (it might be pointing to the old table)
IF OBJECT_ID('FK_Compras_Proveedores_ProveedorId', 'F') IS NOT NULL
    ALTER TABLE [Compras] DROP CONSTRAINT [FK_Compras_Proveedores_ProveedorId];

-- Create new FK pointing to the new table
ALTER TABLE [Compras] ADD CONSTRAINT [FK_Compras_Proveedores_ProveedorId] FOREIGN KEY ([ProveedorId]) REFERENCES [Proveedores] ([Id]) ON DELETE NO ACTION;
");

            // Step 5: Drop old table
            migrationBuilder.Sql(@"
DROP TABLE [Proveedores_old];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Revertir: renombrar tabla actual a vieja
EXEC sp_rename 'Proveedores', 'Proveedores_new';

-- Renombrar la tabla original que puede estar como backup
IF OBJECT_ID('Proveedores_old', 'U') IS NOT NULL
BEGIN
    EXEC sp_rename 'Proveedores_old', 'Proveedores';
END;

-- Eliminar tabla nueva si existe
IF OBJECT_ID('Proveedores_new', 'U') IS NOT NULL
BEGIN
    DROP TABLE [Proveedores_new];
END;
");
        }
    }
}
