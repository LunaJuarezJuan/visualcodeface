using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using VectoresNumericos_Luna_Chino.Models;

namespace VectoresNumericos_Luna_Chino.Controllers
{
    public class FacialRecognitionApiController : Controller
    {
        public ActionResult Captura()
        {
            return View();
        }

        [HttpPost]
        public JsonResult RegistrarUsuarioMultiplesFotos(UserPhotoData data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.Nombre) || string.IsNullOrEmpty(data.Correo)
                    || string.IsNullOrEmpty(data.Contrasena) || data.FotosBase64 == null || data.FotosBase64.Count == 0)
                {
                    return Json(new { success = false, message = "Todos los campos y al menos una foto son obligatorios." });
                }

                string pythonPath = "python";
                string scriptPath = @"C:\Users\luna3\OneDrive\Escritorio\python\face\api.py";

                string tempFolder = Server.MapPath("~/App_Data/TempFotos");
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                List<string> outputs = new List<string>();

                foreach (var base64Image in data.FotosBase64)
                {
                    var match = Regex.Match(base64Image, @"data:image/(?<type>.+?),(?<data>.+)");
                    if (!match.Success)
                    {
                        throw new Exception("Formato de alguna imagen no válido.");
                    }

                    var base64Data = match.Groups["data"].Value;
                    byte[] imageBytes = Convert.FromBase64String(base64Data);

                    string tempFile = Path.Combine(tempFolder, Guid.NewGuid().ToString() + ".png");
                    System.IO.File.WriteAllBytes(tempFile, imageBytes);

                    string argumentos = $"\"{scriptPath}\" \"{tempFile}\" \"{data.Nombre}\" \"{data.Correo}\" \"{data.Contrasena}\"";

                    ProcessStartInfo start = new ProcessStartInfo()
                    {
                        FileName = pythonPath,
                        Arguments = argumentos,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(start))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (!string.IsNullOrEmpty(error))
                        {
                            throw new Exception("Error Python: " + error);
                        }
                        outputs.Add(output);
                    }
                }

                return Json(new { success = true, message = "Fotos procesadas correctamente:\n" + string.Join("\n", outputs) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500; // Para indicar error en HTTP
                return Json(new { success = false, message = "Error servidor: " + ex.Message });
            }
        }
    }
}