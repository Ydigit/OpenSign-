using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;

//ImportDoument
namespace PlaceholderTextApp.Controllers
{
    [Route("ImportDocument")]
    public class ImportDocumentController : Controller
    {
        [HttpGet("")]
        public IActionResult ImportDocument()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportDocument(IFormFile jsonFile, IFormCollection form)
        {
            if (jsonFile != null)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var json = JsonConvert.DeserializeObject<dynamic>(await reader.ReadToEndAsync());

                ViewBag.Template = (string)json.original;
                ViewBag.Placeholders = json.placeholders.ToObject<Dictionary<string, object>>();
                ViewBag.SignedCombinations = json.signed_combinations;

                return View();
            }
            else
            {
                string template = form["template"];
                var signedCombinations = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(form["signedCombinations"]);

                var respostas = form
                    .Where(k => k.Key != "template" && k.Key != "signedCombinations" && !k.Key.StartsWith("__"))
                    .ToDictionary(k => k.Key, k => k.Value.ToString());

                string textoCompleto = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
                {
                    var key = m.Groups[1].Value;
                    return respostas.ContainsKey(key) ? respostas[key] : m.Value;
                });

                string textoAssinado = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
                {
                    var key = m.Groups[1].Value;
                    var hasOptions = m.Groups[2].Success;
                    return hasOptions && respostas.ContainsKey(key) ? respostas[key] : m.Value;
                });

                byte[] dados = Encoding.UTF8.GetBytes(textoAssinado);
                using SHA256 sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(dados);
                string hashBase64 = Convert.ToBase64String(hash);

                string? assinatura = null;
                bool assinaturaValida = signedCombinations.TryGetValue(hashBase64, out var match);

                if (assinaturaValida)
                {
                    assinatura = match.signature;
                }

                var outputJson = new
                {
                    selected_text = textoCompleto,
                    signed_text = textoAssinado,
                    inputs = respostas,
                    hash = hashBase64,
                    signature = assinatura,
                    signature_matched = assinaturaValida,
                    signature_algorithm = "RSA"
                };

                ViewBag.OutputJson = JsonConvert.SerializeObject(outputJson, Formatting.Indented);
                ViewBag.AssinaturaValida = assinaturaValida;
                ViewBag.TextoFinal = textoCompleto;
                ViewBag.TextoAssinado = textoAssinado;

                return View();
            }
        }
    }
}