-- =============================================================
-- SASI: Otorgar al rol Operador el acceso al modulo Solicitudes de Acceso
-- =============================================================
-- El menu y permisos de SASI se construyen desde Objeto + RolObjeto (IdSistema = 18).
-- El rol Operador (IdRol 22) ahora puede ver y usar "Solicitudes de Acceso".
-- El script es IDEMPOTENTE.
-- =============================================================

DECLARE @IdSistemaSasi INT = (SELECT IdSistema FROM dbo.Sistemas WHERE IdSistema = 18);
IF @IdSistemaSasi IS NULL
BEGIN
    SET @IdSistemaSasi = (SELECT TOP 1 IdSistema FROM dbo.Sistemas WHERE Nombre LIKE 'Sistema de Administracion de Sistemas de Informacion%');
END

DECLARE @IdRolOperador INT = (SELECT TOP 1 IdRol FROM dbo.Roles WHERE IdSistema = @IdSistemaSasi AND Nombre = 'Operador' AND Activo = 1);
DECLARE @IdObjetoSolicitudes INT = (SELECT IdObjeto FROM dbo.Objeto WHERE Url = 'Soporte/Solicitudes' AND IdSistema = @IdSistemaSasi);

IF @IdRolOperador IS NOT NULL AND @IdObjetoSolicitudes IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RolObjeto WHERE IdRol = @IdRolOperador AND IdObjeto = @IdObjetoSolicitudes)
BEGIN
    INSERT INTO dbo.RolObjeto (
        IdRol, IdObjeto, Activo,
        AuditUsuarioCreacion, AuditFechaCreacion, IpCreacion
    )
    VALUES (
        @IdRolOperador, @IdObjetoSolicitudes, 1,
        'system', GETDATE(), 'script'
    );
END

-- Verificacion
SELECT ro.IdRol, r.Nombre AS Rol, o.Nombre AS Objeto, ro.Activo
FROM dbo.RolObjeto ro
INNER JOIN dbo.Roles r ON ro.IdRol = r.IdRol
INNER JOIN dbo.Objeto o ON ro.IdObjeto = o.IdObjeto
WHERE r.IdSistema = @IdSistemaSasi AND o.Url = 'Soporte/Solicitudes';
