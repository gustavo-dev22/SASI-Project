using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace SASI.Logging
{
    public class PiiLogEventEnricher : ILogEventEnricher
    {
        private static readonly HashSet<string> CamposSensibles = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "pass", "clave", "secret", "secretkey", "token", "refreshtoken", "apikey",
            "dni", "documento", "nrodoc", "email", "correo", "telefono", "phone", "celular", "ipcreacion", "ipmodificacion"
        };

        private static readonly Regex EmailRegex = new(@"[\w\.\-]+@[\w\.\-]+\.\w{2,}", RegexOptions.Compiled);
        private static readonly Regex DniRegex = new(@"\b\d{8}\b", RegexOptions.Compiled);

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var aEnmascarar = new List<KeyValuePair<string, LogEventPropertyValue>>();

            foreach (var kv in logEvent.Properties)
            {
                if (CamposSensibles.Contains(kv.Key))
                {
                    aEnmascarar.Add(new KeyValuePair<string, LogEventPropertyValue>(kv.Key, new ScalarValue("***")));
                }
                else if (kv.Value is ScalarValue { Value: string texto } &&
                         (EmailRegex.IsMatch(texto) || DniRegex.IsMatch(texto)))
                {
                    var redactado = EmailRegex.Replace(texto, "***");
                    redactado = DniRegex.Replace(redactado, "***");
                    aEnmascarar.Add(new KeyValuePair<string, LogEventPropertyValue>(kv.Key, new ScalarValue(redactado)));
                }
            }

            foreach (var kv in aEnmascarar)
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(kv.Key, kv.Value));
            }
        }
    }
}
