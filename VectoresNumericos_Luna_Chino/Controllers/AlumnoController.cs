using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VectoresNumericos_Luna_Chino.Models;

namespace VectoresNumericos_Luna_Chino.Controllers
{
    public class AlumnoController : Controller
    {
        private AsistenciaRepository repo = new AsistenciaRepository();

        public ActionResult Index()
        {
            var usuario = Session["Usuario"] as UsuarioViewModel;
            if (usuario == null)
                return RedirectToAction("Login", "Account");

            int porcentajeAsistencia = repo.ObtenerPorcentajeAsistencia(usuario.IdUsuario);

            var model = new AlumnoViewModel
            {
                NombreCompleto = usuario.NombreCompleto,
                Correo = usuario.Correo,
                PorcentajeAsistencia = porcentajeAsistencia
            };

            return View(model);
        }
     
    }
}