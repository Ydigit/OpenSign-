using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    public class KeysController : Controller
    {
        private readonly KeyService _keyService;

        //On this particular controller , we are using dependency injection to inject the KeyService for Generation
        public KeysController(KeyService keyService)
        {
            _keyService = keyService;
        }
        //GET action for view rendering->Generate=GET
        //ASP.Net returns a view with same name as the action method(Generate)
        public IActionResult Generate()
        {
            return View();
        }

        
        [HttpPost]//Post req only!! Send data to server from html!(**maybe encrypt**)
        //Returns a IActionResult action that can be a view or a redirect
        public IActionResult Generate(int keySize, string format)
        {
            if (keySize != 2048 && keySize != 3072 && keySize != 4096 || (format != "pem" && format != "xml"))
            {
                TempData["Error"] = "Entrada inválida.";
                return View();//Same view for the user from Generate
            }

            // Use reflection to access private method GenerateRSAKeyPair
            _keyService.GetType()//The type is an instance for KeyService
                //Grabs the methodon the class Keyservice
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
