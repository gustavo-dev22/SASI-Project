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
            var siguiente = await _context.Database
                .SqlQuery<int>($"UPDATE Correlativo SET UltimoNumero = UltimoNumero + 1 OUTPUT INSERTED.UltimoNumero WHERE Entidad = {entidad}")
                .SingleOrDefaultAsync();

            if (siguiente > 0)
                return siguiente;

            var nuevo = new Correlativo { Entidad = entidad, UltimoNumero = 1 };
            _context.Correlativos.Add(nuevo);
            await _context.SaveChangesAsync();

            return nuevo.UltimoNumero;
        }

        public async Task<int> ObtenerValorActualCorrelativo(string entidad)
        {
            var correlativo = await _context.Correlativos
            .FirstOrDefaultAsync(c => c.Entidad == entidad);

            return correlativo?.UltimoNumero ?? 0;
        }
    }
}
