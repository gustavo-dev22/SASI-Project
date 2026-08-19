using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SASI.Infraestructura.Identity;
using SASI.Models;
using SistemaConvocatorias.Infraestructura.Datos;

namespace SASI.Servicios
{
    public interface IDashboardServicio
    {
        Task<HomeDashboardViewModel> ObtenerTotalesAsync();
    }

    public class DashboardServicio : IDashboardServicio
    {
        private readonly SasiDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardServicio(SasiDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<HomeDashboardViewModel> ObtenerTotalesAsync()
        {
            var activos = await _context.Sistemas.CountAsync(s => s.Activo);
            var inactivos = await _context.Sistemas.CountAsync(s => !s.Activo);
            var roles = await _context.Roles.CountAsync();
            var oficinas = await _context.Oficina.CountAsync();
            var usuarios = await _userManager.Users.CountAsync();

            return new HomeDashboardViewModel
            {
                TotalSistemas = activos + inactivos,
                SistemasActivos = activos,
                SistemasInactivos = inactivos,
                TotalRoles = roles,
                TotalOficinas = oficinas,
                TotalUsuarios = usuarios
            };
        }
    }
}
