using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface IRolObjetoRepository
    {
        Task<List<int>> ObtenerIdsObjetosPorRolAsync(int idRol);
        Task EliminarAsignacionesPorRolAsync(int idRol);
        Task AsignarObjetoARolAsync(int idRol, int idObjeto);
        Task ActualizarAsignacionesAsync(int idRol, List<int> idsAsignados);
    }
}
