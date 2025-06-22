using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VectoresNumericos_Luna_Chino.Models
{
    public class SesionConClase
    {
        public int IdSesion { get; set; }
        public string NombreSesion { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public string Descripcion { get; set; }
        public string NombreClase { get; set; } // Info adicional
    }
}