using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface IOficinaRepository
    {
        Task<List<Oficina>> ListarAsync();
        Task CrearAsync(Oficina oficina);
        Task Actualizar(Oficina oficina);
        Task<IEnumerable<Oficina>> ListarActivasAsync();
        Task<(bool Exito, string Mensaje)> ActualizarEstadoAsync(int id);
        Task<Oficina> ObtenerPorId(int id);
        Task<Oficina> ObtenerPorNombre(string nombre);
        Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorOficinaAsync(int idOficina);
    }
}
