using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using VectoresNumericos_Luna_Chino.Models;

using static System.Collections.Specialized.BitVector32;

namespace VectoresNumericos_Luna_Chino.Controllers
{
    public class DocenteController : Controller
    {
        private string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        // GET: Mostrar formulario para crear sesión
        public ActionResult CrearSesion()
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            ViewBag.Clases = ObtenerClases(idDocente.Value);
            return View();
        }

        // POST: Recibir datos para crear sesión
        [HttpPost]
        public ActionResult CrearSesion(int idClase, DateTime? fechaHoraInicio, DateTime? fechaHoraFin, string nombreSesion, string descripcion)
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            // Validaciones
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

            // ✅ Validar máximo 2 horas
            if ((fechaHoraFin.Value - fechaHoraInicio.Value).TotalHours > 2)
            {
                ModelState.AddModelError("", "La duración máxima de una sesión es de 2 horas.");
                ViewBag.Clases = ObtenerClases(idDocente.Value);
                return View();
            }

            if (string.IsNullOrWhiteSpace(nombreSesion))
            {
                ModelState.AddModelError("", "El nombre de la sesión es obligatorio.");
                ViewBag.Clases = ObtenerClases(idDocente.Value);
                return View();
            }

            if (string.IsNullOrWhiteSpace(descripcion))
            {
                descripcion = "";
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO sesiones (id_docente, id_clase, fecha_hora_inicio, fecha_hora_fin, nombre_sesion, descripcion) 
              VALUES (@idDocente, @idClase, @fechaHoraInicio, @fechaHoraFin, @nombreSesion, @descripcion)";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idDocente", idDocente.Value);
                    cmd.Parameters.AddWithValue("idClase", idClase);
                    cmd.Parameters.AddWithValue("fechaHoraInicio", fechaHoraInicio.Value);
                    cmd.Parameters.AddWithValue("fechaHoraFin", fechaHoraFin.Value);
                    cmd.Parameters.AddWithValue("nombreSesion", nombreSesion);
                    cmd.Parameters.AddWithValue("descripcion", descripcion);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Mensaje"] = "Sesión creada correctamente.";
            return RedirectToAction("SesionesActivas");
        }


        public ActionResult SesionesActivas(string estadoFiltro = "todos", string claseFiltro = "todas")
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            List<SesionConClase> sesiones = new List<SesionConClase>();
            var estados = new List<string> { "activo", "inactivo", "finalizado", "deshabilitado" };
            var clases = new List<string>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string claseQuery = "SELECT DISTINCT nombre_clase FROM clases WHERE id_docente = @idDocente";
                using (var cmdClase = new NpgsqlCommand(claseQuery, conn))
                {
                    cmdClase.Parameters.AddWithValue("idDocente", idDocente.Value);
                    using (var reader = cmdClase.ExecuteReader())
                        while (reader.Read()) clases.Add(reader.GetString(0));
                }

                string sql = @"
            SELECT s.id_sesion, s.nombre_sesion, s.fecha_hora_inicio, 
                   s.fecha_hora_fin, s.descripcion, c.nombre_clase, s.estado
            FROM sesiones s
            JOIN clases c ON s.id_clase = c.id_clase
            WHERE s.id_docente = @idDocente";

                if (estadoFiltro != "todos")
                    sql += " AND s.estado = @estadoFiltro";
                if (claseFiltro != "todas")
                    sql += " AND c.nombre_clase = @claseFiltro";

                sql += " ORDER BY s.fecha_hora_inicio DESC";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idDocente", idDocente.Value);
                    if (estadoFiltro != "todos") cmd.Parameters.AddWithValue("estadoFiltro", estadoFiltro);
                    if (claseFiltro != "todas") cmd.Parameters.AddWithValue("claseFiltro", claseFiltro);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            sesiones.Add(new SesionConClase
                            {
                                IdSesion = reader.GetInt32(0),
                                NombreSesion = reader.GetString(1),
                                FechaHoraInicio = reader.GetDateTime(2),
                                FechaHoraFin = reader.GetDateTime(3),
                                Descripcion = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                NombreClase = reader.GetString(5),
                                Estado = reader.GetString(6)
                            });
                        }
                }
            }

            ViewBag.ListaEstados = estados;
            ViewBag.ListaClases = clases;
            ViewBag.EstadoFiltro = estadoFiltro;
            ViewBag.ClaseFiltro = claseFiltro;

            return View(sesiones);
        }



        // GET: Página principal del docente
        public ActionResult Index()
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            ViewBag.Message = "Bienvenido, docente.";
            return View();
        }

        // GET: Listar clases del docente
        public ActionResult ClasesDocente()
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            List<Clase> clases = ObtenerClases(idDocente.Value);
            return View(clases);
        }

        // GET: Mostrar formulario para crear clase
        public ActionResult CrearClase()
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: Procesar creación de clase
        [HttpPost]
        public ActionResult CrearClase(string nombreClase, string descripcion = null)
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(nombreClase))
            {
                ModelState.AddModelError("", "El nombre de la clase es obligatorio.");
                return View();
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO clases (nombre_clase, descripcion, id_docente) 
                             VALUES (@nombreClase, @descripcion, @idDocente)
                             RETURNING id_clase";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("nombreClase", nombreClase);
                    cmd.Parameters.AddWithValue("descripcion", (object)descripcion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("idDocente", idDocente.Value);

                    int idClase = (int)cmd.ExecuteScalar();
                    TempData["Mensaje"] = $"Clase creada correctamente (ID: {idClase})";
                }
            }

            return RedirectToAction("ClasesDocente");
        }

        public ActionResult VerAlumnos(int idClase)
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            if (!ClasePerteneceADocente(idClase, idDocente.Value))
                return RedirectToAction("ClasesDocente");

            List<AlumnoClase> alumnos = new List<AlumnoClase>(); // <--- Models.AlumnoClase

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT u.id_usuario, u.nombre_completo, u.correo
                     FROM inscripciones i
                     JOIN usuarios u ON i.id_usuario = u.id_usuario
                     JOIN sesiones s ON i.id_sesion = s.id_sesion
                     WHERE s.id_clase = @idClase";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idClase", idClase);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alumnos.Add(new AlumnoClase
                            {
                                IdAlumno = reader.GetInt32(0),
                                NombreAlumno = reader.GetString(1),
                                CorreoAlumno = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            ViewBag.IdClase = idClase;
            ViewBag.NombreClase = ObtenerNombreClase(idClase);
            return View(alumnos); // ✔️ Ya devuelve Models.AlumnoClase
        }


        // GET: Mostrar formulario para agregar alumno
        public ActionResult AgregarAlumno(int idClase)
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            if (!ClasePerteneceADocente(idClase, idDocente.Value))
                return RedirectToAction("ClasesDocente");

            ViewBag.IdClase = idClase;
            ViewBag.NombreClase = ObtenerNombreClase(idClase);
            return View();
        }

        // POST: Procesar agregado de alumno
        [HttpPost]
        public ActionResult AgregarAlumno(int idClase, string correoAlumno)
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            if (!ClasePerteneceADocente(idClase, idDocente.Value))
                return RedirectToAction("ClasesDocente");

            if (string.IsNullOrWhiteSpace(correoAlumno))
            {
                ModelState.AddModelError("", "El correo del alumno es obligatorio.");
                ViewBag.IdClase = idClase;
                ViewBag.NombreClase = ObtenerNombreClase(idClase);
                return View();
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                // Buscar al alumno por correo
                int? idAlumno = null;
                string sqlBuscarAlumno = @"SELECT id_usuario FROM usuarios 
                                 WHERE correo = @correo AND rol = 'alumno'";

                using (var cmd = new NpgsqlCommand(sqlBuscarAlumno, conn))
                {
                    cmd.Parameters.AddWithValue("correo", correoAlumno);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        idAlumno = Convert.ToInt32(result);
                }

                if (!idAlumno.HasValue)
                {
                    ModelState.AddModelError("", "No se encontró un alumno con ese correo.");
                    ViewBag.IdClase = idClase;
                    ViewBag.NombreClase = ObtenerNombreClase(idClase);
                    return View();
                }

                // Obtener o crear sesión activa
                int idSesion = ObtenerSesionActivaParaClase(idClase, conn);

                // Comprobar si ya está inscrito
                string sqlExiste = @"SELECT COUNT(*) FROM inscripciones WHERE id_sesion = @idSesion AND id_usuario = @idAlumno";
                using (var cmd = new NpgsqlCommand(sqlExiste, conn))
                {
                    cmd.Parameters.AddWithValue("idSesion", idSesion);
                    cmd.Parameters.AddWithValue("idAlumno", idAlumno.Value);
                    int existe = Convert.ToInt32(cmd.ExecuteScalar());
                    if (existe > 0)
                    {
                        ModelState.AddModelError("", "Este alumno ya está inscrito en la clase.");
                        ViewBag.IdClase = idClase;
                        ViewBag.NombreClase = ObtenerNombreClase(idClase);
                        return View();
                    }
                }

                // Si no existe, inscribir
                string sqlInscribir = @"INSERT INTO inscripciones (id_sesion, id_usuario, estado) 
                                VALUES (@idSesion, @idAlumno, 'activo')";
                using (var cmd = new NpgsqlCommand(sqlInscribir, conn))
                {
                    cmd.Parameters.AddWithValue("idSesion", idSesion);
                    cmd.Parameters.AddWithValue("idAlumno", idAlumno.Value);
                    cmd.ExecuteNonQuery();
                }

                TempData["Mensaje"] = "Alumno agregado correctamente a la clase.";
            }

            return RedirectToAction("VerAlumnos", new { idClase = idClase });
        }

        // Reporte de asistencia
        public ActionResult ReporteAsistencia(int idClase)
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            if (!ClasePerteneceADocente(idClase, idDocente.Value))
                return RedirectToAction("ClasesDocente");

            List<AsistenciaAlumno> reportes = new List<AsistenciaAlumno>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                /* string sql = @"SELECT u.nombre_completo, a.fecha_registro, 
                             CASE WHEN a.estado = 'presente' THEN true ELSE false END AS presente,
                             CASE WHEN a.estado = 'tarde' THEN true ELSE false END AS tardanza,
                             0 AS tiempo_presente
                             FROM asistencia_sesion a
                             JOIN usuarios u ON a.id_usuario = u.id_usuario
                             JOIN sesiones s ON a.id_sesion = s.id_sesion
                             WHERE s.id_clase = @idClase
                             ORDER BY u.nombre_completo, a.fecha_registro";*/

               
              string sql = @" SELECT u.nombre_completo, a.fecha_sesion, 
                                     a.presente, a.tardanza, a.tiempo_presente
                              FROM asistencia_sesion a
                              JOIN usuarios u ON a.id_usuario = u.id_usuario
                              JOIN sesiones s ON a.id_sesion = s.id_sesion
                              WHERE s.id_clase = @idClase
                              ORDER BY u.nombre_completo, a.fecha_sesion";


                using (var cmd = new NpgsqlCommand(sql, conn))
              {
                  cmd.Parameters.AddWithValue("idClase", idClase);
                  using (var reader = cmd.ExecuteReader())
                  {
                      while (reader.Read())
                      {
                            /* reportes.Add(new AsistenciaAlumno
                           {
                               NombreAlumno = reader.GetString(0),
                               FechaSesion = reader.GetDateTime(1),
                               Presente = reader.GetBoolean(2),
                               Tardanza = reader.GetBoolean(3),
                               TiempoPresente = reader.GetInt32(4)
                           });*/
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

            ViewBag.NombreClase = ObtenerNombreClase(idClase);
            return View(reportes);
        }

        #region Métodos Auxiliares
        private List<Clase> ObtenerClases(int idDocente)
        {
            var clases = new List<Clase>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT id_clase, nombre_clase, descripcion 
                             FROM clases WHERE id_docente = @idDocente";

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
                                NombreClase = reader.GetString(1),
                                Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return clases;
        }

        private bool EsDocente(int idUsuario)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM usuarios WHERE id_usuario = @idUsuario AND rol = 'docente'";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idUsuario", idUsuario);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private bool ClasePerteneceADocente(int idClase, int idDocente)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM clases WHERE id_clase = @idClase AND id_docente = @idDocente";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idClase", idClase);
                    cmd.Parameters.AddWithValue("idDocente", idDocente);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private string ObtenerNombreClase(int idClase)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT nombre_clase FROM clases WHERE id_clase = @idClase";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idClase", idClase);
                    return cmd.ExecuteScalar()?.ToString() ?? "Clase Desconocida";
                }
            }
        }
        public ActionResult EditarSesion(int idSesion)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT id_sesion, nombre_sesion, fecha_hora_inicio, fecha_hora_fin, descripcion 
                       FROM sesiones WHERE id_sesion = @idSesion";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idSesion", idSesion);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var sesion = new Sesion
                            {
                                IdSesion = reader.GetInt32(0),
                                NombreSesion = reader.GetString(1),
                                FechaHoraInicio = reader.GetDateTime(2),
                                FechaHoraFin = reader.GetDateTime(3),
                                Descripcion = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            };
                            return View(sesion);
                        }
                    }
                }
            }

            return RedirectToAction("SesionesActivas");
        }

        [HttpPost]
        public ActionResult EditarSesion(Sesion sesion)
        {
            if (!ModelState.IsValid)
            {
                return View(sesion);
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"UPDATE sesiones 
                       SET nombre_sesion = @nombre, 
                           fecha_hora_inicio = @inicio, 
                           fecha_hora_fin = @fin, 
                           descripcion = @desc
                       WHERE id_sesion = @id";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("nombre", sesion.NombreSesion);
                    cmd.Parameters.AddWithValue("inicio", sesion.FechaHoraInicio);
                    cmd.Parameters.AddWithValue("fin", sesion.FechaHoraFin);
                    cmd.Parameters.AddWithValue("desc", (object)sesion.Descripcion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("id", sesion.IdSesion);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Mensaje"] = "Sesión actualizada correctamente.";
            return RedirectToAction("SesionesActivas");
        }

        public ActionResult DeshabilitarSesion(int idSesion)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE sesiones SET estado = 'inactivo' WHERE id_sesion = @idSesion";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idSesion", idSesion);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Mensaje"] = "Sesión deshabilitada.";
            return RedirectToAction("SesionesActivas");
        }

        public ActionResult SesionesPorClase(int idClase)
        {
            int? idDocente = Session["IdUsuario"] as int?;
            if (idDocente == null || !EsDocente(idDocente.Value))
                return RedirectToAction("Login", "Account");

            if (!ClasePerteneceADocente(idClase, idDocente.Value))
                return RedirectToAction("ClasesDocente");

            List<SesionConClase> sesiones = new List<SesionConClase>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT s.id_sesion, s.nombre_sesion, s.fecha_hora_inicio, s.fecha_hora_fin, s.descripcion, c.nombre_clase
                      FROM sesiones s
                      JOIN clases c ON s.id_clase = c.id_clase
                      WHERE s.id_clase = @idClase AND s.estado = 'activo'
                      ORDER BY s.fecha_hora_inicio";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("idClase", idClase);

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

            ViewBag.IdClase = idClase;
            ViewBag.NombreClase = ObtenerNombreClase(idClase);
            return View("SesionesPorClase", sesiones);
        }


        private int ObtenerSesionActivaParaClase(int idClase, NpgsqlConnection conn)
        {
            string sqlBuscarSesion = @"SELECT id_sesion FROM sesiones 
                                     WHERE id_clase = @idClase AND fecha_hora_inicio <= NOW() 
                                     AND fecha_hora_fin >= NOW() LIMIT 1";

            using (var cmd = new NpgsqlCommand(sqlBuscarSesion, conn))
            {
                cmd.Parameters.AddWithValue("idClase", idClase);
                var resultado = cmd.ExecuteScalar();
                if (resultado != null)
                    return Convert.ToInt32(resultado);
            }

            string sqlCrearSesion = @"INSERT INTO sesiones (id_docente, id_clase, nombre_sesion, 
                                    fecha_hora_inicio, fecha_hora_fin)
                                    SELECT id_docente, @idClase, 'Sesión General', 
                                           NOW(), NOW() + INTERVAL '1 year'
                                    FROM clases WHERE id_clase = @idClase
                                    RETURNING id_sesion";

            using (var cmd = new NpgsqlCommand(sqlCrearSesion, conn))
            {
                cmd.Parameters.AddWithValue("idClase", idClase);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        #endregion



        
        public class SesionConClase : Sesion
        {
            public string NombreClase { get; set; }
            public string Estado { get; set; }
        }

       

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

    }
}