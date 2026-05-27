-- ============================================================
-- Migración: Refactor Sistema de Crédito Barberos
-- Ejecutar en orden, dentro de una transacción
-- ============================================================

BEGIN TRANSACTION;

BEGIN TRY

    -- --------------------------------------------------------
    -- 1. CreditosBarbero: eliminar índice único en BarberoId
    --    (un barbero ahora puede tener múltiples ciclos)
    -- --------------------------------------------------------
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CreditosBarbero_BarberoId' AND object_id = OBJECT_ID('CreditosBarbero'))
        DROP INDEX [IX_CreditosBarbero_BarberoId] ON [CreditosBarbero];

    -- --------------------------------------------------------
    -- 2. CreditosBarbero: renombrar columnas
    -- --------------------------------------------------------
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CreditosBarbero') AND name = 'CupoMaximo')
        EXEC sp_rename 'CreditosBarbero.CupoMaximo', 'LimiteCredito', 'COLUMN';

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CreditosBarbero') AND name = 'SaldoDeuda')
        EXEC sp_rename 'CreditosBarbero.SaldoDeuda', 'SaldoPendiente', 'COLUMN';

    -- --------------------------------------------------------
    -- 3. CreditosBarbero: expandir Estado a nvarchar(50)
    -- --------------------------------------------------------
    ALTER TABLE [CreditosBarbero] ALTER COLUMN [Estado] nvarchar(50) NOT NULL;

    -- --------------------------------------------------------
    -- 4. CreditosBarbero: agregar nuevas columnas (nullable primero)
    -- --------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CreditosBarbero') AND name = 'PlazoDias')
        ALTER TABLE [CreditosBarbero] ADD [PlazoDias] int NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CreditosBarbero') AND name = 'FechaInicio')
        ALTER TABLE [CreditosBarbero] ADD [FechaInicio] datetime NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CreditosBarbero') AND name = 'FechaVencimiento')
        ALTER TABLE [CreditosBarbero] ADD [FechaVencimiento] datetime NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CreditosBarbero') AND name = 'FechaCierre')
        ALTER TABLE [CreditosBarbero] ADD [FechaCierre] datetime NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CreditosBarbero') AND name = 'ExtensionUsada')
        ALTER TABLE [CreditosBarbero] ADD [ExtensionUsada] bit NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CreditosBarbero') AND name = 'CreadoPor')
        ALTER TABLE [CreditosBarbero] ADD [CreadoPor] int NULL;

    -- --------------------------------------------------------
    -- 5. CreditosBarbero: poblar datos en registros existentes
    --    FechaInicio = FechaCreacion
    --    FechaVencimiento = FechaCreacion + 7 días (a las 23:59:59)
    --    FechaCierre = NULL (los activos) — ajustar manualmente si aplica
    --    CreadoPor = 1 (ajustar al ID del admin si es diferente)
    -- --------------------------------------------------------
    UPDATE [CreditosBarbero] SET
        [PlazoDias]       = 7,
        [FechaInicio]     = [FechaCreacion],
        [FechaVencimiento]= DATEADD(SECOND, -1, DATEADD(DAY, 8, CAST(CAST([FechaCreacion] AS date) AS datetime))),
        [FechaCierre]     = NULL,
        [ExtensionUsada]  = 0,
        [CreadoPor]       = 1  -- << cambiar al ID real del usuario admin si es diferente de 1
    WHERE [PlazoDias] IS NULL;

    -- --------------------------------------------------------
    -- 6. CreditosBarbero: hacer columnas NOT NULL
    -- --------------------------------------------------------
    ALTER TABLE [CreditosBarbero] ALTER COLUMN [PlazoDias] int NOT NULL;
    ALTER TABLE [CreditosBarbero] ALTER COLUMN [FechaInicio] datetime NOT NULL;
    ALTER TABLE [CreditosBarbero] ALTER COLUMN [FechaVencimiento] datetime NOT NULL;
    ALTER TABLE [CreditosBarbero] ALTER COLUMN [ExtensionUsada] bit NOT NULL;
    ALTER TABLE [CreditosBarbero] ALTER COLUMN [CreadoPor] int NOT NULL;

    -- --------------------------------------------------------
    -- 7. CreditosBarbero: FK hacia Usuarios para CreadoPor
    -- --------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CreditosBarbero_Usuarios_CreadoPor')
        ALTER TABLE [CreditosBarbero]
            ADD CONSTRAINT [FK_CreditosBarbero_Usuarios_CreadoPor]
            FOREIGN KEY ([CreadoPor]) REFERENCES [Usuarios] ([Id]);

    -- --------------------------------------------------------
    -- 8. Ventas: agregar BarberoPrestadorId
    -- --------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ventas') AND name = 'BarberoPrestadorId')
        ALTER TABLE [Ventas] ADD [BarberoPrestadorId] int NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Ventas_Barberos_BarberoPrestadorId')
        ALTER TABLE [Ventas]
            ADD CONSTRAINT [FK_Ventas_Barberos_BarberoPrestadorId]
            FOREIGN KEY ([BarberoPrestadorId]) REFERENCES [Barberos] ([Id]) ON DELETE SET NULL;

    -- --------------------------------------------------------
    -- 9. AbonosCreditoBarbero: agregar VentaId
    -- --------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AbonosCreditoBarbero') AND name = 'VentaId')
        ALTER TABLE [AbonosCreditoBarbero] ADD [VentaId] int NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AbonosCreditoBarbero_Ventas_VentaId')
        ALTER TABLE [AbonosCreditoBarbero]
            ADD CONSTRAINT [FK_AbonosCreditoBarbero_Ventas_VentaId]
            FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE SET NULL;

    -- --------------------------------------------------------
    -- 10. AbonosCreditoBarbero: migrar Estado 'Activo' → 'Completado'
    -- --------------------------------------------------------
    UPDATE [AbonosCreditoBarbero] SET [Estado] = 'Completado' WHERE [Estado] = 'Activo';

    -- --------------------------------------------------------
    -- 11. Crear tabla HistorialEstadoCredito
    -- --------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HistorialEstadoCredito')
    BEGIN
        CREATE TABLE [HistorialEstadoCredito] (
            [Id]               int            NOT NULL IDENTITY(1,1),
            [CreditoBarberoId] int            NOT NULL,
            [EstadoAnterior]   nvarchar(50)   NOT NULL,
            [EstadoNuevo]      nvarchar(50)   NOT NULL,
            [FechaCambio]      datetime       NOT NULL DEFAULT GETDATE(),
            [ResponsableId]    int            NOT NULL,
            [Observacion]      nvarchar(500)  NULL,
            CONSTRAINT [PK_HistorialEstadoCredito] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_Historial_CreditosBarbero]
                FOREIGN KEY ([CreditoBarberoId]) REFERENCES [CreditosBarbero] ([Id]),
            CONSTRAINT [FK_Historial_Usuarios_Responsable]
                FOREIGN KEY ([ResponsableId]) REFERENCES [Usuarios] ([Id])
        );
    END

    COMMIT TRANSACTION;
    PRINT 'Migración completada exitosamente.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error durante la migración: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
