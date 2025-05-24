using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace VectoresNumericos_Luna_Chino.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult DashboardAlumno()
        {
            // Aquí podrías pasar el modelo con datos del alumno si quieres
            return View();
        }

        public ActionResult DashboardDocente()
        {
            return View();
        }
    }
}