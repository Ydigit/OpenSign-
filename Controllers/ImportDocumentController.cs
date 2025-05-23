using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

// ImportDocument
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
        public async Task<IActionResult> ImportDocument(IFormFile jsonFile, IFormFile keyJsonFile, IFormCollection form)
        {
            if (jsonFile != null && keyJsonFile != null)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var json = JsonConvert.DeserializeObject<dynamic>(await reader.ReadToEndAsync());

                using var pkReader = new StreamReader(keyJsonFile.OpenReadStream());
                var pkFile = JsonConvert.DeserializeObject<dynamic>(await pkReader.ReadToEndAsync());
                Console.WriteLine((string)pkFile.PublicKey);

                ViewBag.Template = (string)json.original;
                ViewBag.Placeholders = json.placeholders.ToObject<Dictionary<string, object>>();
                ViewBag.SignedCombinations = json.signed_combinations;
                ViewBag.PublicKey = (string)pkFile.PublicKey;

                return View();
            }
            else
            {
                string template = form["template"];
                var signedCombinations = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(form["signedCombinations"]);
                string publicKeyRaw = form["publicKey"];

                if (string.IsNullOrEmpty(publicKeyRaw))
                {
                    Console.WriteLine("Erro: Chave pública não fornecida.");
                    ViewBag.OutputJson = "Erro: Chave pública não fornecida.";
                    return View();
                }

                var respostas = form
                    .Where(k => k.Key != "template" && k.Key != "signedCombinations" && k.Key != "publicKey" && !k.Key.StartsWith("__"))
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

                    bool verf = VerificarAssinatura(publicKeyRaw, textoAssinado, assinatura, out string verifiedHash);

                    if (verf)
                    {
                        Console.WriteLine("Assinatura válida.");
                    }
                    else
                    {
                        Console.WriteLine("Assinatura inválida.");
                    }
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

        private bool VerificarAssinatura(string publicKeyRaw, string texto, string assinaturaBase64, out string hashBase64)
        {
            byte[] pk;
            try
            {
                string cleanedKey = Regex.Replace(publicKeyRaw, "-{5}[ A-Z]+-{5}", "", RegexOptions.Multiline)
                                        .Replace("\n", "")
                                        .Replace("\r", "")
                                        .Trim();
                pk = Convert.FromBase64String(cleanedKey);
            }
            catch
            {
                hashBase64 = string.Empty;
                return false;
            }

            byte[] dados = Encoding.UTF8.GetBytes(texto);
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(dados);
            hashBase64 = Convert.ToBase64String(hash);

            try
            {
                using RSA rsa = RSA.Create();
                try
                {
                    rsa.ImportSubjectPublicKeyInfo(pk, out _);
                }
                catch
                {
                    rsa.ImportRSAPublicKey(pk, out _);
                }

                byte[] assinatura = Convert.FromBase64String(assinaturaBase64);
                return rsa.VerifyHash(hash, assinatura, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }
    }
}
