using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.DTO
{
    public class ResultadoCambioEstadoDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }

        public static ResultadoCambioEstadoDto Ok(string mensaje = "Estado actualizado correctamente.")
            => new ResultadoCambioEstadoDto { Exito = true, Mensaje = mensaje };

        public static ResultadoCambioEstadoDto Error(string mensaje)
            => new ResultadoCambioEstadoDto { Exito = false, Mensaje = mensaje };
    }
}
