using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using OpenSign.Services;

namespace OpenSignControllers
{
    [Route("CreateDocumentHMAC")]
    public class CreateDocumentHMACController : Controller
    {
        private readonly HmacService _hmacService;

        public CreateDocumentHMACController()
        {
            _hmacService = new HmacService();
        }

        [HttpGet]
        public IActionResult CreateDocumentHMAC()
        {
            return View();
        }

        [HttpPost("GerarEAssinarJson")]
        public IActionResult GerarEAssinarJson(IFormCollection form)
        {
            string? textoInput = form["novoInput"];
            string? chaveHmacInput = form["hmacKey"];

            if (string.IsNullOrWhiteSpace(textoInput) || string.IsNullOrWhiteSpace(chaveHmacInput))
                return BadRequest("Invalid text or HMAC key.");

            object resultJson;
            try
            {
                resultJson = GerarJsonAssinaturas(textoInput, chaveHmacInput);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erro generating HMAC's: {ex.Message}";
                return RedirectToAction("CreateDocumentHMAC"); // Corrigido nome da action
            }

            var jsonString = JsonConvert.SerializeObject(resultJson, Formatting.Indented);
            var bytes = Encoding.UTF8.GetBytes(jsonString);
            var fileName = "assinaturas.json";

            return File(bytes, "application/json", fileName);
        }


        private object GerarJsonAssinaturas(string texto, string chaveHmac)
        {
            var regex = new Regex(@"\[([^\]]*)\]");
            var matches = regex.Matches(texto);

            var placeholders = new Dictionary<string, object>();
            var fixedOptionValues = new List<List<string>>();
            var fixedPlaceholders = new List<string>();

            foreach (Match match in matches)
            {
                string conteudo = match.Groups[1].Value;

                if (conteudo.Contains(":"))
                {
                    var partes = conteudo.Split(":", 2);
                    string nome = partes[0].Trim();
                    var opcoes = partes[1]
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToList();

                    if (opcoes.Count > 0)
                    {
                        placeholders[nome] = opcoes;
                        fixedOptionValues.Add(opcoes);
                        fixedPlaceholders.Add(match.Value);
                    }
                    else
                    {
                        placeholders[nome] = "Free Text";
                    }
                }
                else
                {
                    string nome = conteudo.Trim();
                    placeholders[nome] = "Free Text";
                }
            }

            // Derivar o "segredo" que vai ser usado para calcular o HMAC
            byte[] salt = DerivationService.genSalt(16);
            var combinacoes = GerarCombinacoes(fixedOptionValues);
            var combinacoesAssinadas = new Dictionary<string, object>();

            foreach (var combinacao in combinacoes)
            {
            string textoFinal = texto;

            // Substitui apenas os placeholders com opções obrigatórias
            for (int i = 0; i < fixedPlaceholders.Count; i++)
            {
                textoFinal = textoFinal.Replace(fixedPlaceholders[i], combinacao[i]);
            }

            // Remove campos de texto livre como [morada]
            textoFinal = Regex.Replace(textoFinal, @"\[[^\]:\]]+\]", "");

            
            var hmacHex = _hmacService.CalcularHmac(textoFinal, chaveHmac, salt);

            combinacoesAssinadas[hmacHex] = new
            {
                text = textoFinal,
                hmac = hmacHex
            };
        }

            return new
            {
                original = texto,
                placeholders = placeholders,
                signed_combinations = combinacoesAssinadas,
                salt = Convert.ToBase64String(salt),
                signature_algorithm = "HMAC-SHA256 (hex)"
            };
        }

        private List<List<string>> GerarCombinacoes(List<List<string>> listas)
        {
            var resultado = new List<List<string>> { new List<string>() };

            foreach (var lista in listas)
            {
                var temp = new List<List<string>>();
                foreach (var prefixo in resultado)
                {
                    foreach (var item in lista)
                    {
                        var nova = new List<string>(prefixo) { item };
                        temp.Add(nova);
                    }
                }
                resultado = temp;
            }

            return resultado;
        }
    }
}
