BEGIN TRANSACTION;
GO

ALTER TABLE [Sistemas] ADD [EstadoOperativoActual] int NOT NULL DEFAULT 0;
GO

CREATE TABLE [EstadoOperativoSistema] (
    [IdEstadoOperativo] int NOT NULL IDENTITY,
    [SistemaId] int NOT NULL,
    [Estado] int NOT NULL,
    [Observacion] nvarchar(max) NULL,
    [FechaRegistro] datetime2 NOT NULL,
    [UsuarioRegistro] nvarchar(max) NULL,
    [AuditUsuarioCreacion] nvarchar(max) NULL,
    [AuditFechaCreacion] datetime2 NULL,
    [IpCreacion] nvarchar(max) NULL,
    [AuditUsuarioModificacion] nvarchar(max) NULL,
    [AuditFechaModificacion] datetime2 NULL,
    [IpModificacion] nvarchar(max) NULL,
    CONSTRAINT [PK_EstadoOperativoSistema] PRIMARY KEY ([IdEstadoOperativo]),
    CONSTRAINT [FK_EstadoOperativoSistema_Sistemas_SistemaId] FOREIGN KEY ([SistemaId]) REFERENCES [Sistemas] ([IdSistema]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Incidencia] (
    [IdIncidencia] int NOT NULL IDENTITY,
    [SistemaId] int NOT NULL,
    [Titulo] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NOT NULL,
    [Prioridad] int NOT NULL,
    [Estado] int NOT NULL,
    [Responsable] nvarchar(max) NULL,
    [FechaReporte] datetime2 NOT NULL,
    [FechaAtencion] datetime2 NULL,
    [FechaCierre] datetime2 NULL,
    [UsuarioReporte] nvarchar(max) NULL,
    [AuditUsuarioCreacion] nvarchar(max) NULL,
    [AuditFechaCreacion] datetime2 NULL,
    [IpCreacion] nvarchar(max) NULL,
    [AuditUsuarioModificacion] nvarchar(max) NULL,
    [AuditFechaModificacion] datetime2 NULL,
    [IpModificacion] nvarchar(max) NULL,
    CONSTRAINT [PK_Incidencia] PRIMARY KEY ([IdIncidencia]),
    CONSTRAINT [FK_Incidencia_Sistemas_SistemaId] FOREIGN KEY ([SistemaId]) REFERENCES [Sistemas] ([IdSistema]) ON DELETE NO ACTION
);
GO

CREATE TABLE [SolicitudAcceso] (
    [IdSolicitud] int NOT NULL IDENTITY,
    [UsuarioId] uniqueidentifier NOT NULL,
    [SistemaId] int NOT NULL,
    [RolId] int NOT NULL,
    [Justificacion] nvarchar(max) NULL,
    [Estado] int NOT NULL,
    [FechaSolicitud] datetime2 NOT NULL,
    [FechaRespuesta] datetime2 NULL,
    [AprobadoPor] nvarchar(max) NULL,
    [ComentarioRespuesta] nvarchar(max) NULL,
    [AuditUsuarioCreacion] nvarchar(max) NULL,
    [AuditFechaCreacion] datetime2 NULL,
    [IpCreacion] nvarchar(max) NULL,
    [AuditUsuarioModificacion] nvarchar(max) NULL,
    [AuditFechaModificacion] datetime2 NULL,
    [IpModificacion] nvarchar(max) NULL,
    CONSTRAINT [PK_SolicitudAcceso] PRIMARY KEY ([IdSolicitud]),
    CONSTRAINT [FK_SolicitudAcceso_Roles_RolId] FOREIGN KEY ([RolId]) REFERENCES [Roles] ([IdRol]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SolicitudAcceso_Sistemas_SistemaId] FOREIGN KEY ([SistemaId]) REFERENCES [Sistemas] ([IdSistema]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_EstadoOperativoSistema_SistemaId] ON [EstadoOperativoSistema] ([SistemaId]);
GO

CREATE INDEX [IX_Incidencia_SistemaId] ON [Incidencia] ([SistemaId]);
GO

CREATE INDEX [IX_SolicitudAcceso_RolId] ON [SolicitudAcceso] ([RolId]);
GO

CREATE INDEX [IX_SolicitudAcceso_SistemaId] ON [SolicitudAcceso] ([SistemaId]);
GO

CREATE INDEX [IX_SolicitudAcceso_UsuarioId] ON [SolicitudAcceso] ([UsuarioId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819164232_AgregarOperacionSoporte', N'8.0.10');
GO

COMMIT;
GO

