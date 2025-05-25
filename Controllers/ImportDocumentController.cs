using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenSign.Services;
using System.Text;

namespace PlaceholderTextApp.Controllers
{
    [Route("ImportDocument")]
    public class ImportDocumentController : Controller
    {
        private readonly DocumentImportService _documentImportService;

        public ImportDocumentController()
        {
            _documentImportService = new DocumentImportService();
        }

        [HttpGet("")]
        public IActionResult ImportDocument()
        {
            return View();
        }

        [HttpPost("")]
        public async Task<IActionResult> ImportDocument(IFormFile jsonFile, IFormFile keyJsonFile, IFormCollection form)
        {
            try
            {
                var result = await _documentImportService.ProcessAsync(jsonFile, keyJsonFile, form);

                ViewBag.Template = result.Template;
                ViewBag.Placeholders = result.Placeholders;
                ViewBag.SignedCombinations = result.SignedCombinations;
                ViewBag.PublicKey = result.PublicKey;
                ViewBag.OutputJson = result.OutputJson;
                ViewBag.Assinatura = result.Signature;
                ViewBag.AssinaturaValida = result.SignatureMatched;
                ViewBag.TextoFinal = result.TextoCompleto;
                ViewBag.TextoAssinado = result.TextoAssinado;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.OutputJson = "Error: " + ex.Message;
                return View();
            }
        }
    }
}