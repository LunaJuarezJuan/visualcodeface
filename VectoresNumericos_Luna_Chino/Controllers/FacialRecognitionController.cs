using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using VectoresNumericos_Luna_Chino.Models;


namespace VectoresNumericos_Luna_Chino.Controllers
{
    public class FacialRecognitionController : Controller
    {
        // GET: FacialRecognition
        public ActionResult Index()
        {
            return View();
        }
        // GET: FacialRecognition/CapturaFacial
        public ActionResult CapturaFacial()
        {
            return View();
        }

        // POST: FacialRecognition/RecibirFoto
        [HttpPost]
        public JsonResult RecibirFoto(PhotoData data)
        {
            try
            {
                var base64Data = Regex.Match(data.ImageBase64, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                string tempPath = Server.MapPath("~/App_Data/temp_image.png");
                System.IO.File.WriteAllBytes(tempPath, imageBytes);

                string pythonPath = "python";
                
                string scriptPath = @"C:\Users\luna3\OneDrive\Escritorio\python\face\procesar_imagen.py";

                string argumentos = $"\"{scriptPath}\" \"{tempPath}\" \"{data.Nombre}\"";

                ProcessStartInfo start = new ProcessStartInfo()
                {
                    FileName = pythonPath,
                    Arguments = argumentos,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(start);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    return Json(new { success = false, message = $"Error en Python: {error}" });
                }

                return Json(new { success = true, message = output });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }

}