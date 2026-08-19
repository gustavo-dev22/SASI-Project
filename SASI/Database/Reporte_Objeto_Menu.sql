-- =============================================================
-- SASI: Registrar el Objeto de menu "Reportes" y asociarlo al rol
-- =============================================================
-- El menu de SASI se construye desde la tabla Objeto (IdSistema = 18)
-- y los permisos por rol desde RolObjeto.
--
-- El rol administrativo del sistema SASI es "Administrador de Seguridad".
-- Si en el futuro se crea un rol "Administrador" en el sistema SASI,
-- basta con ejecutar el bloque 2 con el IdRol correspondiente.
--
-- El script es IDEMPOTENTE: puede ejecutarse mas de una vez sin duplicar.
-- =============================================================

DECLARE @IdSistemaSasi INT = (SELECT IdSistema FROM dbo.Sistemas WHERE IdSistema = 18);
IF @IdSistemaSasi IS NULL
BEGIN
    -- Fallback: buscar por el nombre oficial del sistema SASI
    SET @IdSistemaSasi = (SELECT TOP 1 IdSistema FROM dbo.Sistemas WHERE Nombre LIKE 'Sistema de Administracion de Sistemas de Informacion%');
END

-- -------------------------------------------------------------
-- 1) Insertar el Objeto de menu "Reportes" (si no existe)
-- -------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Objeto WHERE Url = 'Reporte/Index' AND IdSistema = @IdSistemaSasi)
BEGIN
    INSERT INTO dbo.Objeto (
        Nombre, Tipo, Url, Icono, Titulo, Activo, Orden, IdPadre, IdSistema,
        AuditUsuarioCreacion, AuditFechaCreacion, IpCreacion
    )
    VALUES (
        'Reportes', 'Menu', 'Reporte/Index', 'bi bi-clipboard-data-fill', 'Reportes y Consultas', 1,
        (SELECT ISNULL(MAX(Orden), 0) + 1 FROM dbo.Objeto WHERE IdSistema = @IdSistemaSasi),
        NULL, @IdSistemaSasi,
        'system', GETDATE(), 'script'
    );
END

-- -------------------------------------------------------------
-- 2) Asociar el objeto al rol administrativo (Administrador de Seguridad)
--    en RolObjeto
-- -------------------------------------------------------------
DECLARE @IdObjetoReportes INT = (SELECT IdObjeto FROM dbo.Objeto WHERE Url = 'Reporte/Index' AND IdSistema = @IdSistemaSasi);

-- Rol administrativo: Administrador de Seguridad (u "Administrador" si existiera en el sistema SASI)
DECLARE @IdRolAdmin INT = (
    SELECT TOP 1 IdRol
    FROM dbo.Roles
    WHERE IdSistema = @IdSistemaSasi
      AND Nombre IN ('Administrador', 'Administrador de Seguridad')
      AND Activo = 1
);

IF @IdObjetoReportes IS NOT NULL AND @IdRolAdmin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RolObjeto WHERE IdRol = @IdRolAdmin AND IdObjeto = @IdObjetoReportes)
BEGIN
    INSERT INTO dbo.RolObjeto (
        IdRol, IdObjeto, Activo,
        AuditUsuarioCreacion, AuditFechaCreacion, IpCreacion
    )
    VALUES (
        @IdRolAdmin, @IdObjetoReportes, 1,
        'system', GETDATE(), 'script'
    );
END

-- -------------------------------------------------------------
-- 3) Verificacion
-- -------------------------------------------------------------
SELECT
    o.IdObjeto,
    o.Nombre,
    o.Url,
    o.Orden,
    o.Activo AS ObjetoActivo,
    r.IdRol,
    r.Nombre AS Rol,
    ro.Activo AS RolObjetoActivo
FROM dbo.Objeto o
LEFT JOIN dbo.RolObjeto ro ON ro.IdObjeto = o.IdObjeto
LEFT JOIN dbo.Roles r ON r.IdRol = ro.IdRol
WHERE o.Url = 'Reporte/Index' AND o.IdSistema = @IdSistemaSasi;
