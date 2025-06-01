using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VectoresNumericos_Luna_Chino.Models
{
	public class AsistenciaAlumno
	{
        public string NombreAlumno { get; set; }
        public DateTime FechaSesion { get; set; }
        public bool Presente { get; set; }
        public bool Tardanza { get; set; }
        public int TiempoPresente { get; set; } // en segundos
    }
}