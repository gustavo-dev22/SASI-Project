using SASI.Dominio.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface ISistemaRepository
    {
        Task<List<Sistema>> ListarAsync();
        Task CrearAsync(Sistema sistema);
        Task<(bool Exito, string Mensaje)> EliminarAsync(int id);
        Task Actualizar(Sistema sistema);
        Task<Sistema> ObtenerPorId(int id);
        Task<(bool Exito, string Mensaje)> ActualizarEstadoAsync(int id);
    }
}
