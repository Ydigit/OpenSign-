using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;

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
            // Quando ambos os ficheiros são enviados, processa os conteúdos JSON
            // para carregar o modelo, placeholders, assinaturas e chave pública.
            if (jsonFile != null && keyJsonFile != null)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var json = JsonConvert.DeserializeObject<dynamic>(await reader.ReadToEndAsync());

                using var pkReader = new StreamReader(keyJsonFile.OpenReadStream());
                var pkFile = JsonConvert.DeserializeObject<dynamic>(await pkReader.ReadToEndAsync());

                ViewBag.Template = (string)json.original;
                ViewBag.Placeholders = json.placeholders.ToObject<Dictionary<string, object>>();
                ViewBag.SignedCombinations = json.signed_combinations;
                ViewBag.PublicKey = (string)pkFile.PublicKey;

                return View();
            }
            else
            {
                // Recupera os dados enviados manualmente através do formulário
                string template = form["template"];
                var signedCombinations = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(form["signedCombinations"]);
                string publicKeyBase64 = form["PublicKey"];

                // Extrai as respostas submetidas pelo utilizador, ignorando campos internos
                var respostas = form
                    .Where(k => k.Key != "template" && k.Key != "signedCombinations" && !k.Key.StartsWith("__"))
                    .ToDictionary(k => k.Key, k => k.Value.ToString());

                // Preenche todos os placeholders no template com os valores fornecidos
                string textoCompleto = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
                {
                    var key = m.Groups[1].Value;
                    return respostas.ContainsKey(key) ? respostas[key] : m.Value;
                });

                // Preenche apenas os placeholders com opções predefinidas
                string textoAssinado = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
                {
                    var key = m.Groups[1].Value;
                    var hasOptions = m.Groups[2].Success;
                    return hasOptions && respostas.ContainsKey(key) ? respostas[key] : m.Value;
                });

                // Gera o hash SHA-256 do texto assinado para validação da assinatura
                byte[] dados = Encoding.UTF8.GetBytes(textoAssinado);
                using SHA256 sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(dados);
                string hashBase64 = Convert.ToBase64String(hash);

                string? assinatura = null;
                bool assinaturaValida = signedCombinations.TryGetValue(hashBase64, out var match);

                if (assinaturaValida)
                {
                    assinatura = match.signature;

                    // Tenta importar a chave pública fornecida
                    using RSA rsa = RSA.Create();
                    byte[] pk_byte;
                    try
                    {
                        pk_byte = Convert.FromBase64String(publicKeyBase64);
                    }
                    catch (FormatException)
                    {
                        ViewBag.OutputJson = "Error: Public key with invalid Base64 format.";
                        return View();
                    }

                    try
                    {
                        rsa.ImportSubjectPublicKeyInfo(pk_byte, out _);
                    }
                    catch (CryptographicException ex)
                    {
                        ViewBag.OutputJson = "Error importing public key: ASN.1 format invalid or corrupted.";
                        ViewBag.AssinaturaValida = false;
                        ViewBag.TextoFinal = textoCompleto;
                        ViewBag.TextoAssinado = textoAssinado;
                        Console.WriteLine($"Error importing public key: {ex.Message}");
                        return View();
                    }

                    // Converte novamente o hash e a assinatura de Base64 para byte[]
                    byte[] hashByte = Convert.FromBase64String(hashBase64);

                    byte[] assinaturaByte;
                    try
                    {
                        assinaturaByte = Convert.FromBase64String(assinatura);
                    }
                    catch (FormatException)
                    {
                        ViewBag.OutputJson = "Error: Signature is not coded correctly in Base64.";
                        ViewBag.AssinaturaValida = false;
                        ViewBag.TextoFinal = textoCompleto;
                        ViewBag.TextoAssinado = textoAssinado;
                        return View();
                    }

                    // Valida a assinatura com a chave pública usando SHA256 + PKCS#1
                    bool verf;
                    try
                    {
                        verf = rsa.VerifyHash(hashByte, assinaturaByte, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        Console.WriteLine(verf);
                    }
                    catch (FormatException)
                    {
                        ViewBag.AssinaturaValida = false;
                        ViewBag.TextoFinal = textoCompleto;
                        ViewBag.TextoAssinado = textoAssinado;
                        return View();
                    }
                }

                // Constrói um objeto de resposta com os dados processados
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

                // Envia os dados para a view para visualização pelo utilizador
                ViewBag.OutputJson = JsonConvert.SerializeObject(outputJson, Formatting.Indented);
                ViewBag.Assinatura = assinatura ?? "N/A";
                ViewBag.AssinaturaValida = assinaturaValida;
                ViewBag.TextoFinal = textoCompleto;
                ViewBag.TextoAssinado = textoAssinado;

                return View();
            }
        }
    }
}
