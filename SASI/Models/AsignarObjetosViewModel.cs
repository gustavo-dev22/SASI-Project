using SASI.Dominio.Modelo;

namespace SASI.Models
{
    public class AsignarObjetosViewModel
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; }
        public List<Objeto> Objetos { get; set; }
        public List<int> IdsAsignados { get; set; }
    }
}
