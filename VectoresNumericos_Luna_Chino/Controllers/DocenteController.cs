using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VectoresNumericos_Luna_Chino.Models;

namespace VectoresNumericos_Luna_Chino.Controllers
{
    public class DocenteController : Controller
    {
        private static List<Docente> docentes = new List<Docente>
    {
        new Docente { Usuario = "profe1", Contrasena = "1234" },
        new Docente { Usuario = "lucrecia", Contrasena = "abcd" }
    };

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string usuario, string contrasena)
        {
            var docente = docentes.Find(d => d.Usuario == usuario && d.Contrasena == contrasena);

            if (docente != null)
            {
                Session["Docente"] = docente.Usuario;
                return RedirectToAction("Panel");
            }

            ViewBag.Error = "Credenciales incorrectas.";
            return View();
        }

        public ActionResult Panel()
        {
            if (Session["Docente"] == null)
                return RedirectToAction("Login");

            ViewBag.Usuario = Session["Docente"];
            return View();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}