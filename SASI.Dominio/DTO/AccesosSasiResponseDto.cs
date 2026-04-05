namespace SASI.Dominio.DTO
{
    public class AccesosSasiResponseDto
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public UsuarioSasiDto Usuario { get; set; }
    }
}
