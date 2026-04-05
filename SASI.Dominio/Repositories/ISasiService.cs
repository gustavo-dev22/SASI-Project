using SASI.Dominio.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface ISasiService
    {
        Task<AccesosSasiResponseDto> ObtenerAccesosUsuario(string userName, string password);

        Task<AccesosSasiResponseDto> ObtenerAccesosUsuario(string userName);
    }
}
