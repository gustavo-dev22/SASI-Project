using SASI.Dominio.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface IObjetoRepository
    {
        Task<IEnumerable<Objeto>> ObtenerPorSistemaAsync(int idSistema);
        Task<Objeto?> ObtenerPorIdAsync(int id);
        Task CrearAsync(Objeto objeto);
        Task ActualizarAsync(Objeto objeto);
        Task EliminarAsync(int id);
        Task<IEnumerable<Objeto>> ListarObjetosPadrePorSistemaAsync(int idSistema);
        Task<bool> ExistenObjetosParaSistema(int idSistema);
    }
}
