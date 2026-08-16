using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SASI.Aplicacion.Servicios;
using SASI.Configuration;
using SASI.Infraestructura.Identity;
using SASI.Models;

namespace SASI.Servicios
{
    public class CuentaLoginResult
    {
        public bool Success { get; set; }
        public string? Tipo { get; set; }
        public int? IntentosRestantes { get; set; }
        public int? OficinaId { get; set; }
        public string? OficinaNombre { get; set; }
        public int? RolSeleccionado { get; set; }
        public List<MenuItemViewModel>? Menu { get; set; }
        public bool RequiereCambioPassword { get; set; }
        public bool PasswordVencida { get; set; }
        public int? DiasRestantesPassword { get; set; }
    }

    public class CuentaServicio
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly IOficinaServicio _oficinaServicio;
        private readonly int _sistemaId;
        private readonly int _diasVencimientoPassword;

        public CuentaServicio(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IUsuarioSistemaServicio usuarioSistemaServicio,
            IOficinaServicio oficinaServicio,
            IOptions<ConfiguracionSistemaSASI> config)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _usuarioSistemaServicio = usuarioSistemaServicio;
            _oficinaServicio = oficinaServicio;
            _sistemaId = config.Value.Id;
            _diasVencimientoPassword = config.Value.DiasVencimientoPassword;
        }

        public async Task<CuentaLoginResult> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !user.Activo)
            {
                return new CuentaLoginResult { Success = false, Tipo = "credencialesInvalidas" };
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                var intentosRestantes = Math.Max(0, _userManager.Options.Lockout.MaxFailedAccessAttempts - user.AccessFailedCount);
                return new CuentaLoginResult { Success = false, Tipo = "credencialesInvalidas", IntentosRestantes = intentosRestantes };
            }

            var tieneAccesoSASI = await _usuarioSistemaServicio.UsuarioTieneRolActivoEnSistemaAsync(user.Id, _sistemaId);
            if (!tieneAccesoSASI)
            {
                await _signInManager.SignOutAsync();
                return new CuentaLoginResult { Success = false, Tipo = "credencialesInvalidas" };
            }

            user.IntentosFallidosConsecutivos = 0;
            await _userManager.UpdateAsync(user);

            var resultado = new CuentaLoginResult { Success = true };

            if (user.IdOficina.HasValue)
            {
                var oficina = await _oficinaServicio.ObtenerPorIdAsync(user.IdOficina.Value);
                if (oficina != null)
                {
                    resultado.OficinaId = oficina.IdOficina;
                    resultado.OficinaNombre = oficina.Nombre;
                }
            }

            if (user.DebeCambiarPassword)
            {
                resultado.Tipo = "cambioPasswordObligatorio";
                resultado.RequiereCambioPassword = true;
                return resultado;
            }

            if (user.FechaUltimoCambioPassword.HasValue)
            {
                var diasDesdeCambio = (DateTime.UtcNow - user.FechaUltimoCambioPassword.Value).TotalDays;
                var diasRestantes = _diasVencimientoPassword - (int)diasDesdeCambio;
                if (diasDesdeCambio >= _diasVencimientoPassword)
                {
                    resultado.Tipo = "cambioPasswordObligatorio";
                    resultado.PasswordVencida = true;
                    return resultado;
                }
                resultado.DiasRestantesPassword = diasRestantes;
            }

            var rolPredeterminado = await _usuarioSistemaServicio.ObtenerRolPredeterminadoAsync(user.Id, _sistemaId);
            if (rolPredeterminado.HasValue)
                resultado.RolSeleccionado = rolPredeterminado;

            var menu = await ConstruirMenuAsync(user.Id, rolPredeterminado);
            if (menu == null)
            {
                await _signInManager.SignOutAsync();
                return new CuentaLoginResult { Success = false, Tipo = "credencialesInvalidas" };
            }

            resultado.Menu = menu;
            return resultado;
        }

        public async Task<List<MenuItemViewModel>> SeleccionarRolAsync(Guid userId, int rolId)
        {
            var sistemasYRoles = await _usuarioSistemaServicio.ObtenerSistemasYRolesDelUsuarioAsync(userId);

            if (!sistemasYRoles.Any(sr => sr.SistemaId == _sistemaId && sr.SistemaActivo))
                return new List<MenuItemViewModel>();

            var nuevoRol = sistemasYRoles
                .FirstOrDefault(sr => sr.RolId == rolId && sr.SistemaId == _sistemaId && sr.UsuarioSistemaRolActivo);

            if (nuevoRol == null)
                return new List<MenuItemViewModel>();

            return nuevoRol.Objetos
                .Where(o => o.Activo)
                .GroupBy(o => o.IdObjeto)
                .Select(g => g.First())
                .Select(o => new MenuItemViewModel
                {
                    Id = o.IdObjeto,
                    Nombre = o.Nombre,
                    Url = o.Url,
                    Icono = o.Icono ?? string.Empty,
                    Tipo = o.Tipo,
                    IdPadre = o.IdPadre,
                    Orden = o.Orden
                })
                .ToList();
        }

        public async Task<(bool Exito, string? Error)> CambiarPasswordObligatorioAsync(string email, string nuevaPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return (false, "Usuario no encontrado.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, nuevaPassword);
            if (!result.Succeeded)
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));

            user.FechaUltimoCambioPassword = HoraPeru();
            user.DebeCambiarPassword = false;
            await _userManager.UpdateAsync(user);

            return (true, null);
        }

        private async Task<List<MenuItemViewModel>?> ConstruirMenuAsync(Guid userId, int? rolId)
        {
            var sistemasYRoles = await _usuarioSistemaServicio.ObtenerSistemasYRolesDelUsuarioAsync(userId);

            if (!sistemasYRoles.Any(sr => sr.SistemaId == _sistemaId && sr.SistemaActivo && sr.UsuarioSistemaRolActivo))
                return null;

            if (!rolId.HasValue)
                return new List<MenuItemViewModel>();

            var rolActivo = sistemasYRoles
                .FirstOrDefault(sr => sr.RolId == rolId.Value && sr.SistemaId == _sistemaId && sr.UsuarioSistemaRolActivo);

            if (rolActivo == null)
                return new List<MenuItemViewModel>();

            return rolActivo.Objetos
                .Where(o => o.Activo)
                .Select(o => new MenuItemViewModel
                {
                    Id = o.IdObjeto,
                    Nombre = o.Nombre,
                    Url = o.Url,
                    Icono = o.Icono ?? string.Empty,
                    Tipo = o.Tipo,
                    IdPadre = o.IdPadre,
                    Orden = o.Orden
                })
                .ToList();
        }

        private static DateTime HoraPeru()
        {
            TimeZoneInfo peruZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruZone);
        }
    }
}
