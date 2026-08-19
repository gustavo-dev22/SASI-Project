namespace SASI.Dominio.DTO
{
    public class ContinuidadDto
    {
        public int SistemaId { get; set; }
        public int? RpoHoras { get; set; }
        public int? RtoHoras { get; set; }
        public string? PoliticaRespaldo { get; set; }
        public DateTime? FechaUltimaPruebaRestauracion { get; set; }

        // Información derivada para la UI
        public int? DiasDesdeUltimaPrueba { get; set; }
    }
}
