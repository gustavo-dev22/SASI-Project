BEGIN TRANSACTION;
GO

ALTER TABLE [Sistemas] ADD [AreaDuenaId] int NULL;
GO

ALTER TABLE [Sistemas] ADD [EstadoCicloVida] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Sistemas] ADD [FechaPuestaProduccion] datetime2 NULL;
GO

ALTER TABLE [Sistemas] ADD [FechaUltimaPruebaRestauracion] datetime2 NULL;
GO

ALTER TABLE [Sistemas] ADD [PoliticaRespaldo] nvarchar(max) NULL;
GO

ALTER TABLE [Sistemas] ADD [ResponsableFuncional] nvarchar(max) NULL;
GO

ALTER TABLE [Sistemas] ADD [ResponsableTecnico] nvarchar(max) NULL;
GO

ALTER TABLE [Sistemas] ADD [RpoHoras] int NULL;
GO

ALTER TABLE [Sistemas] ADD [RtoHoras] int NULL;
GO

ALTER TABLE [Sistemas] ADD [VersionActual] nvarchar(max) NULL;
GO

CREATE TABLE [SistemaContrato] (
    [IdSistemaContrato] int NOT NULL IDENTITY,
    [SistemaId] int NOT NULL,
    [Proveedor] nvarchar(max) NULL,
    [NroContrato] nvarchar(max) NULL,
    [FechaInicio] datetime2 NULL,
    [FechaFin] datetime2 NULL,
    [CostoAnual] decimal(18,2) NULL,
    [SLA_Detalle] nvarchar(max) NULL,
    [AuditUsuarioCreacion] nvarchar(max) NULL,
    [AuditFechaCreacion] datetime2 NULL,
    [IpCreacion] nvarchar(max) NULL,
    [AuditUsuarioModificacion] nvarchar(max) NULL,
    [AuditFechaModificacion] datetime2 NULL,
    [IpModificacion] nvarchar(max) NULL,
    CONSTRAINT [PK_SistemaContrato] PRIMARY KEY ([IdSistemaContrato]),
    CONSTRAINT [FK_SistemaContrato_Sistemas_SistemaId] FOREIGN KEY ([SistemaId]) REFERENCES [Sistemas] ([IdSistema]) ON DELETE NO ACTION
);
GO

CREATE TABLE [SistemaDocumento] (
    [IdSistemaDocumento] int NOT NULL IDENTITY,
    [SistemaId] int NOT NULL,
    [Titulo] nvarchar(max) NOT NULL,
    [TipoDoc] nvarchar(max) NOT NULL,
    [RutaArchivo] nvarchar(max) NULL,
    [FechaSubida] datetime2 NOT NULL,
    [UsuarioSubida] nvarchar(max) NULL,
    CONSTRAINT [PK_SistemaDocumento] PRIMARY KEY ([IdSistemaDocumento]),
    CONSTRAINT [FK_SistemaDocumento_Sistemas_SistemaId] FOREIGN KEY ([SistemaId]) REFERENCES [Sistemas] ([IdSistema]) ON DELETE NO ACTION
);
GO

CREATE TABLE [SistemaVersion] (
    [IdSistemaVersion] int NOT NULL IDENTITY,
    [SistemaId] int NOT NULL,
    [Version] nvarchar(max) NOT NULL,
    [Changelog] nvarchar(max) NULL,
    [Entorno] nvarchar(max) NULL,
    [FechaDespliegue] datetime2 NULL,
    [UsuarioDespliegue] nvarchar(max) NULL,
    [AuditUsuarioCreacion] nvarchar(max) NULL,
    [AuditFechaCreacion] datetime2 NULL,
    [IpCreacion] nvarchar(max) NULL,
    [AuditUsuarioModificacion] nvarchar(max) NULL,
    [AuditFechaModificacion] datetime2 NULL,
    [IpModificacion] nvarchar(max) NULL,
    CONSTRAINT [PK_SistemaVersion] PRIMARY KEY ([IdSistemaVersion]),
    CONSTRAINT [FK_SistemaVersion_Sistemas_SistemaId] FOREIGN KEY ([SistemaId]) REFERENCES [Sistemas] ([IdSistema]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_Sistemas_AreaDuenaId] ON [Sistemas] ([AreaDuenaId]);
GO

CREATE INDEX [IX_SistemaContrato_SistemaId] ON [SistemaContrato] ([SistemaId]);
GO

CREATE INDEX [IX_SistemaDocumento_SistemaId] ON [SistemaDocumento] ([SistemaId]);
GO

CREATE INDEX [IX_SistemaVersion_SistemaId] ON [SistemaVersion] ([SistemaId]);
GO

ALTER TABLE [Sistemas] ADD CONSTRAINT [FK_Sistemas_Oficina_AreaDuenaId] FOREIGN KEY ([AreaDuenaId]) REFERENCES [Oficina] ([IdOficina]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819151850_AgregarGobernanzaTISistema', N'8.0.10');
GO

COMMIT;
GO

