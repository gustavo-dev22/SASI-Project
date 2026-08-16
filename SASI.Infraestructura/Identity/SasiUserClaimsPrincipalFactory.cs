using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaConvocatorias.Infraestructura.Datos;
using System.Security.Claims;

namespace SASI.Infraestructura.Identity
{
    public class SasiUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
    {
        private readonly SasiDbContext _context;

        public SasiUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            SasiDbContext context)
            : base(userManager, roleManager, optionsAccessor)
        {
            _context = context;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            var roles = await (
                    from us in _context.UsuarioSistemas
                    join rol in _context.Roles on us.RolId equals rol.IdRol
                    where us.UsuarioId == user.Id && us.Activo && rol.Activo
                    select rol.Nombre)
                .Distinct()
                .ToListAsync();

            foreach (var rol in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, rol));
            }

            return identity;
        }
    }
}
