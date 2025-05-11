using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using OpenSign.Services;

namespace OpenSign.Controllers
{
    [Route("Import")]
    public class ImportController : Controller
    {
        private readonly DecryptCBCService _decryptService;

        public ImportController()
        {
            _decryptService = new DecryptCBCService();
        }

        // GET /Import
        [HttpGet("")]
        public IActionResult Import()
        {
            return View();
        }

        // POST /Import/Decifrar
        [HttpPost("Decifrar")]
        public async Task<IActionResult> Decifrar(IFormFile keyFile, string pss)
        {
            if (keyFile == null || string.IsNullOrEmpty(pss))
            {
                ViewBag.Message = "Erro: Ficheiro ou password inválidos!";
                return View("Import");
            }

            try
            {
                // Salvar o arquivo temporariamente
                var tempFilePath = Path.GetTempFileName();
                using (var stream = System.IO.File.Create(tempFilePath))
                {
                    await keyFile.CopyToAsync(stream);
                }

                // Decifrar a chave privada
                string decryptedPrivateKey = _decryptService.DecryptPrivateKeyFromJson(tempFilePath, pss);

                // Limpar o arquivo temporário
                System.IO.File.Delete(tempFilePath);

                ViewBag.Message = "Secret Key decifrada com sucesso!";
                ViewBag.DecryptedPrivateKey = decryptedPrivateKey;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Erro ao decifrar: {ex.Message}";
            }

            return View("Import");
        }
    }
}