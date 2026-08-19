-- =============================================================
-- SASI: Registrar los objetos de menu del modulo Operacion y Soporte
-- =============================================================
-- Modulos: Incidencias (mesa de ayuda), Solicitudes de Acceso y Monitoreo.
-- El menu de SASI se construye desde la tabla Objeto (IdSistema = 18)
-- y los permisos por rol desde RolObjeto.
-- El rol administrativo del sistema SASI es "Administrador de Seguridad".
-- El script es IDEMPOTENTE: puede ejecutarse mas de una vez sin duplicar.
-- =============================================================

DECLARE @IdSistemaSasi INT = (SELECT IdSistema FROM dbo.Sistemas WHERE IdSistema = 18);
IF @IdSistemaSasi IS NULL
BEGIN
    SET @IdSistemaSasi = (SELECT TOP 1 IdSistema FROM dbo.Sistemas WHERE Nombre LIKE 'Sistema de Administracion de Sistemas de Informacion%');
END

-- Rol administrativo del sistema SASI
DECLARE @IdRolAdmin INT = (
    SELECT TOP 1 IdRol
    FROM dbo.Roles
    WHERE IdSistema = @IdSistemaSasi
      AND Nombre IN ('Administrador', 'Administrador de Seguridad')
      AND Activo = 1
);

-- -------------------------------------------------------------
-- Tabla temporal con los objetos de menu a registrar
-- -------------------------------------------------------------
DECLARE @Objetos TABLE (Nombre NVARCHAR(200), Tipo NVARCHAR(50), Url NVARCHAR(300), Icono NVARCHAR(100), Titulo NVARCHAR(300));

INSERT INTO @Objetos (Nombre, Tipo, Url, Icono, Titulo) VALUES
    ('Incidencias', 'Menu', 'Soporte/Incidencias', 'bi bi-ticket-detailed-fill', 'Mesa de Ayuda: Incidencias'),
    ('Solicitudes de Acceso', 'Menu', 'Soporte/Solicitudes', 'bi bi-person-check-fill', 'Solicitudes de Acceso'),
    ('Monitoreo', 'Menu', 'Soporte/Monitoreo', 'bi bi-activity', 'Monitoreo y Estado Operativo');

-- -------------------------------------------------------------
-- Insertar cada objeto si no existe
-- -------------------------------------------------------------
DECLARE @Nombre NVARCHAR(200), @Tipo NVARCHAR(50), @Url NVARCHAR(300), @Icono NVARCHAR(100), @Titulo NVARCHAR(300), @IdObjeto INT;

DECLARE cur CURSOR FOR SELECT Nombre, Tipo, Url, Icono, Titulo FROM @Objetos;
OPEN cur;
FETCH NEXT FROM cur INTO @Nombre, @Tipo, @Url, @Icono, @Titulo;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Objeto WHERE Url = @Url AND IdSistema = @IdSistemaSasi)
    BEGIN
        INSERT INTO dbo.Objeto (
            Nombre, Tipo, Url, Icono, Titulo, Activo, Orden, IdPadre, IdSistema,
            AuditUsuarioCreacion, AuditFechaCreacion, IpCreacion
        )
        VALUES (
            @Nombre, @Tipo, @Url, @Icono, @Titulo, 1,
            (SELECT ISNULL(MAX(Orden), 0) + 1 FROM dbo.Objeto WHERE IdSistema = @IdSistemaSasi),
            NULL, @IdSistemaSasi,
            'system', GETDATE(), 'script'
        );
    END

    SELECT @IdObjeto = IdObjeto FROM dbo.Objeto WHERE Url = @Url AND IdSistema = @IdSistemaSasi;

    IF @IdObjeto IS NOT NULL AND @IdRolAdmin IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RolObjeto WHERE IdRol = @IdRolAdmin AND IdObjeto = @IdObjeto)
    BEGIN
        INSERT INTO dbo.RolObjeto (
            IdRol, IdObjeto, Activo,
            AuditUsuarioCreacion, AuditFechaCreacion, IpCreacion
        )
        VALUES (
            @IdRolAdmin, @IdObjeto, 1,
            'system', GETDATE(), 'script'
        );
    END

    FETCH NEXT FROM cur INTO @Nombre, @Tipo, @Url, @Icono, @Titulo;
END
CLOSE cur;
DEALLOCATE cur;

-- -------------------------------------------------------------
-- Verificacion
-- -------------------------------------------------------------
SELECT
    o.IdObjeto, o.Nombre, o.Url, o.Orden, o.Activo AS ObjetoActivo,
    r.IdRol, r.Nombre AS Rol, ro.Activo AS RolObjetoActivo
FROM dbo.Objeto o
LEFT JOIN dbo.RolObjeto ro ON ro.IdObjeto = o.IdObjeto
LEFT JOIN dbo.Roles r ON r.IdRol = ro.IdRol
WHERE o.IdSistema = @IdSistemaSasi
  AND o.Url LIKE 'Soporte/%'
ORDER BY o.Orden;
