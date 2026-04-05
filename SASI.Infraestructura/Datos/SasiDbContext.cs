using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Modelo.Commons;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaConvocatorias.Infraestructura.Datos
{
    public class SasiDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserContext _userContext;

        public SasiDbContext(DbContextOptions<SasiDbContext> options, IHttpContextAccessor httpContextAccessor, IUserContext userContext)
        : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _userContext = userContext;
        }

        public DbSet<Sistema> Sistemas { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<UsuarioSistema> UsuarioSistemas { get; set; }
        public DbSet<Objeto> Objetos { get; set; }
        public DbSet<RolObjeto> RolObjetos { get; set; }
        public DbSet<Correlativo> Correlativos { get; set; }
        public DbSet<Oficina> Oficina { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sistema>().ToTable("Sistemas");

            modelBuilder.Entity<Rol>().ToTable("Roles");

            modelBuilder.Entity<Usuario>().ToTable("Usuarios");

            modelBuilder.Entity<UsuarioSistema>().ToTable("UsuarioSistema");

            modelBuilder.Entity<Objeto>().ToTable("Objeto");

            modelBuilder.Entity<RolObjeto>().ToTable("RolObjeto");

            modelBuilder.Entity<Correlativo>().ToTable("Correlativo");

            modelBuilder.Entity<Oficina>().ToTable("Oficina");

            modelBuilder.Entity<Sistema>()
                .HasKey(p => p.IdSistema);

            modelBuilder.Entity<Rol>()
                .HasKey(p => p.IdRol);

            modelBuilder.Entity<Usuario>()
                .HasKey(p => p.IdUsuario);

            modelBuilder.Entity<UsuarioSistema>()
                .HasKey(p => p.IdUsuarioSistema);

            modelBuilder.Entity<Objeto>()
                .HasKey(p => p.IdObjeto);

            modelBuilder.Entity<RolObjeto>()
                .HasKey(p => p.IdRolObjeto);

            modelBuilder.Entity<Correlativo>()
                .HasKey(c => c.Entidad);

            modelBuilder.Entity<Oficina>()
                .HasKey(c => c.IdOficina);

            modelBuilder.Entity<Rol>()
                .HasOne(r => r.Sistema)
                .WithMany(s => s.Roles)
                .HasForeignKey(r => r.IdSistema);

            // Relación Objeto -> ObjetoPadre (auto-relación)
            modelBuilder.Entity<Objeto>()
                .HasOne(o => o.ObjetoPadre)
                .WithMany(o => o.Hijos)
                .HasForeignKey(o => o.IdPadre) // 👈 columna real en BD
                .OnDelete(DeleteBehavior.Restrict); // evita eliminación en cascada

            // Relación Objeto -> Sistema
            modelBuilder.Entity<Objeto>()
                .HasOne(o => o.Sistema)
                .WithMany()
                .HasForeignKey(o => o.IdSistema); // 👈 columna real en BD

            // Relación RolObjeto -> Rol
            modelBuilder.Entity<RolObjeto>()
                .HasOne(ro => ro.Rol)
                .WithMany()
                .HasForeignKey(ro => ro.IdRol);

            // Relación RolObjeto -> Objeto
            modelBuilder.Entity<RolObjeto>()
                .HasOne(ro => ro.Objeto)
                .WithMany()
                .HasForeignKey(ro => ro.IdObjeto);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;

            var usuario = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Sistema";

            foreach (var entry in ChangeTracker.Entries<AuditoriaBase>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.AuditFechaCreacion = now;
                    entry.Entity.AuditUsuarioCreacion ??= usuario;
                    entry.Entity.IpCreacion = _userContext.GetIpAddress();
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.AuditFechaModificacion = now;
                    entry.Entity.AuditUsuarioModificacion = usuario;
                    entry.Entity.IpModificacion = _userContext.GetIpAddress();
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}