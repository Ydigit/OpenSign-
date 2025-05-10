//Aqui serve so para gerar e cifrar


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
        //Adicionar os parametros de cifra simetrica
        public IActionResult Generate(int keySize, string encmode, string pss){
            if (keySize != 2048 && keySize != 3072 && keySize != 4096 )
            {
                TempData["Error"] = "Tamanho de pk invalido.";
                return View();
            }
            //cmode e chill
            //pass
            //if (keySize != 2048 && keySize != 3072 && keySize != 4096 || )
            //{
            //    TempData["Error"] = "Password is too weak.";
            //    return View();
            //}

            // Use reflection to access private method GenerateRSAKeyPair
            _keyService.GetType()
                .GetMethod("GenerateRSAKeyPair", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(_keyService, new object[] { keySize, encmode });

            var keys = _keyService.GetCurrentKeys();
            ViewBag.PublicKey = keys.GetType().GetProperty("PublicKey")?.GetValue(keys);
            ViewBag.PrivateKey = keys.GetType().GetProperty("PrivateKey")?.GetValue(keys);

            TempData["Success"] = $"Chaves {encmode.ToUpper()} de {keySize} bits geradas com sucesso.";
            return View();
        }
    }
}
