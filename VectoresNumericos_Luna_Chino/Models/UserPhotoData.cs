using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VectoresNumericos_Luna_Chino.Models
{
	public class UserPhotoData
	{
        public List<string> FotosBase64 { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Contrasena { get; set; }  

    }
}