using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    [Route("Generate")]
    public class KeysController : Controller
    {
        private readonly KeyService _keyService;

        // Construtor para injeção de dependência
        public KeysController(KeyService keyService)
        {
            _keyService = keyService;
        }

        // GET: /Generate
        [HttpGet("")]
        public IActionResult Generate()
        {
            return View();
        }

        // POST: /Generate
        //trigger quando em /Generate chamo o metodo de post
        [HttpPost("")]
        public IActionResult Generate(int keySize, string format) //Recebe 2 parametros dos formularios
        {
            if (keySize != 2048 && keySize != 3072 && keySize != 4096 || (format != "pem" && format != "xml"))
            {
                TempData["Error"] = "Entrada inválida.";
                return View();
            }

            // Use reflection to access private method GenerateRSAKeyPair
            _keyService.GetType()
                .GetMethod("GenerateRSAKeyPair", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(_keyService, new object[] { keySize, format });

            var keys = _keyService.GetCurrentKeys();
            ViewBag.PublicKey = keys.GetType().GetProperty("PublicKey")?.GetValue(keys);
            ViewBag.PrivateKey = keys.GetType().GetProperty("PrivateKey")?.GetValue(keys);

            TempData["Success"] = $"Chaves {format.ToUpper()} de {keySize} bits geradas com sucesso.";
            return View();
        }
    }
}
