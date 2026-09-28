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
