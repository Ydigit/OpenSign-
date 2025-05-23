//Aqui serve so para gerar e cifrar

using System.IO.Compression;
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
        public IActionResult Generate(int keySize, string encmode, string pss, string keys){
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

            //string jsonPath = _keyService.GenerateRSAKeyPairJSON(keySize, pss, encmode);
            var jsonPath = _keyService.GenerateRSAKeyPairJSON(keySize, pss, encmode);

            // cria um arquivo .zip com 1 file json com a private key e outro com a public key
            if(keys.Equals("genKeys")){
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)){
                        archive.CreateEntryFromFile(jsonPath.jsonfilePath, Path.GetFileName(jsonPath.jsonfilePath));
                        archive.CreateEntryFromFile(jsonPath.pubfilePath, Path.GetFileName(jsonPath.pubfilePath));
                    }

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return File(memoryStream.ToArray(), "application/zip", "rsa_keypair.zip");
                }
                //return PhysicalFile(jsonPath.jsonfilePath, "application/json", Path.GetFileName(jsonPath.jsonfilePath));
            }
            //if(keys.Equals("pub"))
                //return PhysicalFile(jsonPath.pubfilePath, "application/json", Path.GetFileName(jsonPath.pubfilePath));

            TempData["Success"] = $"Chaves {encmode.ToUpper()} de {keySize} bits geradas com sucesso.";
            return View();
        }
    }
}
