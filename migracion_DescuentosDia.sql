-- ============================================================================
-- Migración: DescuentosDia
-- Crea la tabla de descuentos por día (se sincronizan entre el panel web y la
-- app móvil). Antes cada plataforma los guardaba localmente y no se compartían.
--
-- Seguro de ejecutar varias veces (idempotente): si la tabla o el registro de
-- migración ya existen, no hace nada.
--
-- Cómo aplicarlo: ejecutar este script contra la base de datos de la API
-- (SQL Server Management Studio, Azure Data Studio, o el cliente que uses).
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE [name] = 'DescuentosDia')
BEGIN
    CREATE TABLE [DescuentosDia] (
        [Id] int NOT NULL IDENTITY,
        [Fecha] date NOT NULL,
        [Porcentaje] decimal(5,2) NOT NULL,
        CONSTRAINT [PK_DescuentosDia] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_DescuentosDia_Fecha] ON [DescuentosDia] ([Fecha]);
END;
GO

-- Registrar la migración en el historial de EF Core para que 'dotnet ef' la
-- considere aplicada y no intente recrearla en el futuro.
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614184658_AddDescuentoDia'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614184658_AddDescuentoDia', N'8.0.11');
END;
GO
