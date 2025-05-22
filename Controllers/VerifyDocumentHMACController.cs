using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenSign.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenSign.Controllers
{
    [Route("VerifyDocumentHMAC")]
    public class VerifyDocumentHMACController : Controller
    {
        private readonly HmacService _hmacService;

        public VerifyDocumentHMACController()
        {
            _hmacService = new HmacService();
        }

        [HttpGet("")]
        public IActionResult VerifyDocumentHMAC()
        {
            // A view está em /Views/VerifyDocumentHMAC/VerifyDocumentHMAC.cshtml
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyDocumentHMAC(IFormFile jsonFile, IFormCollection form)
        {
            string hmacKey = form["hmacKey"];

            if (jsonFile != null)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var json = JsonConvert.DeserializeObject<dynamic>(await reader.ReadToEndAsync());

                ViewBag.Template = (string)json.original;
                ViewBag.Placeholders = json.placeholders.ToObject<Dictionary<string, object>>();
                ViewBag.SignedCombinations = json.signed_combinations;
                ViewBag.HmacKey = hmacKey;

                return View();
            }
            else
            {
                string template = form["template"];
                string key = form["hmacKey"];
                var signedCombinations = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(form["signedCombinations"]);

                var respostas = form
                    .Where(k => k.Key != "template" && k.Key != "signedCombinations" && k.Key != "hmacKey" && !k.Key.StartsWith("__"))
                    .ToDictionary(k => k.Key, k => k.Value.ToString());

                string textoFinal = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
                {
                    var key = m.Groups[1].Value;
                    return respostas.ContainsKey(key) ? respostas[key] : m.Value;
                });

                string hmacHex = _hmacService.CalcularHmac(textoFinal, key);
                bool assinaturaValida = signedCombinations.ContainsKey(hmacHex);
                string? assinatura = assinaturaValida ? signedCombinations[hmacHex].hmac : null;

                var outputJson = new
                {
                    selected_text = textoFinal,
                    inputs = respostas,
                    hmac = hmacHex,
                    signature = assinatura,
                    signature_matched = assinaturaValida,
                    signature_algorithm = "HMAC-SHA256 (hex)"
                };

                ViewBag.OutputJson = JsonConvert.SerializeObject(outputJson, Formatting.Indented);
                ViewBag.AssinaturaValida = assinaturaValida;
                ViewBag.TextoFinal = textoFinal;

                return View();
            }
        }
    }
}
