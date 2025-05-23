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
        public async Task<IActionResult> ImportDocument(IFormFile jsonFile, IFormFile keyJsonFile,IFormCollection form)
        {
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
                string template = form["template"];
                var signedCombinations = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(form["signedCombinations"]);

                string publicKeyBase64 = form["PublicKey"];

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
                    // signed hash 
                    assinatura = match.signature;

                    // Import public key 
                    using RSA rsa = RSA.Create();
                    byte[] pk_byte = Convert.FromBase64String(publicKeyBase64);
                    rsa.ImportSubjectPublicKeyInfo(pk_byte, out _);
                    
                    // Base64 to byte[] conversion needed for verification step
                    byte[] hashByte = Convert.FromBase64String(hashBase64);
                    byte[] assinaturaByte = Convert.FromBase64String(assinatura);

                    bool verf = rsa.VerifyHash(hashByte, assinaturaByte, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);    
                    Console.WriteLine(verf);
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