using Microsoft.AspNetCore.Mvc;
using OpenSign.Services;

namespace OpenSign.Controllers
{
    [Route("VerifyDocument")]
    public class VerifyController : Controller
    {
        private readonly VerifyDocumentService _verifyService;

        public VerifyController()
        {
            _verifyService = new VerifyDocumentService();
        }

        [HttpGet("")]
        public IActionResult VerifyView()
        {
            return View("~/Views/VerifyDocument/VerifyDocument.cshtml");
        }

        [HttpPost("VerifySignature")]
        public async Task<IActionResult> VerifySignature(IFormFile signedFile, IFormFile publicKeyFile)
        {
            if (signedFile == null || publicKeyFile == null)
            {
                TempData["Error"] = "Ficheiros inválidos. Por favor envie ambos os ficheiros .json.";
                return RedirectToAction(nameof(VerifyView));
            }

            try
            {
                using var signedStream = signedFile.OpenReadStream();
                using var signedReader = new StreamReader(signedStream);
                var signedJson = await signedReader.ReadToEndAsync();

                using var keyStream = publicKeyFile.OpenReadStream();
                using var keyReader = new StreamReader(keyStream);
                var publicKeyJson = await keyReader.ReadToEndAsync();

                // Chama o service
                bool isValid = _verifyService.ValidateSignature(signedJson, publicKeyJson);

                if (isValid)
                    TempData["Success"] = "A assinatura é válida!";
                else
                    TempData["Error"] = "Assinatura inválida.";

                return RedirectToAction(nameof(VerifyView));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erro ao verificar assinatura: {ex.Message}";
                return RedirectToAction(nameof(VerifyView));
            }
        }
    }
}

// Esperado
//
// Ficheiro assinado (signedFile.json):
/*

{
  "content": "Texto original do documento",
  "signature": "BASE64_DA_ASSINATURA"
}

*/

// Chave pública (publicKey.json):
/*

{
  "publicKey": "<RSAKeyValue><Modulus>...</Modulus><Exponent>...</Exponent></RSAKeyValue>"
}

*/