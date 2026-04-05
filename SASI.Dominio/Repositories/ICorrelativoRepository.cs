using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface ICorrelativoRepository
    {
        Task<int> ObtenerSiguienteCorrelativoAsync(string entidad);
        Task ActualizarCorrelativo(string entidad, int nuevoNumero);
        Task<int> ObtenerValorActualCorrelativo(string entidad);
    }
}
