using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenSign.Services;
using PlaceholderTextApp.Services;


namespace PlaceholderTextApp.Controllers
{
    [Route("CreateDocument")]
    public class CreateDocumentController : Controller
    {
        private readonly DocumentSigningService _documentSigningService;

        public CreateDocumentController()
        {
            _documentSigningService = new DocumentSigningService();
        }

        [HttpGet]
        public IActionResult CreateDocument()
        {
            return View();
        }

        [HttpPost("GerarEAssinarJson")]
        public async Task<IActionResult> GerarEAssinarJson(IFormCollection form, IFormFile keyFile)
        {
            string? textoInput = form["novoInput"];
            string? password = form["pss"];

            if (string.IsNullOrWhiteSpace(textoInput) || string.IsNullOrWhiteSpace(password))
                return BadRequest("Texto ou senha inválido.");

            if (keyFile == null || keyFile.Length == 0)
                return BadRequest("Arquivo de chave não fornecido.");

            try
            {
                var resultJson = await _documentSigningService.ProcessDocumentAsync(keyFile, password, textoInput);

                var jsonString = System.Text.Json.JsonSerializer.Serialize(resultJson, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var bytes = Encoding.UTF8.GetBytes(jsonString);
                var fileName = "assinaturas.json";

                TempData["Success"] = "Documento gerado com sucesso!";
                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erro: {ex.Message}";
                return RedirectToAction("CreateDocument");
            }
        }
    }
}
