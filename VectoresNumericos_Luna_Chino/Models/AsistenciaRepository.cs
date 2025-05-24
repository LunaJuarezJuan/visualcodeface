using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace VectoresNumericos_Luna_Chino.Models
{
	public class AsistenciaRepository
	{
        private readonly string connectionString;

        public AsistenciaRepository()
        {
            connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        public int ObtenerPorcentajeAsistencia(int idUsuario)
        {
            int totalSesiones = 0;
            int sesionesPresentes = 0;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                // Contar total registros de asistencia para el usuario
                string queryTotal = "SELECT COUNT(*) FROM asistencia_sesion WHERE id_usuario = @idUsuario";
                using (var cmd = new NpgsqlCommand(queryTotal, conn))
                {
                    cmd.Parameters.AddWithValue("idUsuario", idUsuario);
                    totalSesiones = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (totalSesiones == 0)
                    return 0;

                // Contar sesiones donde estuvo presente
                string queryPresentes = "SELECT COUNT(*) FROM asistencia_sesion WHERE id_usuario = @idUsuario AND presente = true";
                using (var cmd = new NpgsqlCommand(queryPresentes, conn))
                {
                    cmd.Parameters.AddWithValue("idUsuario", idUsuario);
                    sesionesPresentes = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            double porcentaje = (double)sesionesPresentes / totalSesiones * 100;
            return (int)Math.Round(porcentaje);
        }
    }
}