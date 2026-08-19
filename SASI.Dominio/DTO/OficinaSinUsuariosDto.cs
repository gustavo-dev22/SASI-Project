namespace SASI.Dominio.DTO
{
    public class OficinaSinUsuariosDto
    {
        public int IdOficina { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Sigla { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
