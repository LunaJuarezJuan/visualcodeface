using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VectoresNumericos_Luna_Chino.Models
{
    public class UsuarioViewModel
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public string Rol { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        // Otros campos que quieras mostrar o manejar
    }
}