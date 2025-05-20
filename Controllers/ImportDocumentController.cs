using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace PlaceholderTextApp.Controllers
{
    public class ImportDocumentController : Controller
    {
        [HttpGet]
        public IActionResult ImportDoc()
        {
            return View("~/Views/ImportDocument/ImportDoc.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ImportDoc(IFormFile jsonFile, IFormCollection form)
        {
            if (jsonFile != null)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var json = JsonConvert.DeserializeObject<dynamic>(await reader.ReadToEndAsync());

                ViewBag.Template = (string)json.original;
                ViewBag.Placeholders = json.placeholders.ToObject<Dictionary<string, object>>();
                ViewBag.SignedCombinations = json.signed_combinations;

                return View("~/Views/ImportDocument/ImportDoc.cshtml");
            }
            else
            {
                string template = form["template"];
                var signedCombinations = JsonConvert.DeserializeObject<List<dynamic>>(form["signedCombinations"]);

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

                string? assinatura = null;
                bool assinaturaValida = false;

                foreach (var comb in signedCombinations)
                {
                    if ((string)comb.text == textoAssinado)
                    {
                        assinatura = comb.signature;
                        assinaturaValida = true;
                        break;
                    }
                }

                var outputJson = new
                {
                    selected_text = textoCompleto,
                    signed_text = textoAssinado,
                    inputs = respostas,
                    signature = assinaturaValida ? assinatura : null
                };

                ViewBag.OutputJson = JsonConvert.SerializeObject(outputJson, Formatting.Indented);
                ViewBag.AssinaturaValida = assinaturaValida;
                ViewBag.TextoFinal = textoCompleto;
                ViewBag.TextoAssinado = textoAssinado;

                return View("~/Views/ImportDocument/ImportDoc.cshtml");
            }
        }
    }
}
