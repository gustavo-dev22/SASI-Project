using SASI.Dominio.Modelo.Commons;

namespace SASI.Dominio.Modelo
{
    // Catálogo global y fijo de acciones que se pueden conceder sobre un módulo/objeto.
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
