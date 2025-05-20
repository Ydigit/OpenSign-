using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using PlaceholderTextApp.Services;

namespace PlaceholderTextApp.Controllers
{
    public class CreateDocumentHmacController : Controller
    {
        private readonly IDocumentHmacSigningService _documentHmacSigningService;

        public CreateDocumentHmacController(IDocumentHmacSigningService documentHmacSigningService)
        {
            _documentHmacSigningService = documentHmacSigningService;
        }

        [HttpGet]
        public IActionResult CreateDoc()
        {
            return View("~/Views/CreateDocument/CreateDoc.cshtml");
        }

        [HttpPost]
        public IActionResult DownloadJson(IFormCollection form)
        {
            string? textoInput = form["novoInput"];
            if (string.IsNullOrWhiteSpace(textoInput))
                return BadRequest("Texto inválido.");

            var resultadoJson = _documentHmacSigningService.GerarJsonAssinaturasHmac(textoInput);
            var jsonString = JsonConvert.SerializeObject(resultadoJson, Formatting.Indented);
            var bytes = Encoding.UTF8.GetBytes(jsonString);
            var fileName = "assinaturas_hmac.json";

            return File(bytes, "application/json", fileName);
        }
    }
}
