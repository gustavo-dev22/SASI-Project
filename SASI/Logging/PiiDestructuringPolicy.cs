using Serilog.Core;
using Serilog.Events;

namespace SASI.Logging
{
    public class PiiDestructuringPolicy : IDestructuringPolicy
    {
        private static readonly HashSet<string> CamposSensibles = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "pass", "clave", "secret", "secretkey", "token", "refreshtoken", "apikey",
            "dni", "documento", "nrodoc", "email", "correo", "telefono", "phone", "celular", "ipcreacion", "ipmodificacion"
        };

        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            if (value is not IDictionary<string, object> dict)
            {
                result = null!;
                return false;
            }

            var redacted = new Dictionary<string, object>();
            foreach (var kv in dict)
            {
                redacted[kv.Key] = CamposSensibles.Contains(kv.Key) ? "***" : kv.Value;
            }

            result = propertyValueFactory.CreatePropertyValue(redacted, true);
            return true;
        }
    }
}
