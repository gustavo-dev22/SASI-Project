using SASI.Dominio.Modelo;

namespace SASI.Models
{
    public class PermisoRolViewModel
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public int IdSistema { get; set; }
        public string NombreSistema { get; set; } = string.Empty;
        public List<Objeto> Objetos { get; set; } = new();
        public List<Accion> Acciones { get; set; } = new();

        // Claves "idObjeto:idAccion" que el rol tiene concedidas.
        public List<string> PermisosSeleccionados { get; set; } = new();
    }

    public class GuardarPermisosRequest
    {
        public int IdRol { get; set; }
        public List<PermisoItemRequest> Permisos { get; set; } = new();
    }

    public class PermisoItemRequest
    {
        public int IdObjeto { get; set; }
        public int IdAccion { get; set; }
    }
}
