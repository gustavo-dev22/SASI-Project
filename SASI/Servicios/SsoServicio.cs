using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SistemaConvocatorias.Infraestructura.Datos;
using System.Security.Cryptography;
using System.Text;

namespace SASI.Servicios
{
    // Flujo SSO por authorization code + PKCE.
    public class SsoServicio
    {
        private readonly SasiDbContext _db;
        private readonly int _codeTtlSeconds;
        private readonly bool _permitirLoopbackDesarrollo;

        public SsoServicio(SasiDbContext db, IConfiguration config, IHostEnvironment env)
        {
            _db = db;
            _codeTtlSeconds = int.TryParse(config["Sso:CodeTtlSeconds"], out var ttl) ? ttl : 60;

            // En desarrollo se aceptan callbacks en loopback (localhost en cualquier puerto)
            // para no atar el SPA a un puerto fijo. En producción el match sigue siendo exacto.
            _permitirLoopbackDesarrollo = env.IsDevelopment();
        }

        public Task<SistemaCliente?> ObtenerClienteActivoAsync(string clientId)
            => _db.SistemaClientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == clientId && c.Activo);

        public bool RedirectUriValida(SistemaCliente cliente, string? redirectUri)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
                return false;

            var uris = (cliente.RedirectUris ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (uris.Any(u => string.Equals(u, redirectUri, StringComparison.Ordinal)))
                return true;

            return _permitirLoopbackDesarrollo && EsLoopbackLocal(redirectUri);
        }

        private static bool EsLoopbackLocal(string redirectUri)
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            return uri.Host is "localhost" or "127.0.0.1" or "::1" or "[::1]";
        }

        // Destino permitido para el post-logout: mismo origen que algún RedirectUri
        // registrado por un cliente activo (evita open redirect).
        public async Task<bool> ReturnUrlPermitidoAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var destino))
                return false;

            if (_permitirLoopbackDesarrollo && EsLoopbackLocal(url))
                return true;

            var listas = await _db.SistemaClientes.AsNoTracking()
                .Where(c => c.Activo)
                .Select(c => c.RedirectUris)
                .ToListAsync();

            foreach (var lista in listas)
            {
                var uris = (lista ?? string.Empty)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var u in uris)
                {
                    if (Uri.TryCreate(u, UriKind.Absolute, out var registrada) && MismoOrigen(registrada, destino))
                        return true;
                }
            }

            return false;
        }

        private static bool MismoOrigen(Uri a, Uri b)
            => string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase)
               && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)
               && a.Port == b.Port;

        public async Task<string> CrearAuthCodeAsync(
            SistemaCliente cliente,
            Guid usuarioId,
            string redirectUri,
            string? codeChallenge,
            string? codeChallengeMethod)
        {
            var code = GenerarTokenAleatorio();

            _db.AuthCodes.Add(new AuthCode
            {
                CodeHash = Hash(code),
                ClientId = cliente.ClientId,
                UsuarioId = usuarioId,
                SistemaId = cliente.IdSistema,
                RedirectUri = redirectUri,
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod,
                ExpiraUtc = DateTime.UtcNow.AddSeconds(_codeTtlSeconds),
                CreatedUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return code;
        }

        public async Task<(bool Exito, string? Error, Guid UsuarioId)> CanjearAsync(
            string clientId,
            string? clientSecret,
            string code,
            string? codeVerifier,
            string? redirectUri)
        {
            var cliente = await ObtenerClienteActivoAsync(clientId);
            if (cliente == null)
                return (false, "client_id inválido o inactivo.", Guid.Empty);

            if (string.IsNullOrWhiteSpace(code))
                return (false, "code requerido.", Guid.Empty);

            if (!string.IsNullOrWhiteSpace(cliente.ClientSecretHash))
            {
                if (string.IsNullOrWhiteSpace(clientSecret) || !ConstanteIgual(Hash(clientSecret), cliente.ClientSecretHash))
                    return (false, "client_secret inválido.", Guid.Empty);
            }

            var hash = Hash(code);
            var authCode = await _db.AuthCodes.FirstOrDefaultAsync(a => a.CodeHash == hash);
            if (authCode == null || authCode.ClientId != clientId)
                return (false, "code inválido.", Guid.Empty);
            if (authCode.UsadoUtc != null)
                return (false, "code ya utilizado.", Guid.Empty);
            if (authCode.ExpiraUtc < DateTime.UtcNow)
                return (false, "code expirado.", Guid.Empty);

            if (!string.IsNullOrWhiteSpace(authCode.RedirectUri))
            {
                if (string.IsNullOrWhiteSpace(redirectUri) || !string.Equals(authCode.RedirectUri, redirectUri, StringComparison.Ordinal))
                    return (false, "redirect_uri no coincide.", Guid.Empty);
            }

            if (cliente.RequierePkce)
            {
                if (string.IsNullOrWhiteSpace(authCode.CodeChallenge))
                    return (false, "PKCE requerido.", Guid.Empty);
                if (string.IsNullOrWhiteSpace(codeVerifier))
                    return (false, "code_verifier requerido.", Guid.Empty);

                var metodo = string.IsNullOrWhiteSpace(authCode.CodeChallengeMethod)
                    ? "S256"
                    : authCode.CodeChallengeMethod!.ToUpperInvariant();

                if (metodo != "S256")
                    return (false, "code_challenge_method no soportado.", Guid.Empty);

                var esperado = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
                if (!ConstanteIgual(esperado, authCode.CodeChallenge!))
                    return (false, "code_verifier inválido.", Guid.Empty);
            }

            // Revocación atómica: un solo uso.
            var filas = await _db.Database.ExecuteSqlRawAsync(
                "UPDATE AuthCode SET UsadoUtc = GETUTCDATE() WHERE IdAuthCode = @p0 AND UsadoUtc IS NULL",
                authCode.IdAuthCode);

            if (filas == 0)
                return (false, "code ya utilizado.", Guid.Empty);

            return (true, null, authCode.UsuarioId);
        }

        private static string Hash(string valor)
            => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));

        private static string Base64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private static string GenerarTokenAleatorio()
            => Base64Url(RandomNumberGenerator.GetBytes(64));

        private static bool ConstanteIgual(string a, string b)
        {
            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
        }
    }
}
