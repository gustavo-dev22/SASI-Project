using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SistemaConvocatorias.Infraestructura.Datos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Infraestructura.Repositories
{
    public class CorrelativoRepository : ICorrelativoRepository
    {
        private readonly SasiDbContext _context;

        public CorrelativoRepository(SasiDbContext context)
        {
            _context = context;
        }

        public async Task ActualizarCorrelativo(string entidad, int nuevoNumero)
        {
            var correlativo = await _context.Correlativos
                            .FirstOrDefaultAsync(c => c.Entidad == entidad);

            if (correlativo != null)
            {
                correlativo.UltimoNumero = nuevoNumero;
                _context.Correlativos.Update(correlativo);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> ObtenerSiguienteCorrelativoAsync(string entidad)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var correlativo = await _context.Correlativos
                .FirstOrDefaultAsync(c => c.Entidad == entidad);

            if (correlativo == null)
            {
                correlativo = new Correlativo { Entidad = entidad, UltimoNumero = 1 };
                _context.Correlativos.Add(correlativo);
            }
            else
            {
                correlativo.UltimoNumero++;
                _context.Correlativos.Update(correlativo);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return correlativo.UltimoNumero;
        }

        public async Task<int> ObtenerValorActualCorrelativo(string entidad)
        {
            var correlativo = await _context.Correlativos
            .FirstOrDefaultAsync(c => c.Entidad == entidad);

            return correlativo?.UltimoNumero ?? 0;
        }
    }
}
