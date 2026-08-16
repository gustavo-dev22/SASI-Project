using SASI.Dominio.Modelo;

namespace SASI.Models
{
    public class AsignarObjetosViewModel
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public List<Objeto> Objetos { get; set; } = default!;
        public List<int> IdsAsignados { get; set; } = default!;
    }
}
