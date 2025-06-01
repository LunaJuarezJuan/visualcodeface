using Npgsql;
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
        private string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        // GET: Mostrar formulario para crear sesión
        public ActionResult CrearSesion()
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Clases = ObtenerClases(idDocente.Value);
            return View();
        }

        

        // POST: Recibir datos para crear sesión
        [HttpPost]
        public ActionResult CrearSesion(int idClase, DateTime? fechaHoraInicio, DateTime? fechaHoraFin, string nombreSesion)
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null)
                return RedirectToAction("Login", "Account");

            // Validaciones simples antes de insertar
            if (!fechaHoraInicio.HasValue || !fechaHoraFin.HasValue)
            {
                ModelState.AddModelError("", "La fecha y hora de inicio y fin son obligatorias.");
                ViewBag.Clases = ObtenerClases(idDocente.Value);
                return View();
            }

            if (fechaHoraInicio >= fechaHoraFin)
            {
                ModelState.AddModelError("", "La fecha y hora de inicio debe ser menor que la de fin.");
                ViewBag.Clases = ObtenerClases(idDocente.Value);
                return View();
            }

            if (string.IsNullOrWhiteSpace(nombreSesion))
            {
                ModelState.AddModelError("", "El nombre de la sesión es obligatorio.");
                ViewBag.Clases = ObtenerClases(idDocente.Value);
                return View();
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string sql = @"INSERT INTO sesiones (id_docente, id_clase, fecha_hora_inicio, fecha_hora_fin, nombre_sesion) 
                       VALUES (@idDocente, @idClase, @fechaHoraInicio, @fechaHoraFin, @nombreSesion)";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idDocente", idDocente.Value);
                    cmd.Parameters.AddWithValue("idClase", idClase);
                    cmd.Parameters.AddWithValue("fechaHoraInicio", fechaHoraInicio.Value);
                    cmd.Parameters.AddWithValue("fechaHoraFin", fechaHoraFin.Value);
                    cmd.Parameters.AddWithValue("nombreSesion", nombreSesion);

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Mensaje"] = "Sesión creada correctamente.";
            return RedirectToAction("Index");
        }


        // Listar sesiones activas para el docente con nombre de clase
        public ActionResult SesionesActivas()
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null)
                return RedirectToAction("Login", "Account");

            List<SesionConClase> sesiones = new List<SesionConClase>();
            DateTime ahora = DateTime.Now;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT s.id_sesion, s.nombre_sesion, s.fecha_hora_inicio, s.fecha_hora_fin, s.descripcion, c.nombre_clase
                    FROM sesiones s
                    JOIN clases c ON s.id_clase = c.id_clase
                    WHERE s.id_docente = @idDocente
                    AND s.fecha_hora_inicio <= @ahora
                    AND s.fecha_hora_fin >= @ahora
                    ORDER BY s.fecha_hora_inicio";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idDocente", idDocente.Value);
                    cmd.Parameters.AddWithValue("ahora", ahora);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sesiones.Add(new SesionConClase
                            {
                                IdSesion = reader.GetInt32(0),
                                NombreSesion = reader.GetString(1),
                                FechaHoraInicio = reader.GetDateTime(2),
                                FechaHoraFin = reader.GetDateTime(3),
                                Descripcion = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                NombreClase = reader.GetString(5)
                            });
                        }
                    }
                }
            }

            return View(sesiones);
        }

        public ActionResult Index()
        {
            ViewBag.Message = "Bienvenido, docente.";
            return View();
        }

        public ActionResult ClasesDocente()
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null)
                return RedirectToAction("Login", "Account");

            List<Clase> clases = ObtenerClases(idDocente.Value);

            return View(clases);
        }

        public ActionResult ReporteAsistencia(int idClase)
        {
            List<AsistenciaAlumno> reportes = new List<AsistenciaAlumno>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT u.nombre_completo, a.fecha_registro, 
                           CASE WHEN a.estado = 'presente' THEN true ELSE false END AS presente,
                           CASE WHEN a.estado = 'tarde' THEN true ELSE false END AS tardanza,
                           0 AS tiempo_presente
                    FROM asistencia_sesion a
                    JOIN usuarios u ON a.id_usuario = u.id_usuario
                    JOIN sesiones s ON a.id_sesion = s.id_sesion
                    WHERE s.id_clase = @idClase
                    ORDER BY u.nombre_completo, a.fecha_registro";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idClase", idClase);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reportes.Add(new AsistenciaAlumno
                            {
                                NombreAlumno = reader.GetString(0),
                                FechaSesion = reader.GetDateTime(1),
                                Presente = reader.GetBoolean(2),
                                Tardanza = reader.GetBoolean(3),
                                TiempoPresente = reader.GetInt32(4)
                            });
                        }
                    }
                }
            }

            return View(reportes);
        }

        // Método privado para evitar repetir código
        private List<Clase> ObtenerClases(int idDocente)
        {
            var clases = new List<Clase>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string sql = @"SELECT id_clase, nombre_clase FROM clases WHERE id_docente = @idDocente";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idDocente", idDocente);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clases.Add(new Clase
                            {
                                IdClase = reader.GetInt32(0),
                                NombreClase = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return clases;
        }
    }

    // Modelo para sesiones con nombre de clase
    public class SesionConClase : Sesion
    {
        public string NombreClase { get; set; }
    }
}
