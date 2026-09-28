using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SASI.Aplicacion.Servicios;
using SASI.Configuration;

namespace SASI.Servicios
{
    // Cachea en sesión las acciones concedidas al rol activo, para usarlas en las vistas.
    public interface IPermisoUsuarioServicio
    {
        bool EsAdministrador { get; }
        bool Tiene(string modulo, string codigoAccion);
        Task EstablecerAsync(int rolId);
        void Limpiar();
    }

    public class PermisoUsuarioServicio : IPermisoUsuarioServicio
    {
        private const string ClaveSesion = "PermisosUsuario";
        private const string RolAdministrador = "Administrador";
        private const string RolAdministradorSeguridad = "Administrador de Seguridad";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPermisoServicio _permisoServicio;
        private readonly IObjetoServicio _objetoServicio;
        private readonly int _sistemaId;

        public PermisoUsuarioServicio(
            IHttpContextAccessor httpContextAccessor,
            IPermisoServicio permisoServicio,
            IObjetoServicio objetoServicio,
            IOptions<ConfiguracionSistemaSASI> config)
        {
            _httpContextAccessor = httpContextAccessor;
            _permisoServicio = permisoServicio;
            _objetoServicio = objetoServicio;
            _sistemaId = config.Value.Id;
        }

        private HttpContext? Contexto => _httpContextAccessor.HttpContext;

        public bool EsAdministrador
        {
            get
            {
                var user = Contexto?.User;
                return user != null &&
                       (user.IsInRole(RolAdministrador) || user.IsInRole(RolAdministradorSeguridad));
            }
        }

        public bool Tiene(string modulo, string codigoAccion)
        {
            if (EsAdministrador)
                return true;

            var permisos = Obtener();
            if (permisos == null || permisos.Count == 0)
                return true; // sin acciones configuradas: fail-open en la UI

            var clave = Normalizar(modulo);
            var entrada = permisos.FirstOrDefault(p => Normalizar(p.Key) == clave);
            if (entrada.Key == null)
                return true; // el módulo no tiene acciones configuradas

            return entrada.Value.Contains(codigoAccion, StringComparer.OrdinalIgnoreCase);
        }

        public async Task EstablecerAsync(int rolId)
        {
            if (Contexto?.Session == null || rolId <= 0)
                return;

            var permisos = await _permisoServicio.ObtenerPermisosPorRolSistemaAsync(rolId, _sistemaId);
            var objetos = await _objetoServicio.ObtenerPorSistemaAsync(_sistemaId);
            var urlPorObjeto = objetos.ToDictionary(o => o.IdObjeto, o => o.Url ?? string.Empty);

            var mapa = new Dictionary<string, List<string>>();
            foreach (var item in permisos)
            {
                var url = urlPorObjeto.GetValueOrDefault(item.Key, string.Empty);
                var clave = Normalizar(url);
                if (string.IsNullOrEmpty(clave))
                    continue;

                if (!mapa.TryGetValue(clave, out var lista))
                {
                    lista = new List<string>();
                    mapa[clave] = lista;
                }

                foreach (var accion in item.Value)
                {
                    if (!lista.Contains(accion, StringComparer.OrdinalIgnoreCase))
                        lista.Add(accion);
                }
            }

            Contexto.Session.SetString(ClaveSesion, JsonConvert.SerializeObject(mapa));
        }

        public void Limpiar()
        {
            Contexto?.Session?.Remove(ClaveSesion);
        }

        private Dictionary<string, List<string>>? Obtener()
        {
            var json = Contexto?.Session?.GetString(ClaveSesion);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string Normalizar(string? url)
            => string.IsNullOrWhiteSpace(url)
                ? string.Empty
                : url.Split('/', StringSplitOptions.RemoveEmptyEntries)[0].Trim().ToLowerInvariant();
    }
}
