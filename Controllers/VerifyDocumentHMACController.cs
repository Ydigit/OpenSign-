using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenSign.Services;
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
            // Retorna a página da view para o utilizador carregar o JSON e a chave HMAC
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyDocumentHMAC(IFormFile jsonFile, IFormCollection form)
        {
            // Verifica se a chave HMAC está presente e válida
            if (!form.TryGetValue("hmacKey", out var hmacKeyValue) || string.IsNullOrWhiteSpace(hmacKeyValue))
            {
                TempData["Error"] = "Chave HMAC em falta.";
                return View();
            }

            string hmacKey = hmacKeyValue!;

            // Se o utilizador subiu um ficheiro JSON
            if (jsonFile != null)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var jsonText = await reader.ReadToEndAsync();

                // Verifica se o ficheiro está vazio
                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    TempData["Error"] = "Conteúdo JSON vazio.";
                    return View();
                }

                // Tenta deserializar o JSON para objeto dinâmico
                var json = JsonConvert.DeserializeObject<dynamic>(jsonText);
                if (json == null)
                {
                    TempData["Error"] = "Erro ao processar o ficheiro JSON.";
                    return View();
                }

                // Preenche a ViewBag com os dados carregados para construir os campos dinâmicos
                ViewBag.Template = (string?)json.original ?? "";
                ViewBag.Placeholders = json.placeholders?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                ViewBag.SignedCombinations = json.signed_combinations ?? new Dictionary<string, object>();
                ViewBag.HmacKey = hmacKey;

                return View(); // Volta à view para mostrar os campos preenchíveis
            }
            else
            {
                // Se os dados foram enviados via formulário (segunda etapa)
                if (!form.TryGetValue("template", out var templateValue) ||
                    !form.TryGetValue("signedCombinations", out var signedJsonValue))
                {
                    TempData["Error"] = "Dados do formulário em falta.";
                    return View();
                }

                string template = templateValue!;
                string signedJson = signedJsonValue!;

                if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(signedJson))
                {
                    TempData["Error"] = "Campos obrigatórios estão vazios.";
                    return View();
                }

                // Tenta reconverter o JSON das assinaturas
                var signedCombinations = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(signedJson);
                if (signedCombinations == null)
                {
                    TempData["Error"] = "Assinaturas inválidas.";
                    return View();
                }

                // Recolhe os valores preenchidos pelo utilizador (exceto campos internos)
                var respostas = form
                    .Where(k => k.Key != "template" && k.Key != "signedCombinations" && k.Key != "hmacKey" && !k.Key.StartsWith("__"))
                    .ToDictionary(k => k.Key, k => k.Value.ToString());

                // Monta o texto final substituindo apenas os placeholders com opções fixas
                string textoFinal = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
                {
                    var keyName = m.Groups[1].Value;
                    var hasOptions = m.Groups[2].Success;

                    // Substitui apenas se for uma opção fixa e o utilizador forneceu valor
                    if (hasOptions && respostas.ContainsKey(keyName))
                        return respostas[keyName];

                    // Caso contrário (Free Text), mantém o placeholder original (ex: [cidade])
                    return m.Value;
                });

                // Calcula o HMAC com o texto final e a chave fornecida
                string hmacHex = _hmacService.CalcularHmac(textoFinal, hmacKey);

                // Verifica se o HMAC calculado existe nas assinaturas conhecidas
                bool assinaturaValida = signedCombinations.ContainsKey(hmacHex);

                // Recupera a assinatura original, se existir
                string? assinatura = assinaturaValida ? signedCombinations[hmacHex].hmac : null;

                // Prepara o JSON de resposta para exibir na interface
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
