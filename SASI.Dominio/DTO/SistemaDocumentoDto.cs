namespace SASI.Dominio.DTO
{
    public class SistemaDocumentoDto
    {
        public int IdSistemaDocumento { get; set; }
        public int SistemaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string TipoDoc { get; set; } = string.Empty; // Manual, Diagrama, Acta
        public string? RutaArchivo { get; set; }
        public DateTime FechaSubida { get; set; }
        public string? UsuarioSubida { get; set; }
    }
}
