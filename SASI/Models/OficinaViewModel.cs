namespace SASI.Models
{
    public class OficinaViewModel
    {
        public int IdOficina { get; set; }
        public string Nombre { get; set; }
        public string Sigla { get; set; }
        public bool TieneOficinaPadre { get; set; }
        public int? IdOficinaPadre { get; set; }
        public bool Activo { get; set; }
    }
}
