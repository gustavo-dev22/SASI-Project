# Plan de implementación — Permisos por acción + Login único (SSO) en SASI

> Documento de diseño técnico. Estado: **propuesta** (no implementado).
> Fecha: 2026-09-27.

## 1. Contexto y decisiones

SASI es un sistema transversal: gestiona usuarios, roles, menús/submenús/ítems por sistema,
bloqueo por intentos fallidos, oficinas, gobernanza TI y operación/soporte. La autenticación de
su consola web es por **cookie** (`.SASI.Auth`, `Path=/SASI`); los sistemas externos se integran
por **API JWT** (`/api/auth/...`).

Estado actual relevante:

- La autorización es **solo a nivel de módulo**: `AccesoModuloHandler`
  (`SASI/Authorization/AccesoModulo.cs:34`) valida que el rol tenga un `Objeto` activo cuya
  `Url` coincida con el controlador (`obj.Url LIKE controller%`).
- `Objeto` solo tiene `Tipo` = `Menu`/`Submenu`/`Item` (`SASI.Dominio/Modelo/TipoObjeto.cs`).
- `RolObjeto` solo tiene un booleano `Activo` (`SASI.Dominio/Modelo/RolObjeto.cs:10`).
- No existe tabla de acciones ni motor de permisos.
- `CuentaServicio` está **fijo al sistema 18** (`_sistemaId`, `SASI/Servicios/CuentaServicio.cs:44,65,170`).
- No existe SSO/OAuth/OIDC.

### Decisiones tomadas

| Tema | Decisión |
|------|----------|
| Catálogo de acciones | **Global fijo** (tabla `Accion`, códigos estándar compartidos). |
| Enforcement | **SASI administra y entrega** (API/claims); **cada sistema externo valida** en su backend y pinta sus botones. |
| Login único | **SSO por *authorization code*** (dominios distintos, clientes .NET y SPA → PKCE). |

### Acciones base (seed)

`LISTAR`, `CREAR`, `EDITAR`, `ELIMINAR`, `BLOQUEAR`, `DESBLOQUEAR`, `EXPORTAR`, `APROBAR`.

Regla base: al asignar un módulo a un rol, `LISTAR` se concede por defecto.

### Convenciones del proyecto

- Capas: `SASI` (web), `SASI.Aplicacion` (servicios), `SASI.Dominio` (modelo/DTO/interfaces), `SASI.Infraestructura` (EF Core/Identity/repos).
- Dos contextos EF: `IdentityDbContext` (`SASI/Migrations`) y `SasiDbContext` (`SASI/MigrationsSasi`).
- Entidades de negocio heredan `AuditoriaBase` (auditoría automática en `SasiDbContext.SaveChangesAsync`).
- Migraciones se aplican con `MigracionesConReconciliacion.AplicarAsync(...)` en arranque (solo Development).

### Orden sugerido de trabajo

1. **Feature A** (Fases A1–A5): es la base de seguridad y no depende del SSO.
2. **Feature B** (Fases B1–B4): el login reutiliza los contratos de entrega de A.
3. Cierre: A6/A7 y B5/B6.

---

# FEATURE A — Permisos por acción (CREAR, EDITAR, ELIMINAR, LISTAR, BLOQUEAR…)

## Objetivo

Poder conceder a un rol, sobre un módulo/objeto concreto, un conjunto de acciones. SASI es la
fuente de verdad y **entrega** esos permisos a los sistemas externos. En las páginas propias de
SASI, opcionalmente, se refuerza con un filtro.

## Modelo conceptual

```
Sistema 1───N Objeto (árbol: Menu > Submenu > Item)
Rol     1───N RolObjeto     (acceso al módulo)         [ya existe]
RolObjeto  1───N RolObjetoAccion (acción sobre módulo) [nuevo]
Accion  (catálogo global)                              [nuevo]
```

---

## Fase A1 — Dominio

### A1.1 `SASI.Dominio/Modelo/Accion.cs` (nuevo)

```csharp
using SASI.Dominio.Modelo.Commons;

namespace SASI.Dominio.Modelo
{
    // Catálogo global y fijo de acciones.
    public class Accion : AuditoriaBase
    {
        public int IdAccion { get; set; }
        public string Codigo { get; set; } = string.Empty; // LISTAR, CREAR, EDITAR...
        public string Nombre { get; set; } = string.Empty; // "Listar", "Crear"...
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }
}
```

### A1.2 `SASI.Dominio/Modelo/RolObjetoAccion.cs` (nuevo)

```csharp
using SASI.Dominio.Modelo.Commons;

namespace SASI.Dominio.Modelo
{
    // Concede una Accion sobre un Objeto (módulo) a un Rol.
    public class RolObjetoAccion : AuditoriaBase
    {
        public int IdRolObjetoAccion { get; set; }
        public int IdRol { get; set; }
        public int IdObjeto { get; set; }
        public int IdAccion { get; set; }
        public bool Activo { get; set; } = true;

        public Rol Rol { get; set; } = default!;
        public Objeto Objeto { get; set; } = default!;
        public Accion Accion { get; set; } = default!;
    }
}
```

### A1.3 Constantes de códigos (evitar strings sueltos)

`SASI.Dominio/Modelo/AccionesSistema.cs` (nuevo):

```csharp
namespace SASI.Dominio.Modelo
{
    public static class AccionesSistema
    {
        public const string Listar      = "LISTAR";
        public const string Crear       = "CREAR";
        public const string Editar      = "EDITAR";
        public const string Eliminar    = "ELIMINAR";
        public const string Bloquear    = "BLOQUEAR";
        public const string Desbloquear = "DESBLOQUEAR";
        public const string Exportar    = "EXPORTAR";
        public const string Aprobar     = "APROBAR";
    }
}
```

### A1.4 Extender `SASI.Dominio/DTO/ObjetoDto.cs`

Agregar la lista de acciones para la entrega a externos:

```csharp
public List<string> Acciones { get; set; } = new(); // ["LISTAR","CREAR",...]
```

---

## Fase A2 — Infraestructura

### A2.1 `SasiDbContext` — DbSets

En `SASI.Infraestructura/Datos/SasiDbContext.cs`:

```csharp
public DbSet<Accion> Acciones { get; set; }
public DbSet<RolObjetoAccion> RolObjetoAcciones { get; set; }
```

### A2.2 `OnModelCreating` — configuración

```csharp
modelBuilder.Entity<Accion>().ToTable("Accion");
modelBuilder.Entity<RolObjetoAccion>().ToTable("RolObjetoAccion");

modelBuilder.Entity<Accion>().HasKey(a => a.IdAccion);
modelBuilder.Entity<Accion>().HasIndex(a => a.Codigo).IsUnique();

modelBuilder.Entity<RolObjetoAccion>().HasKey(x => x.IdRolObjetoAccion);
modelBuilder.Entity<RolObjetoAccion>().HasIndex(x => x.IdRol);
modelBuilder.Entity<RolObjetoAccion>().HasIndex(x => x.IdObjeto);
modelBuilder.Entity<RolObjetoAccion>().HasIndex(x => x.IdAccion);
modelBuilder.Entity<RolObjetoAccion>()
    .HasIndex(x => new { x.IdRol, x.IdObjeto, x.IdAccion }).IsUnique();

modelBuilder.Entity<RolObjetoAccion>()
    .HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.IdRol);
modelBuilder.Entity<RolObjetoAccion>()
    .HasOne(x => x.Objeto).WithMany().HasForeignKey(x => x.IdObjeto);
modelBuilder.Entity<RolObjetoAccion>()
    .HasOne(x => x.Accion).WithMany().HasForeignKey(x => x.IdAccion);
```

### A2.3 DDL de referencia (SQL Server)

```sql
CREATE TABLE [Accion] (
    [IdAccion]     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Codigo]       NVARCHAR(50)  NOT NULL,
    [Nombre]       NVARCHAR(100) NOT NULL,
    [Descripcion]  NVARCHAR(250) NULL,
    [Orden]        INT NOT NULL DEFAULT 0,
    [Activo]       BIT NOT NULL DEFAULT 1,
    [AuditFechaCreacion]       DATETIME2 NULL,
    [AuditUsuarioCreacion]     NVARCHAR(MAX) NULL,
    [IpCreacion]               NVARCHAR(MAX) NULL,
    [AuditFechaModificacion]   DATETIME2 NULL,
    [AuditUsuarioModificacion] NVARCHAR(MAX) NULL,
    [IpModificacion]           NVARCHAR(MAX) NULL
);
CREATE UNIQUE INDEX [IX_Accion_Codigo] ON [Accion]([Codigo]);

CREATE TABLE [RolObjetoAccion] (
    [IdRolObjetoAccion] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [IdRol]    INT NOT NULL,
    [IdObjeto] INT NOT NULL,
    [IdAccion] INT NOT NULL,
    [Activo]   BIT NOT NULL DEFAULT 1,
    [AuditFechaCreacion]       DATETIME2 NULL,
    [AuditUsuarioCreacion]     NVARCHAR(MAX) NULL,
    [IpCreacion]               NVARCHAR(MAX) NULL,
    [AuditFechaModificacion]   DATETIME2 NULL,
    [AuditUsuarioModificacion] NVARCHAR(MAX) NULL,
    [IpModificacion]           NVARCHAR(MAX) NULL,
    CONSTRAINT [FK_RolObjetoAccion_Rol]    FOREIGN KEY ([IdRol])    REFERENCES [Roles]([IdRol]),
    CONSTRAINT [FK_RolObjetoAccion_Objeto] FOREIGN KEY ([IdObjeto]) REFERENCES [Objeto]([IdObjeto]),
    CONSTRAINT [FK_RolObjetoAccion_Accion] FOREIGN KEY ([IdAccion]) REFERENCES [Accion]([IdAccion])
);
CREATE UNIQUE INDEX [IX_RolObjetoAccion_Rol_Objeto_Accion]
    ON [RolObjetoAccion]([IdRol],[IdObjeto],[IdAccion]);
```

### A2.4 Migración + seed

Comando (ejecutar desde la carpeta del proyecto `SASI`):

```powershell
dotnet ef migrations add AgregarPermisosPorAccion --context SasiDbContext --output-dir MigrationsSasi
dotnet ef database update --context SasiDbContext
```

Seed de acciones (en la migración o en un inicializador de arranque):

```csharp
var acciones = new[] { "LISTAR", "CREAR", "EDITAR", "ELIMINAR", "BLOQUEAR", "DESBLOQUEAR", "EXPORTAR", "APROBAR" };
```

### A2.5 Repositorio

`SASI.Dominio/Repositories/IRolObjetoAccionRepository.cs`:

```csharp
public interface IRolObjetoAccionRepository
{
    Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolAsync(int idRol);
    Task<List<(int IdObjeto, int IdAccion)>> ObtenerAsignacionesPorRolAsync(int idRol);
    Task ActualizarAsignacionesAsync(int idRol, List<(int IdObjeto, int IdAccion)> asignaciones);
}
```

`SASI.Infraestructura/Repositories/RolObjetoAccionRepository.cs`: implementación con
desactivación de existentes y upsert (mismo patrón que `RolObjetoRepository.ActualizarAsignacionesAsync`).

### A2.6 Registro en `Program.cs`

```csharp
builder.Services.AddTransient<IRolObjetoAccionRepository, RolObjetoAccionRepository>();
```

---

## Fase A3 — Aplicación

### A3.1 `IPermisoServicio` (nuevo)

`SASI.Aplicacion/Servicios/PermisoServicio.cs`:

```csharp
public interface IPermisoServicio
{
    Task<bool> TienePermisoAsync(int sistemaId, int rolId, int idObjeto, string codigoAccion);
    Task<bool> TienePermisoPorUrlAsync(int sistemaId, int rolId, string urlController, string codigoAccion);
    Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolAsync(int idRol);
}
```

Reglas de implementación:
- `SistemaId` permite validar que el objeto pertenece al sistema consultado.
- `TienePermisoPorUrlAsync` resuelve el `Objeto` por `Url LIKE controller%` (mismo criterio de `AccesoModuloHandler`).
- Administradores de SASI (`Administrador`, `Administrador de Seguridad`) → siempre `true`.

### A3.2 Extender `IRolServicio`

```csharp
Task<Dictionary<int, List<string>>> ObtenerAccionesPorRolAsync(int idRol);
Task GuardarAsignacionAccionesAsync(int idRol, List<(int IdObjeto, int IdAccion)> asignaciones);
```

---

## Fase A4 — Administración en SASI

### A4.1 ViewModel

`SASI/Models/PermisoRolViewModel.cs` (nuevo):

```csharp
public class PermisoRolViewModel
{
    public int IdRol { get; set; }
    public string NombreRol { get; set; } = string.Empty;
    public List<Objeto> Objetos { get; set; } = new();
    public List<Accion> Acciones { get; set; } = new();
    public Dictionary<int, List<int>> Permisos { get; set; } = new(); // idObjeto -> idAccion[]
}
```

### A4.2 `RolController`

- `GET AsignarAcciones(int idRol)` → vista.
- `POST GuardarAsignacionAcciones(PermisoRolViewModel model)` → `RolServicio.GuardarAsignacionAccionesAsync`.

### A4.3 Vista `SASI/Views/Rol/AsignarAcciones.cshtml`

- Reutilizar el árbol jsTree de `AsignarObjetos.cshtml`.
- Por cada nodo, un grupo de checkboxes (una por acción) con nombres
  `Permisos[{idObjeto}][{idAccion}]`.
- Al marcar un objeto, precargar `LISTAR`.
- Guardado vía `fetch` + `FormData`, confirmación con SweetAlert (patrón ya usado).

---

## Fase A5 — Entrega a sistemas externos (núcleo del requerimiento)

### A5.1 Extender `AutenticacionServicio`

En `LoginAsync` y `ObtenerAccesosAsync`, por cada objeto del rol, adjuntar `acciones`:

```csharp
// construir mapa rol -> (idObjeto -> acciones)
var permisos = await _permisoServicio.ObtenerPermisosPorRolAsync(rol.IdRol);

// en la proyección del objeto:
acciones = permisos.TryGetValue(o.IdObjeto, out var ac) ? ac : new List<string>()
```

### A5.2 Endpoint de refresco

`SASI/Controllers/API/PermisosApiController.cs` (nuevo):

```
GET /api/permisos/{sistemaId}/{usuario}
```

Respuesta (ver §Contratos). Requiere JWT (`[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`).

### A5.3 Claims opcionales en JWT

En `AutenticacionServicio.ConstruirClaimsAsync`, opcionalmente:

```csharp
claims.Add(new Claim("permiso", $"{sistemaId}:{idObjeto}:{codigo}"));
```

> Si el token crece demasiado (muchos sistemas), dejar solo el endpoint y no los claims.

### A5.4 Documentar contrato para el sistema externo

El externo debe:
1. Al iniciar sesión, guardar el mapa de permisos.
2. Ocultar/mostrar botones según `acciones`.
3. **Validar en backend** cada endpoint (no confiar solo en el front) → responder `403` si no tiene la acción.

---

## Fase A6 — Enforcement dentro de SASI (recomendado, bajo costo)

### A6.1 Atributo + handler

`SASI/Authorization/PermisoAccion.cs` (nuevo), análogo a `AccesoModulo.cs`:

```csharp
public class PermisoAccionRequirement : IAuthorizationRequirement
{
    public string Modulo { get; }
    public string Accion { get; }
    public PermisoAccionRequirement(string modulo, string accion) { Modulo = modulo; Accion = accion; }
}

public class PermisoAccionAttribute : AuthorizeAttribute
{
    public PermisoAccionAttribute(string modulo, string accion)
        => Policy = $"PermisoAccion:{modulo}:{accion}";
}
```

Registro de política dinámica en `Program.cs` (`options.AddPolicy` o `IAuthorizationPolicyProvider`).

### A6.2 Uso en controladores

```csharp
[PermisoAccion("Documentos", AccionesSistema.Crear)]
public async Task<IActionResult> Crear(...) { ... }
```

### A6.3 Helper de botones y caché de sesión

- Helper/tag-helper: `@if (User.TienePermiso("Documentos", "CREAR")) { <button ...> }`.
- Al login, guardar permisos en sesión (igual que `MenuUsuario`) para evitar consultas por request.
- Invalidar la caché al cambiar rol (`SeleccionarRol`) o al vencer/renovar sesión.

---

## Fase A7 — Pruebas

- Rol sin acción → botón oculto y endpoint de backend responde `403`.
- `LISTAR` sin `CREAR`; `CREAR` sin `EDITAR`; `BLOQUEAR`/`DESBLOQUEAR` independientes.
- Cambios de permisos reflejados tras re-login/refresh de token.
- Aislamiento entre sistemas (permisos del sistema A no aplican al B).
- Administradores de SASI mantienen bypass.

---

# FEATURE B — Login único provisto por SASI (SSO por authorization code)

## Objetivo

Una sola interfaz de login alojada en SASI, con título dinámico "Acceso al sistema {Nombre}",
compartida por todos los sistemas (dominios distintos, clientes .NET y SPA). Modelo OAuth2
Authorization Code + PKCE, reutilizando Identity y el JWT/refresh existentes.

## Flujo (resumen)

```
Usuario → SistemaExterno
SistemaExterno → (redirect) SASI /Cuenta/Login?client_id=DOC&returnUrl=...&state=...&code_challenge=...
SASI autentica + valida acceso al sistema destino
SASI → (redirect) returnUrl?code=ONE_TIME_CODE&state=...
SistemaExterno → POST /api/auth/token  (client_id, code, code_verifier / client_secret)
SASI valida y responde { access, refresh, expiration, usuario, sistemas }
SistemaExterno crea su sesión y descarta el code
```

---

## Fase B1 — Registro de clientes

### B1.1 Entidad `SASI.Dominio/Modelo/SistemaCliente.cs` (nuevo)

```csharp
public class SistemaCliente : AuditoriaBase
{
    public int IdSistemaCliente { get; set; }
    public int IdSistema { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecretHash { get; set; } // null => cliente público (SPA)
    public string RedirectUris { get; set; } = string.Empty; // separadas por ';'
    public bool RequierePkce { get; set; } = true;
    public bool Activo { get; set; } = true;
    public Sistema Sistema { get; set; } = default!;
}
```

### B1.2 DDL de referencia

```sql
CREATE TABLE [SistemaCliente] (
    [IdSistemaCliente] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [IdSistema]        INT NOT NULL,
    [ClientId]         NVARCHAR(100) NOT NULL,
    [ClientSecretHash] NVARCHAR(MAX) NULL,
    [RedirectUris]     NVARCHAR(MAX) NOT NULL,
    [RequierePkce]     BIT NOT NULL DEFAULT 1,
    [Activo]           BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_SistemaCliente_Sistema] FOREIGN KEY ([IdSistema]) REFERENCES [Sistemas]([IdSistema])
);
CREATE UNIQUE INDEX [IX_SistemaCliente_ClientId] ON [SistemaCliente]([ClientId]);
```

### B1.3 Reglas

- `ClientId` único y opaco (ej. `sasi`, `documental`, `citas`).
- `RedirectUris`: **validación exacta** (comparar contra la lista) para evitar open redirect.
- Seed: cliente `sasi` (consola) y uno por cada sistema externo.

---

## Fase B2 — Parametrizar el login actual

### B2.1 Refactor `CuentaServicio`

Hoy `_sistemaId` está fijo en config (`CuentaServicio.cs:44,65,170`). Cambiar a parámetro:

```csharp
public async Task<CuentaLoginResult> LoginAsync(string userName, string password, int sistemaId)
public async Task<List<MenuItemViewModel>> SeleccionarRolAsync(Guid userId, int rolId, int sistemaId)
private async Task<List<MenuItemViewModel>?> ConstruirMenuAsync(Guid userId, int? rolId, int sistemaId)
```

- Validar rol activo **en `sistemaId`** (`UsuarioTieneRolActivoEnSistemaAsync(user.Id, sistemaId)`).
- Menú y rol predeterminado calculados para `sistemaId`.
- La consola de SASI usa `sistemaId = 18` (config).

### B2.2 `GET /Cuenta/Login` con parámetros SSO

```
GET /Cuenta/Login?client_id={clientId}&returnUrl={uri}&state={state}&code_challenge={...}&code_challenge_method=S256
```

- Resolver `SistemaCliente` por `client_id` → `IdSistema` → `Sistema.Nombre`.
- `ViewBag.NombreSistema` para el título "Acceso al sistema {Nombre}".
- Validar `returnUrl` contra `RedirectUris` del cliente (no usar `Url.IsLocalUrl` solo).

### B2.3 Vista

Reutilizar `SASI/Views/Cuenta/Login.cshtml` cambiando el encabezado a dinámico:

```razor
<h4 class="text-center mb-4">Acceso al sistema @ViewBag.NombreSistema</h4>
```

---

## Fase B3 — Flujo SSO

### B3.1 Entidad `AuthCode` (nuevo)

`SASI.Dominio/Modelo/AuthCode.cs`:

```csharp
public class AuthCode
{
    public int IdAuthCode { get; set; }
    public string CodeHash { get; set; } = string.Empty; // SHA-256 del code
    public string ClientId { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public int SistemaId { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; } // S256
    public DateTime ExpiraUtc { get; set; }
    public DateTime? UsadoUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}
```

Índices: unique en `CodeHash`; índice en `UsuarioId`. (Sin `AuditoriaBase`; es efímero y técnico.)

> Almacenar solo el **hash** del code (mismo criterio que `RefreshToken`).

### B3.2 Emisión del code

En el POST de login, si la petición es SSO (trae `client_id` y `returnUrl` válidos):

1. Generar token aleatorio (`RandomNumberGenerator`, 64 bytes, base64url).
2. Persistir `AuthCode` con hash, expiración ~60 s y datos del cliente/usuario/sistema.
3. Redirigir `returnUrl?code={code}&state={state}` (**nunca el token en URL**).

### B3.3 Canje

`POST /api/auth/token` (en `AuthApiController` o nuevo `SsoApiController`):

```jsonc
// request (cliente confidencial)
{ "grant_type": "authorization_code", "client_id": "documental",
  "client_secret": "...", "code": "...", "code_verifier": null }

// request (cliente público / SPA, PKCE)
{ "grant_type": "authorization_code", "client_id": "documental",
  "code": "...", "code_verifier": "..." }
```

Validaciones:
1. `client_id` existe y está activo.
2. Si el cliente es confidencial → validar `client_secret` (hash).
3. `code` existe (por hash), no usado, no expirado.
4. Si `RequierePkce` → `SHA256(code_verifier) == code_challenge`.
5. Marcar `UsadoUtc = GETUTCDATE()` (revocación atómica, un solo uso).
6. Emitir `access` + `refresh` (reutilizar `AutenticacionServicio`).

### B3.4 Secuencia (detalle)

```
GET  /Cuenta/Login?client_id=...&returnUrl=...&state=...&code_challenge=...
POST /Cuenta/Login  (form: userName, password, client_id, returnUrl, state, code_challenge)
     -> valida credenciales (Identity + bloqueo + vencimiento password)
     -> valida rol activo en el sistema destino
     -> crea AuthCode (hash, exp 60s)
     -> 200 { success:true, redirectUrl:"returnUrl?code=...&state=..." }
GET  returnUrl?code=...&state=...
SistemaExterno backend -> POST /api/auth/token
     -> 200 { success:true, token, refreshToken, expiration, usuario, sistemas }
```

---

## Fase B4 — Cliente .NET vs SPA

| Tipo | Registro | Secreto | PKCE |
|------|----------|---------|------|
| .NET server-side (confidencial) | `ClientSecretHash` definido | Sí | Opcional |
| SPA / Angular (público) | `ClientSecretHash = null` | No | **Obligatorio (S256)** |

El front SPA **no** debe canjear el code con secreto; usa `code_verifier`. Si el canje se hace
desde backend del mismo sistema, puede usar secreto.

---

## Fase B5 — Seguridad / infraestructura

- La cookie SASI sigue en `Path=/SASI` para su consola; los externos usan JWT.
- `AuthCode`: un solo uso, corto, ligado a `client_id` + `redirect_uri` + PKCE.
- Nunca mover tokens por querystring.
- `state` obligatorio (anti-CSRF del flujo).
- Revisar `SameSite`/HTTPS y lista CORS (`appsettings.json:13`).
- Si crecen clientes/requisitos formales → evaluar OIDC (OpenIddict/Duende IdentityServer).
- Registrar auditoría de emisiones/canjes (Serilog ya configurado; enmascarar PII).

---

## Fase B6 — Pruebas

- Code de un solo uso (segundo canje falla).
- Code expirado rechazado.
- `code_verifier` inválido en cliente PKCE → rechazado.
- `redirect_uri` no registrado → rechazado (no redirigir).
- Usuario sin rol activo en el sistema destino → rechazado.
- `state` ausente/incorrecto → rechazado.
- Cliente con secreto incorrecto → rechazado.

---

# Contratos JSON

## Entrega de permisos (extiende respuesta de login/accesos)

```jsonc
{
  "success": true,
  "token": "...",
  "refreshToken": "...",
  "expiration": "2026-09-27T18:00:00Z",
  "usuario": {
    "id": "guid",
    "nombreCompleto": "Juan Pérez",
    "userName": "jperez",
    "email": "jperez@dom.gob.pe",
    "activo": true,
    "oficina": { "id": 3, "nombre": "Informática", "sigla": "OTIC" },
    "sistemas": [
      {
        "id": 14,
        "nombre": "Gestión Documental",
        "activo": true,
        "roles": [
          {
            "idRol": 13,
            "nombreRol": "Operador",
            "activo": true,
            "esPrincipal": true,
            "objetos": [
              {
                "idObjeto": 101,
                "nombre": "Documentos",
                "tipo": "Item",
                "url": "Documentos",
                "titulo": "Gestión de Documentos",
                "icono": "bi bi-file-earmark",
                "activo": true,
                "orden": 1,
                "idPadre": 100,
                "acciones": ["LISTAR", "CREAR", "EDITAR", "EXPORTAR"]
              }
            ]
          }
        ]
      }
    ]
  }
}
```

## Endpoint de refresco de permisos

```
GET /api/permisos/{sistemaId}/{usuario}
```

```jsonc
{
  "exito": true,
  "datos": {
    "sistemaId": 14,
    "objetos": [
      { "idObjeto": 101, "url": "Documentos", "acciones": ["LISTAR","CREAR","EDITAR","EXPORTAR"] }
    ]
  }
}
```

## Canje de authorization code

```
POST /api/auth/token
```

```jsonc
// request
{ "grant_type": "authorization_code", "client_id": "documental",
  "client_secret": null, "code": "abc123", "code_verifier": "verifier" }

// response
{ "success": true, "token": "...", "refreshToken": "...", "expiration": "..." }
```

---

# Migraciones (resumen de comandos)

Ejecutar desde la carpeta del proyecto `SASI`:

```powershell
# Feature A
dotnet ef migrations add AgregarPermisosPorAccion --context SasiDbContext --output-dir MigrationsSasi
dotnet ef database update --context SasiDbContext

# Feature B
dotnet ef migrations add AgregarSsoClientes --context SasiDbContext --output-dir MigrationsSasi
dotnet ef database update --context SasiDbContext
```

---

# Archivos a crear/modificar (inventario)

### Feature A
- `SASI.Dominio/Modelo/Accion.cs` *(nuevo)*
- `SASI.Dominio/Modelo/RolObjetoAccion.cs` *(nuevo)*
- `SASI.Dominio/Modelo/AccionesSistema.cs` *(nuevo)*
- `SASI.Dominio/DTO/ObjetoDto.cs` *(modificar: `Acciones`)*
- `SASI.Dominio/Repositories/IRolObjetoAccionRepository.cs` *(nuevo)*
- `SASI.Infraestructura/Repositories/RolObjetoAccionRepository.cs` *(nuevo)*
- `SASI.Infraestructura/Datos/SasiDbContext.cs` *(modificar)*
- `SASI.Aplicacion/Servicios/PermisoServicio.cs` *(nuevo)*
- `SASI.Aplicacion/Servicios/RolServicio.cs` *(modificar)*
- `SASI/Controllers/RolController.cs` *(modificar)*
- `SASI/Controllers/API/PermisosApiController.cs` *(nuevo)*
- `SASI/Models/PermisoRolViewModel.cs` *(nuevo)*
- `SASI/Views/Rol/AsignarAcciones.cshtml` *(nuevo)*
- `SASI/Authorization/PermisoAccion.cs` *(nuevo, opcional A6)*
- `SASI/Servicios/AutenticacionServicio.cs` *(modificar: incluir acciones)*
- `SASI/Program.cs` *(registros DI/políticas)*

### Feature B
- `SASI.Dominio/Modelo/SistemaCliente.cs` *(nuevo)*
- `SASI.Dominio/Modelo/AuthCode.cs` *(nuevo)*
- `SASI.Infraestructura/Datos/SasiDbContext.cs` *(modificar)*
- `SASI/Servicios/CuentaServicio.cs` *(modificar: parametrizar sistemaId)*
- `SASI/Controllers/CuentaController.cs` *(modificar: flujo SSO)*
- `SASI/Controllers/API/AuthApiController.cs` *(modificar: `POST token`)*
- `SASI/Views/Cuenta/Login.cshtml` *(modificar: título dinámico + campos SSO)*
- `SASI/Program.cs` *(registros)*

---

# Riesgos y consideraciones

- **A**: el cumplimiento real depende del sistema externo; sin validación en su backend el permiso es solo visual.
- **A**: cachear permisos en sesión exige invalidación al cambiar rol; mantener TTL corto o revalidar en endpoints sensibles.
- **B**: el code exchange debe ser de un solo uso, corto y ligado a `redirect_uri`; nunca mover tokens por querystring.
- **B**: `state` y PKCE son obligatorios para evitar CSRF y robo de code.
- **Ambos**: respetar el mecanismo de migraciones separadas (`Migrations` vs `MigrationsSasi`) y `MigracionesConReconciliacion`.
- **Compatibilidad**: extender `ObjetoDto`/respuestas de API es aditivo; los clientes actuales que ignoren `acciones` siguen funcionando.
