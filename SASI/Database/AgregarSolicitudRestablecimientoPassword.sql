BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101045923_AgregarSolicitudRestablecimientoPassword'
)
BEGIN
    CREATE TABLE [SolicitudRestablecimientoPassword] (
        [IdSolicitud] int NOT NULL IDENTITY,
        [UsuarioId] uniqueidentifier NULL,
        [Identificador] nvarchar(max) NOT NULL,
        [UserName] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [NombreCompleto] nvarchar(max) NULL,
        [SistemaId] int NULL,
        [ClientId] nvarchar(max) NULL,
        [Mensaje] nvarchar(max) NULL,
        [Estado] int NOT NULL,
        [FechaSolicitud] datetime2 NOT NULL,
        [FechaRespuesta] datetime2 NULL,
        [AtendidoPor] nvarchar(max) NULL,
        [ComentarioRespuesta] nvarchar(max) NULL,
        [AuditUsuarioCreacion] nvarchar(max) NULL,
        [AuditFechaCreacion] datetime2 NULL,
        [IpCreacion] nvarchar(max) NULL,
        [AuditUsuarioModificacion] nvarchar(max) NULL,
        [AuditFechaModificacion] datetime2 NULL,
        [IpModificacion] nvarchar(max) NULL,
        CONSTRAINT [PK_SolicitudRestablecimientoPassword] PRIMARY KEY ([IdSolicitud])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101045923_AgregarSolicitudRestablecimientoPassword'
)
BEGIN
    CREATE INDEX [IX_SolicitudRestablecimientoPassword_Estado] ON [SolicitudRestablecimientoPassword] ([Estado]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101045923_AgregarSolicitudRestablecimientoPassword'
)
BEGIN
    CREATE INDEX [IX_SolicitudRestablecimientoPassword_FechaSolicitud] ON [SolicitudRestablecimientoPassword] ([FechaSolicitud]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101045923_AgregarSolicitudRestablecimientoPassword'
)
BEGIN
    CREATE INDEX [IX_SolicitudRestablecimientoPassword_UsuarioId] ON [SolicitudRestablecimientoPassword] ([UsuarioId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101045923_AgregarSolicitudRestablecimientoPassword'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101045923_AgregarSolicitudRestablecimientoPassword', N'8.0.10');
END;
GO

COMMIT;
GO

