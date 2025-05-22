using Microsoft.AspNetCore.Mvc;
using OpenSign.Services; // Supondo que KeyServiceHmac está nesse namespace
using System.IO;

namespace OpenSign.Controllers
{
    [Route("KeysHmac")]
    public class KeysHmacController : Controller
    {
        private readonly KeyServiceHmac _keyService;

        // Injeção de dependência
        public KeysHmacController(KeyServiceHmac keyService)
        {
            _keyService = keyService;
        }

        // GET: /KeysHmac
        [HttpGet("")]
        public IActionResult KeysHmac()
        {
            return View(); // View: Views/KeysHmac/KeysHmac.cshtml
        }

        // POST: /KeysHmac
        [HttpPost("")]
        public IActionResult KeysHmac(int keySize, string encmode, string pss)
        {
            // Validação do tamanho da chave HMAC
            if (keySize != 128 && keySize != 256 && keySize != 512)
            {
                TempData["Error"] = "Tamanho de chave HMAC inválido. Use 128, 256 ou 512 bits.";
                return View();
            }

            // Validação da senha
            if (string.IsNullOrEmpty(pss))
            {
                TempData["Error"] = "Senha (pss) é obrigatória.";
                return View();
            }

            // Validação do modo de cifra
            if (!encmode.Equals("aes-256-cbc", System.StringComparison.OrdinalIgnoreCase) &&
                !encmode.Equals("aes-256-ctr", System.StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Modo de cifra inválido. Use aes-256-cbc ou aes-256-ctr.";
                return View();
            }

            try
            {
                // Geração e cifragem da chave HMAC
                string jsonPath = _keyService.GenerateHmacKeyJSON(keySize, pss, encmode);

                TempData["Success"] = $"Chave HMAC gerada com sucesso ({keySize} bits, modo {encmode}).";

                // Retorna o arquivo JSON gerado
                return PhysicalFile(jsonPath, "application/json", Path.GetFileName(jsonPath));
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Erro ao gerar a chave HMAC: {ex.Message}";
                return View();
            }
        }
    }
}
