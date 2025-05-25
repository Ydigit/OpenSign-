using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;
using OpenSign.Services;

namespace OpenSignControllers
{
    [Route("CreateDocumentHMAC")]
    public class CreateDocumentHMACController : Controller
    {
        private readonly DocumentSigningServiceHmac _documentSigningServiceHmac;

        public CreateDocumentHMACController()
        {
            _documentSigningServiceHmac = new DocumentSigningServiceHmac();
        }

        [HttpGet]
        public IActionResult CreateDocumentHMAC()
        {
            return View();
        }

        [HttpPost("GerarEAssinarJson")]
        public IActionResult GerarEAssinarJson(IFormCollection form)
        {
            string? textoInput = form["novoInput"];
            string? chaveHmacInput = form["hmacKey"];

            if (string.IsNullOrWhiteSpace(textoInput) || string.IsNullOrWhiteSpace(chaveHmacInput))
                return BadRequest("Texto ou chave HMAC inválida.");

            try
            {
                var resultJson = _documentSigningServiceHmac.GenerateHmacSignedJson(textoInput, chaveHmacInput);

                var jsonString = JsonConvert.SerializeObject(resultJson, Formatting.Indented);
                var bytes = Encoding.UTF8.GetBytes(jsonString);
                var fileName = "assinaturas.json";

                TempData["Success"] = "Documento gerado com sucesso!";
                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erro ao gerar HMACs: {ex.Message}";
                return RedirectToAction("CreateDocumentHMAC");
            }
        }
    }
}
