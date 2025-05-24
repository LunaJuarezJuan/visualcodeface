using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VectoresNumericos_Luna_Chino.Models;
using Npgsql; 
using System.Configuration;
using System.Security.Cryptography;
using System.Text;


namespace VectoresNumericos_Luna_Chino.Controllers
{
    public class AccountController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [HttpGet]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string sql = "SELECT id_usuario, nombre_completo, correo, contrasena_hash, rol FROM usuarios WHERE correo = @correo AND estado = true";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("correo", model.Correo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hashBD = reader.GetString(reader.GetOrdinal("contrasena_hash"));

                            if (VerifyPassword(model.Contrasena, hashBD))
                            {
                                // Aquí usuario autenticado correctamente
                                // Crea sesión o cookie con datos del usuario
                                var usuario = new UsuarioViewModel
                                {
                                    IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                                    NombreCompleto = reader.GetString(reader.GetOrdinal("nombre_completo")),
                                    Correo = reader.GetString(reader.GetOrdinal("correo")),
                                    Rol = reader.GetString(reader.GetOrdinal("rol"))
                                };

                                // Guarda datos en sesión
                                Session["Usuario"] = usuario;

                                // Redirige según rol
                                if (usuario.Rol == "docente")
                                    return RedirectToAction("DashboardDocente", "Home");
                                else
                                    return RedirectToAction("DashboardAlumno", "Home");
                            }
                        }
                    }
                }
            }

            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(model);
        }

        private bool VerifyPassword(string password, string hash)
        {
            // Asumiendo SHA256 sin sal. Mejor usar algo como BCrypt en producción
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha256.ComputeHash(bytes);
                var hashInput = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hashInput == hash.ToLower();
            }
        }
    }
}