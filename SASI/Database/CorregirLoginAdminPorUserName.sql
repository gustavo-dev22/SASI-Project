-- Login estricto por UserName.
-- El usuario admin fue creado originalmente con su correo como UserName ('admin@correo.com').
-- Se renombra a 'admin' para que el ingreso al sistema sea únicamente por nombre de usuario
-- (mismo criterio aplicado manualmente en las otras PC y descrito en el commit 013af5b).
-- Es idempotente: solo actualiza si todavía existe el valor antiguo.
BEGIN TRANSACTION;
GO

SET QUOTED_IDENTIFIER ON;
GO

UPDATE [AspNetUsers]
SET [UserName] = N'admin',
    [NormalizedUserName] = N'ADMIN'
WHERE [UserName] = N'admin@correo.com';
GO

COMMIT;
GO
