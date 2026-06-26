BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626223839_AddGastosExternos'
)
BEGIN
    CREATE TABLE [GastosExternos] (
        [Id] int NOT NULL IDENTITY,
        [Descripcion] nvarchar(500) NOT NULL,
        [Monto] decimal(18,2) NOT NULL,
        [Categoria] nvarchar(100) NOT NULL,
        [Fecha] date NOT NULL,
        [UsuarioId] int NOT NULL,
        [Notas] nvarchar(1000) NULL,
        [CreatedAt] datetime NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NULL,
        [DeletedAt] datetime2 NULL,
        [Estado] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_GastosExternos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GastosExternos_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626223839_AddGastosExternos'
)
BEGIN
    CREATE INDEX [IX_GastosExternos_DeletedAt] ON [GastosExternos] ([DeletedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626223839_AddGastosExternos'
)
BEGIN
    CREATE INDEX [IX_GastosExternos_Fecha] ON [GastosExternos] ([Fecha]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626223839_AddGastosExternos'
)
BEGIN
    CREATE INDEX [IX_GastosExternos_UsuarioId] ON [GastosExternos] ([UsuarioId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626223839_AddGastosExternos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626223839_AddGastosExternos', N'8.0.11');
END;
GO

COMMIT;
GO

