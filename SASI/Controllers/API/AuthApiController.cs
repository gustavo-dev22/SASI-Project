using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Identity;
using SASI.Infraestructura.Repositories;
using SASI.Models.Requests;
using SASI.Models.Response;
using SistemaConvocatorias.Infraestructura.Datos;

namespace SASI.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;
        private readonly SasiDbContext _sasiDbContext;
        private const int MAX_INTENTOS = 3;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config, IUsuarioSistemaRepository usuarioSistemaRepository, SasiDbContext sasiDbContext)
        {
            _userManager = userManager;
            _config = config;
            _usuarioSistemaRepository = usuarioSistemaRepository;
            _sasiDbContext = sasiDbContext;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName); 

            // Usuario no encontrado (puedes ocultar si prefieres no revelar existencia)
            if (user == null)
            {
                return Ok(new
                {
                    success = false,
                    codigo = "CREDENCIALES_INCORRECTAS",
                    message = "Credenciales incorrectas",
                    bloqueado = false,
                    intentosFallidos = 0,
                    intentosRestantes = MAX_INTENTOS
                });
            }

            // Usuario bloqueado
            if (user.IntentosFallidosConsecutivos >= MAX_INTENTOS)
            {
                return Ok(new
                {
                    success = false,
                    codigo = "USUARIO_BLOQUEADO",
                    message = "Usuario bloqueado por intentos fallidos",
                    bloqueado = true,
                    intentosFallidos = user.IntentosFallidosConsecutivos,
                    intentosRestantes = 0
                });
            }

            // Contraseña incorrecta
            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                user.IntentosFallidosConsecutivos++;

                if (user.IntentosFallidosConsecutivos >= MAX_INTENTOS)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.MaxValue;
                    await _userManager.UpdateAsync(user);

                    return Ok(new
                    {
                        success = false,
                        codigo = "USUARIO_BLOQUEADO",
                        message = "Usuario bloqueado por intentos fallidos",
                        bloqueado = true,
                        intentosFallidos = user.IntentosFallidosConsecutivos,
                        intentosRestantes = 0
                    });
                }

                await _userManager.UpdateAsync(user);

                var restan = MAX_INTENTOS - user.IntentosFallidosConsecutivos;
                return Ok(new
                {
                    success = false,
                    codigo = "PASSWORD_INCORRECTA",
                    message = "Contraseña incorrecta",
                    bloqueado = false,
                    intentosFallidos = user.IntentosFallidosConsecutivos,
                    intentosRestantes = restan
                });
            }

            // Si el login fue exitoso, reiniciar contador de intentos
            user.IntentosFallidosConsecutivos = 0;
            await _userManager.UpdateAsync(user);

            // Por esta línea:
            var sistemasYRoles = await _usuarioSistemaRepository.ObtenerSistemasYRolesDelUsuarioAsync(user.Id);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("nombreCompleto", user.NombreCompleto)
            };

            // Agregar claims personalizados por sistema
            foreach (var grupo in sistemasYRoles.GroupBy(x => x.SistemaId))
            {
                var sistemaId = grupo.Key;
                var sistemaNombre = grupo.First().SistemaNombre;

                // Podrías agregar uno por rol o uno agrupado
                foreach (var rol in grupo)
                {
                    claims.Add(new Claim("sistema_rol", $"{sistemaId}:{rol.RolNombre}"));
                    claims.Add(new Claim(ClaimTypes.Role, rol.RolNombre));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(4);

            Oficina oficina = null;
            if (user.IdOficina.HasValue)
            {
                oficina = await _sasiDbContext.Oficina
                    .FirstOrDefaultAsync(o => o.IdOficina == user.IdOficina.Value);

                if (oficina != null)
                {
                    claims.Add(new Claim("OficinaId", oficina.IdOficina.ToString()));
                    claims.Add(new Claim("OficinaNombre", oficina.Nombre));
                }
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var idsPadreGlobales = sistemasYRoles
                .SelectMany(sr => sr.Objetos)
                .Where(o => o.Tipo == "Submenu" && o.IdPadre != null)
                .Select(o => o.IdPadre.Value)
                .Distinct()
                .ToList();

            var menusPadreGlobales = _sasiDbContext.Objetos
                .Where(o => idsPadreGlobales.Contains(o.IdObjeto))
                .Select(o => new ObjetoDto
                {
                    IdObjeto = o.IdObjeto,
                    Nombre = o.Nombre,
                    Tipo = o.Tipo,
                    Url = o.Url,
                    Titulo = o.Titulo,
                    Icono = o.Icono,
                    Activo = o.Activo,
                    Orden = o.Orden,
                    IdPadre = o.IdPadre
                })
                .ToList();

            var sistemasEstructurados = sistemasYRoles
                            .GroupBy(x => new { x.SistemaId, x.SistemaNombre, x.SistemaActivo })
                            .Select(g => new {
                                id = g.Key.SistemaId,
                                nombre = g.Key.SistemaNombre,
                                activo = g.Key.SistemaActivo,
                                roles = g.Select(r =>
                                {
                                    // 1️⃣ Objetos asignados directamente al rol
                                    var objetosRol = r.Objetos
                                        .Where(o => o.Activo)
                                        .ToList();

                                    // 2️⃣ Submenus del rol
                                    var submenus = objetosRol
                                        .Where(o => o.Tipo == "Submenu" && o.IdPadre != null)
                                        .ToList();

                                    // 3️⃣ IDs de los menús padre
                                    var idsPadre = submenus
                                        .Select(s => s.IdPadre.Value)
                                        .Distinct()
                                        .ToList();

                                    // 4️⃣ Menús padre que NO están asignados pero son necesarios
                                    var menusPadre = menusPadreGlobales
                                        .Where(o => idsPadre.Contains(o.IdObjeto))
                                        .ToList();

                                    // 5️⃣ Unir todo
                                    var objetosFinales = objetosRol
                                        .Concat(menusPadre)
                                        .DistinctBy(o => o.IdObjeto)
                                        .Select(o => new {
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
                            }).ToList();

            return Ok(new
            {
                success = true,
                bloqueado = false,
                intentosFallidos = 0,
                token = new JwtSecurityTokenHandler().WriteToken(token),
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
            });
        }

        [HttpGet("accesos-usuario/{userName}")]
        public async Task<IActionResult> ObtenerAccesosPorUsuario(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound("Usuario no encontrado");

            var sistemasYRoles = await _usuarioSistemaRepository.ObtenerSistemasYRolesDelUsuarioAsync(user.Id);

            Oficina oficina = null;
            if (user.IdOficina.HasValue)
            {
                oficina = await _sasiDbContext.Oficina
                    .FirstOrDefaultAsync(o => o.IdOficina == user.IdOficina.Value);
            }

            var sistemasEstructurados = sistemasYRoles
                .GroupBy(x => new { x.SistemaId, x.SistemaNombre, x.SistemaActivo })
                .Select(g => new {
                    id = g.Key.SistemaId,
                    nombre = g.Key.SistemaNombre,
                    activo = g.Key.SistemaActivo,
                    roles = g.Select(r => new {
                        idRol = r.RolId,
                        nombreRol = r.RolNombre,
                        activo = r.UsuarioSistemaRolActivo,
                        esPrincipal = r.EsPrincipal,
                        objetos = r.Objetos.Select(o => new {
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

            return Ok(new
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
            });
        }

        [HttpGet("usuarios/{id}/basico")]
        public async Task<IActionResult> ObtenerUsuarioBasico(Guid id)
        {
            var user = await _userManager.Users
                .Where(u => u.Id == id)
                .Select(u => new UsuarioBasicoResponse
                {
                    IdUsuario = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost("usuarios/basicos")]
        public async Task<IActionResult> ObtenerUsuariosBasicos([FromBody] List<Guid> ids)
        {
            var usuarios = await _userManager.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new UsuarioBasicoResponse
                {
                    IdUsuario = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        [HttpPost("sga/crear-alumno")]
        public async Task<IActionResult> CrearAlumnoDesdeSga([FromBody] NuevoUsuarioApiRequest dto)
        {
            // 1. Validaciones de existencia
            var existe = await _userManager.FindByEmailAsync(dto.Email);
            if (existe != null) return Conflict(new { message = "El correo ya existe" });

            // 2. Definir el nuevo usuario
            var usuario = new ApplicationUser
            {
                UserName = dto.Dni,
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto,
                Activo = true,
                AuditUsuarioCreacion = "SGA_SYSTEM",
                AuditFechaCreacion = DateTime.Now,
                DebeCambiarPassword = true,
                IpCreacion = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                IdOficina = 9999
            };

            // 3. Crear en AspNetUsers
            var resultado = await _userManager.CreateAsync(usuario, dto.Dni + "Sga.");

            if (resultado.Succeeded)
            {
                try
                {
                    // 4. ASIGNACIÓN EN UsuarioSistema (Configuración de acceso al SGA)
                    var asignacion = new UsuarioSistema
                    {
                        UsuarioId = usuario.Id,    // El GUID recién generado
                        SistemaId = 14,            // ID del SGA
                        RolId = 13,                // ID del Rol Alumno
                        FechaAsignacion = DateTime.Now,
                        Activo = true,
                        EsPrincipal = true
                    };

                    await _usuarioSistemaRepository.AsignarUsuarioASistemaAsync(asignacion.UsuarioId.ToString(), asignacion.SistemaId, asignacion.RolId, asignacion.EsPrincipal);

                    // 5. Retornar éxito al SGA
                    return Ok(new { success = true, userId = usuario.Id });
                }
                catch (Exception ex)
                {
                    // Si falla la asignación del sistema, deberíamos borrar al usuario 
                    // o manejar el error para no dejarlo a medias
                    return StatusCode(500, new { message = "Usuario creado pero falló la asignación al sistema: " + ex.Message });
                }
            }

            return BadRequest(new { success = false, errors = resultado.Errors });
        }
    }
}
