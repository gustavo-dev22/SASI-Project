using SASI.Dominio.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SASI.Dominio.Repositories
{
    public interface IRolRepository
    {
        Task<IEnumerable<Rol>> ObtenerPorSistemaId(int sistemaId);

        Task<Rol> ObtenerPorId(int id);
        Task Crear(Rol rol);
        Task Editar(Rol rol);
        Task Eliminar(int id);
        Task<List<SelectListItem>> ObtenerRolesComoSelectListAsync(int sistemaId);
        int ObtenerIdSistemaPorRol(int idRol);
    }
}
