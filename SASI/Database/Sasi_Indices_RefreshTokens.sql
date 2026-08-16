/* ============================================================
   SASI - Índices en claves foráneas + tabla RefreshTokens
   Script IDEMPOTENTE para aplicar de forma segura sobre la BD
   existente en producción (no pierde datos).
   Aplicar con: sqlcmd -S <servidor> -U <usuario> -P <clave> -d <bd> -i Sasi_Indices_RefreshTokens.sql
   ============================================================ */

-- Índices en claves foráneas (consultas frecuentes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_IdSistema' AND object_id = OBJECT_ID('Roles'))
    CREATE INDEX IX_Roles_IdSistema ON Roles(IdSistema);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UsuarioSistema_SistemaId' AND object_id = OBJECT_ID('UsuarioSistema'))
    CREATE INDEX IX_UsuarioSistema_SistemaId ON UsuarioSistema(SistemaId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UsuarioSistema_RolId' AND object_id = OBJECT_ID('UsuarioSistema'))
    CREATE INDEX IX_UsuarioSistema_RolId ON UsuarioSistema(RolId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UsuarioSistema_UsuarioId' AND object_id = OBJECT_ID('UsuarioSistema'))
    CREATE INDEX IX_UsuarioSistema_UsuarioId ON UsuarioSistema(UsuarioId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Objeto_IdSistema' AND object_id = OBJECT_ID('Objeto'))
    CREATE INDEX IX_Objeto_IdSistema ON Objeto(IdSistema);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Objeto_IdPadre' AND object_id = OBJECT_ID('Objeto'))
    CREATE INDEX IX_Objeto_IdPadre ON Objeto(IdPadre);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolObjeto_IdRol' AND object_id = OBJECT_ID('RolObjeto'))
    CREATE INDEX IX_RolObjeto_IdRol ON RolObjeto(IdRol);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolObjeto_IdObjeto' AND object_id = OBJECT_ID('RolObjeto'))
    CREATE INDEX IX_RolObjeto_IdObjeto ON RolObjeto(IdObjeto);
GO

-- Tabla RefreshTokens (soporte del flujo I9: renovación/revocación de sesiones)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE RefreshTokens (
        Id INT IDENTITY(1,1) NOT NULL,
        UsuarioId UNIQUEIDENTIFIER NOT NULL,
        TokenHash NVARCHAR(200) NOT NULL,
        ExpiresUtc DATETIME2 NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        RevokedUtc DATETIME2 NULL,
        ReplacedByTokenHash NVARCHAR(200) NULL,
        CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id)
    );

    CREATE INDEX IX_RefreshTokens_UsuarioId ON RefreshTokens(UsuarioId);
    CREATE UNIQUE INDEX IX_RefreshTokens_TokenHash ON RefreshTokens(TokenHash);
END
GO
