namespace SASI.Infraestructura.Identity
{
    // Esquemas de autenticación de SASI.
    // La consola usa la cookie de Identity (.SASI.Auth); el flujo SSO usa una
    // cookie propia para que ambas sesiones sean independientes.
    public static class SasiAuthSchemes
    {
        public const string Sso = "SasiSso";
        public const string SsoCookieName = ".SASI.Sso";
    }
}
