using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    [Route("Import")]
    public class ImportController : Controller
    {
        // GET /Import
        [HttpGet("")]
        public IActionResult Import()
        {
            return View();
        }

        // POST /Import/Assinar
        [HttpPost("Decifrar")]
        public async Task<IActionResult> Decifrar(IFormFile keyFile, string password)
        {
            
            await Task.Delay(1000); 

            if (keyFile != null && password == "2")
            {
                ViewBag.Message = "Secret Key decifrada com sucesso!";
            }
            else
            {
                ViewBag.Message = "Erro ao assinar o arquivo. Verifique os dados!";
            }

            return View("Import");
        }
    }
}