namespace SASI.Requests
{
    public class ConsultaAntecedentesRequest
    {
        public string clienteUsuario { get; set; }
        public string clienteClave { get; set; }
        public string servicioCodigo { get; set; } = "WS_PIDE_ANTECEDENTES_FLAG";
        public string clienteSistema { get; set; }
        public string clienteIp { get; set; }
        public string clienteMac { get; set; }
        public int tipoDocUserClieFin { get; set; } = 2;
        public string nroDocUserClieFin { get; set; }
        public string nroDoc { get; set; }
    }
}
