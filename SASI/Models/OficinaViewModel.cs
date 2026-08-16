namespace SASI.Models
{
    public class OficinaViewModel
    {
        public int IdOficina { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Sigla { get; set; } = string.Empty;
        public bool TieneOficinaPadre { get; set; }
        public int? IdOficinaPadre { get; set; }
        public bool Activo { get; set; }
    }
}
