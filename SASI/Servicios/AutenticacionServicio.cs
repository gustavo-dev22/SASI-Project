using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SASI.Aplicacion.Servicios;
using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Helpers;
using SASI.Infraestructura.Identity;
using SASI.Models.Requests;
using SistemaConvocatorias.Infraestructura.Datos;

namespace SASI.Servicios
{
    public class AutenticacionServicio
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly IOficinaServicio _oficinaServicio;
        private readonly IObjetoServicio _objetoServicio;
        private readonly SasiDbContext _sasiDbContext;

        public AutenticacionServicio(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            IUsuarioSistemaServicio usuarioSistemaServicio,
            IOficinaServicio oficinaServicio,
            IObjetoServicio objetoServicio,
            SasiDbContext sasiDbContext)
        {
            _userManager = userManager;
            _config = config;
            _usuarioSistemaServicio = usuarioSistemaServicio;
            _oficinaServicio = oficinaServicio;
            _objetoServicio = objetoServicio;
            _sasiDbContext = sasiDbContext;
        }

        public async Task<object?> LoginAsync(string userName, string password)
        {
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
                return null;

            // 🛡️ El estado INACTIVO del usuario PRIMA sobre cualquier otro caso:
            // un usuario desactivado en SASI no puede iniciar sesión en ningún
            // sistema integrado, sin importar credenciales o bloqueos.
            if (!user.Activo)
            {
                return new
                {
                    success = false,
                    codigo = "USUARIO_INACTIVO",
                    message = "Su usuario se encuentra inactivo en el sistema. Contacte al administrador para restablecer el acceso.",
                    bloqueado = false,
                    inactivo = true
                };
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return new
                {
                    success = false,
                    codigo = "USUARIO_BLOQUEADO",
                    message = "Su cuenta se encuentra bloqueada temporalmente por intentos fallidos de inicio de sesión. Contacte al administrador del sistema.",
                    bloqueado = true,
                    inactivo = false
                };
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.AccessFailedAsync(user);

                // Si este intento activó el bloqueo por política (máx. intentos fallidos),
                // se informa de inmediato al usuario en lugar de un error genérico.
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return new
                    {
                        success = false,
                        codigo = "USUARIO_BLOQUEADO",
                        message = "Su cuenta se encuentra bloqueada temporalmente por intentos fallidos de inicio de sesión. Contacte al administrador del sistema.",
                        bloqueado = true,
                        inactivo = false
                    };
                }

                return null;
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var sistemasYRoles = await _usuarioSistemaServicio.ObtenerSistemasYRolesDelUsuarioAsync(user.Id);

            var claims = await ConstruirClaimsAsync(user, sistemasYRoles);

            var token = GenerarAccessToken(claims, out var expires);
            var refreshToken = await GenerarYGuardarRefreshTokenAsync(user.Id);

            Oficina? oficina = null;
            if (user.IdOficina.HasValue)
            {
                oficina = await _oficinaServicio.ObtenerPorIdAsync(user.IdOficina.Value);
            }

            var idsPadreGlobales = sistemasYRoles
                .SelectMany(sr => sr.Objetos)
                .Where(o => o.Tipo == "Submenu" && o.IdPadre != null)
                .Select(o => o.IdPadre!.Value)
                .Distinct()
                .ToList();

            var menusPadreGlobales = (await _objetoServicio.ObtenerPorIdsAsync(idsPadreGlobales))
                .Select(o => new ObjetoDto
                {
                    IdObjeto = o.IdObjeto,
                    Nombre = o.Nombre,
                    Tipo = o.Tipo,
                    Url = o.Url ?? "",
                    Titulo = o.Titulo ?? "",
                    Icono = o.Icono ?? string.Empty,
                    Activo = o.Activo,
                    Orden = o.Orden,
                    IdPadre = o.IdPadre
                })
                .ToList();

            var sistemasEstructurados = ConstruirSistemasEstructurados(sistemasYRoles, menusPadreGlobales);

            return new
            {
                success = true,
                bloqueado = false,
                intentosFallidos = 0,
                token,
                refreshToken,
                expiration = expires,
                usuario = new
                {
                    id = user.Id,
                    nombreCompleto = user.NombreCompleto,
                    userName = user.UserName,
                    email = user.Email,
                    activo = user.Activo,
                    oficina = oficina == null ? null : new
                    {
                        id = oficina.IdOficina,
                        nombre = oficina.Nombre,
                        sigla = oficina.Sigla
                    },
                    sistemas = sistemasEstructurados
                }
            };
        }

        public async Task<object?> ObtenerAccesosAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return null;

            var sistemasYRoles = await _usuarioSistemaServicio.ObtenerSistemasYRolesDelUsuarioAsync(user.Id);

            Oficina? oficina = null;
            if (user.IdOficina.HasValue)
            {
                oficina = await _oficinaServicio.ObtenerPorIdAsync(user.IdOficina.Value);
            }

            var sistemasEstructurados = sistemasYRoles
                .GroupBy(x => new { x.SistemaId, x.SistemaNombre, x.SistemaActivo })
                .Select(g => new
                {
                    id = g.Key.SistemaId,
                    nombre = g.Key.SistemaNombre,
                    activo = g.Key.SistemaActivo,
                    roles = g.Select(r => new
                    {
                        idRol = r.RolId,
                        nombreRol = r.RolNombre,
                        activo = r.UsuarioSistemaRolActivo,
                        esPrincipal = r.EsPrincipal,
                        objetos = r.Objetos.Select(o => new
                        {
                            idObjeto = o.IdObjeto,
                            nombre = o.Nombre,
                            tipo = o.Tipo,
                            url = o.Url,
                            titulo = o.Titulo,
                            icono = o.Icono,
                            activo = o.Activo,
                            orden = o.Orden,
                            idPadre = o.IdPadre
                        }).ToList()
                    }).ToList()
                }).ToList();

            return new
            {
                usuario = new
                {
                    id = user.Id,
                    nombreCompleto = user.NombreCompleto,
                    userName = user.UserName,
                    email = user.Email,
                    activo = user.Activo,
                    oficina = oficina == null ? null : new
                    {
                        id = oficina.IdOficina,
                        nombre = oficina.Nombre
                    },
                    sistemas = sistemasEstructurados
                }
            };
        }

        public const string CodigoEmailExistente = "EMAIL_EXISTE";
        public const string CodigoAsignacionFallida = "ASIGNACION_FALLIDA";

        public async Task<(bool Exito, object? Resultado, string? Error, string? Codigo)> CrearAlumnoAsync(NuevoUsuarioApiRequest dto)
        {
            var existe = await _userManager.FindByEmailAsync(dto.Email);
            if (existe != null)
                return (false, null, "El correo ya existe", CodigoEmailExistente);

            var usuario = new ApplicationUser
            {
                UserName = dto.Dni,
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto,
                Activo = true,
                AuditUsuarioCreacion = "SGA_SYSTEM",
                AuditFechaCreacion = DateTime.Now,
                DebeCambiarPassword = true,
                LockoutEnabled = true,
                IdOficina = 9999
            };

            var contrasenaTemporal = PasswordGenerator.GenerarContrasenaTemporal();
            var resultado = await _userManager.CreateAsync(usuario, contrasenaTemporal);

            if (!resultado.Succeeded)
                return (false, null, string.Join("; ", resultado.Errors.Select(e => e.Description)), null);

            try
            {
                var asignacion = new UsuarioSistema
                {
                    UsuarioId = usuario.Id,
                    SistemaId = 14,
                    RolId = 13,
                    FechaAsignacion = DateTime.Now,
                    Activo = true,
                    EsPrincipal = true
                };

                await _usuarioSistemaServicio.AsignarUsuarioASistemaAsync(
                    asignacion.UsuarioId.ToString(), asignacion.SistemaId, asignacion.RolId, asignacion.EsPrincipal);

                return (true, new { success = true, userId = usuario.Id, contrasenaTemporal }, null, null);
            }
            catch (Exception)
            {
                await _userManager.DeleteAsync(usuario);
                return (false, null, "Usuario creado pero falló la asignación al sistema. La operación fue revertida.", CodigoAsignacionFallida);
            }
        }

        public async Task<object?> RefreshAsync(string refreshToken)
        {
            var hash = CalcularHash(refreshToken);
            var stored = await _sasiDbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (stored == null || stored.RevokedUtc != null || stored.ExpiresUtc < DateTime.UtcNow)
                return null;

            var user = await _userManager.FindByIdAsync(stored.UsuarioId.ToString());
            if (user == null || !user.Activo || await _userManager.IsLockedOutAsync(user))
                return null;

            var sistemasYRoles = await _usuarioSistemaServicio.ObtenerSistemasYRolesDelUsuarioAsync(user.Id);
            var claims = await ConstruirClaimsAsync(user, sistemasYRoles);

            var token = GenerarAccessToken(claims, out var expires);
            var nuevoRefreshToken = GenerarTokenAleatorio();
            var nuevoHash = CalcularHash(nuevoRefreshToken);

            // Revocación atómica: solo una rotación por token (impide doble uso concurrente).
            var filasAfectadas = await _sasiDbContext.Database.ExecuteSqlRawAsync(
                "UPDATE RefreshTokens SET RevokedUtc = GETUTCDATE(), ReplacedByTokenHash = @p0 WHERE TokenHash = @p1 AND RevokedUtc IS NULL",
                nuevoHash, hash);

            if (filasAfectadas == 0)
                return null;

            _sasiDbContext.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = user.Id,
                TokenHash = nuevoHash,
                ExpiresUtc = DateTime.UtcNow.AddDays(RefreshTokenDias()),
                CreatedUtc = DateTime.UtcNow
            });

            await _sasiDbContext.SaveChangesAsync();

            await LimpiarTokensExpiradosAsync();

            return new { success = true, token, refreshToken = nuevoRefreshToken, expiration = expires };
        }

        public async Task<bool> RevokeAsync(string refreshToken)
        {
            var hash = CalcularHash(refreshToken);
            var stored = await _sasiDbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (stored == null)
                return false;

            stored.RevokedUtc = DateTime.UtcNow;
            await _sasiDbContext.SaveChangesAsync();
            return true;
        }

        private async Task<List<Claim>> ConstruirClaimsAsync(ApplicationUser user, List<UsuarioSistemaRolDto> sistemasYRoles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("nombreCompleto", user.NombreCompleto)
            };

            foreach (var grupo in sistemasYRoles.GroupBy(x => x.SistemaId))
            {
                var sistemaId = grupo.Key;

                foreach (var rol in grupo)
                {
                    claims.Add(new Claim("sistema_rol", $"{sistemaId}:{rol.RolNombre}"));
                    claims.Add(new Claim(ClaimTypes.Role, rol.RolNombre));
                }
            }

            if (user.IdOficina.HasValue)
            {
                var oficina = await _oficinaServicio.ObtenerPorIdAsync(user.IdOficina.Value);
                if (oficina != null)
                {
                    claims.Add(new Claim("OficinaId", oficina.IdOficina.ToString()));
                    claims.Add(new Claim("OficinaNombre", oficina.Nombre));
                }
            }

            return claims;
        }

        private string GenerarAccessToken(List<Claim> claims, out DateTime expires)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var minutos = double.TryParse(_config["Jwt:AccessTokenMinutes"], out var m) ? m : 30;
            expires = DateTime.UtcNow.AddMinutes(minutos);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GenerarYGuardarRefreshTokenAsync(Guid usuarioId)
        {
            var token = GenerarTokenAleatorio();

            _sasiDbContext.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = usuarioId,
                TokenHash = CalcularHash(token),
                ExpiresUtc = DateTime.UtcNow.AddDays(RefreshTokenDias()),
                CreatedUtc = DateTime.UtcNow
            });

            await _sasiDbContext.SaveChangesAsync();
            return token;
        }

        private static string GenerarTokenAleatorio()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private double RefreshTokenDias()
            => double.TryParse(_config["Jwt:RefreshTokenDays"], out var d) ? d : 30;

        private async Task LimpiarTokensExpiradosAsync()
        {
            await _sasiDbContext.RefreshTokens
                .Where(rt => rt.ExpiresUtc < DateTime.UtcNow)
                .ExecuteDeleteAsync();
        }

        private static string CalcularHash(string valor)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
            return Convert.ToBase64String(bytes);
        }

        private static List<object> ConstruirSistemasEstructurados(List<UsuarioSistemaRolDto> sistemasYRoles, List<ObjetoDto> menusPadreGlobales)
        {
            return sistemasYRoles
                .GroupBy(x => new { x.SistemaId, x.SistemaNombre, x.SistemaActivo })
                .Select(g => new
                {
                    id = g.Key.SistemaId,
                    nombre = g.Key.SistemaNombre,
                    activo = g.Key.SistemaActivo,
                    roles = g.Select(r =>
                    {
                        var objetosRol = r.Objetos
                            .Where(o => o.Activo)
                            .ToList();

                        var submenus = objetosRol
                            .Where(o => o.Tipo == "Submenu" && o.IdPadre != null)
                            .ToList();

                        var idsPadre = submenus
                            .Select(s => s.IdPadre!.Value)
                            .Distinct()
                            .ToList();

                        var menusPadre = menusPadreGlobales
                            .Where(o => idsPadre.Contains(o.IdObjeto))
                            .ToList();

                        var objetosFinales = objetosRol
                            .Concat(menusPadre)
                            .DistinctBy(o => o.IdObjeto)
                            .Select(o => new
                            {
                                idObjeto = o.IdObjeto,
                                nombre = o.Nombre,
                                tipo = o.Tipo,
                                url = o.Url,
                                titulo = o.Titulo,
                                icono = o.Icono,
                                activo = o.Activo,
                                orden = o.Orden,
                                idPadre = o.IdPadre
                            })
                            .ToList();

                        return new
                        {
                            idRol = r.RolId,
                            nombreRol = r.RolNombre,
                            activo = r.UsuarioSistemaRolActivo,
                            esPrincipal = r.EsPrincipal,
                            objetos = objetosFinales
                        };
                    }).ToList()
                }).ToList<object>();
        }
    }
}
