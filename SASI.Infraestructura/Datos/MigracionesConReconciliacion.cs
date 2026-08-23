using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging;

namespace SistemaConvocatorias.Infraestructura.Datos
{
    /// <summary>
    /// Aplica las migraciones de EF Core reconciliando antes el historial de migraciones.
    /// Si una migración pendiente ya está reflejada en el esquema (p. ej. la BD se creó con un
    /// script/backup sin historial, o tras un pull llegaron migraciones que no figuran en
    /// __EFMigrationsHistory), se registra como aplicada en lugar de fallar con
    /// "Ya hay un objeto con el nombre '...' en la base de datos".
    /// </summary>
    public static class MigracionesConReconciliacion
    {
        public static async Task AplicarAsync(DbContext context, string nombreContexto, ILogger logger)
        {
            var migrator = context.Database.GetService<IMigrator>();

            if (!await context.Database.CanConnectAsync())
            {
                logger.LogInformation("[BD {Nombre}] La base de datos no existe; se creará aplicando todas las migraciones.", nombreContexto);
                await migrator.MigrateAsync();
                return;
            }

            var ensamblado = context.Database.GetService<IMigrationsAssembly>();

            var pendientes = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pendientes.Count == 0)
            {
                logger.LogInformation("[BD {Nombre}] No hay migraciones pendientes.", nombreContexto);
                return;
            }

            logger.LogInformation("[BD {Nombre}] {Cantidad} migración(es) pendiente(s): {Ids}",
                nombreContexto, pendientes.Count, string.Join(", ", pendientes));

            var (tablas, columnas, indices) = await ObtenerEsquemaAsync(context);

            var yaReflejadas = new List<string>();
            foreach (var id in pendientes)
            {
                if (!ensamblado.Migrations.TryGetValue(id, out var tipoMigracion))
                    continue;

                var migracion = ensamblado.CreateMigration(tipoMigracion, context.Database.ProviderName!);
                var operaciones = migracion.UpOperations;
                if (operaciones.Count > 0 && operaciones.All(op => OperacionYaReflejadaEnEsquema(op, tablas, columnas, indices)))
                    yaReflejadas.Add(id);
            }

            if (yaReflejadas.Count > 0)
            {
                var historia = context.Database.GetService<IHistoryRepository>();
                if (!historia.Exists())
                    await context.Database.ExecuteSqlRawAsync(historia.GetCreateIfNotExistsScript());

                foreach (var id in yaReflejadas)
                {
                    logger.LogWarning(
                        "[BD {Nombre}] La migración '{Id}' ya está reflejada en el esquema (BD previa sin historial). Se registra como aplicada sin ejecutarla.",
                        nombreContexto, id);
                    await context.Database.ExecuteSqlRawAsync(
                        historia.GetInsertScript(new HistoryRow(id, ProductVersionEntityFramework())));
                }
            }

            pendientes = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pendientes.Count > 0)
            {
                logger.LogInformation("[BD {Nombre}] Aplicando {Cantidad} migración(es) restante(s): {Ids}",
                    nombreContexto, pendientes.Count, string.Join(", ", pendientes));
                await migrator.MigrateAsync();
                logger.LogInformation("[BD {Nombre}] Migraciones aplicadas correctamente.", nombreContexto);
            }
            else
            {
                logger.LogInformation("[BD {Nombre}] Esquema al día tras reconciliación.", nombreContexto);
            }
        }

        // Indica si una operación de migración ya está reflejada en el esquema actual de la BD.
        private static bool OperacionYaReflejadaEnEsquema(
            MigrationOperation operacion,
            HashSet<string> tablas,
            HashSet<string> columnas,
            HashSet<string> indices)
        {
            return operacion switch
            {
                CreateTableOperation crear => tablas.Contains(Norm(crear.Name)),
                AddColumnOperation columna => columnas.Contains($"{Norm(columna.Table)}|{Norm(columna.Name)}"),
                CreateIndexOperation indice => indices.Contains(Norm(indice.Name)),
                _ => false
            };
        }

        // Obtiene el esquema actual de la BD (tablas, columnas e índices) para la reconciliación.
        private static async Task<(HashSet<string> Tablas, HashSet<string> Columnas, HashSet<string> Indices)> ObtenerEsquemaAsync(DbContext context)
        {
            var tablas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var indices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var connection = context.Database.GetDbConnection();
            var abrir = connection.State != ConnectionState.Open;
            if (abrir)
                await connection.OpenAsync();

            try
            {
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sys.tables";
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        tablas.Add(reader.GetString(0));
                }

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT t.name, c.name FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id";
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        columnas.Add($"{reader.GetString(0)}|{reader.GetString(1)}");
                }

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sys.indexes WHERE is_unique_constraint = 0 AND type > 0";
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        indices.Add(reader.GetString(0));
                }
            }
            finally
            {
                if (abrir)
                    await connection.CloseAsync();
            }

            return (tablas, columnas, indices);
        }

        private static string Norm(string valor) => valor.ToUpperInvariant();

        private static string ProductVersionEntityFramework()
            => typeof(IMigrator).Assembly.GetName().Version?.ToString(3) ?? "8.0.0";
    }
}
