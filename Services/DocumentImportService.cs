using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenSign.Services
{
    public class DocumentImportService
    {
        public async Task<DocumentImportResult> ProcessAsync(IFormFile jsonFile, IFormFile keyJsonFile, IFormCollection form)
        {
            if (jsonFile != null && keyJsonFile != null)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var json = JsonConvert.DeserializeObject<dynamic>(await reader.ReadToEndAsync());

                using var pkReader = new StreamReader(keyJsonFile.OpenReadStream());
                var pkFile = JsonConvert.DeserializeObject<dynamic>(await pkReader.ReadToEndAsync());

                return new DocumentImportResult
                {
                    Template = (string)json.original,
                    Placeholders = json.placeholders.ToObject<Dictionary<string, object>>(),
                    SignedCombinations = json.signed_combinations,
                    PublicKey = (string)pkFile.PublicKey
                };
            }

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
                assinatura = match.signature;
                using RSA rsa = RSA.Create();

                try
                {
                    byte[] pkBytes = Convert.FromBase64String(publicKeyBase64);
                    rsa.ImportSubjectPublicKeyInfo(pkBytes, out _);

                    byte[] hashBytes = Convert.FromBase64String(hashBase64);
                    byte[] assinaturaBytes = Convert.FromBase64String(assinatura);

                    assinaturaValida = rsa.VerifyHash(hashBytes, assinaturaBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
                catch (Exception)
                {
                    assinaturaValida = false;
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

            return new DocumentImportResult
            {
                OutputJson = JsonConvert.SerializeObject(outputJson, Formatting.Indented),
                Signature = assinatura ?? "N/A",
                SignatureMatched = assinaturaValida,
                TextoCompleto = textoCompleto,
                TextoAssinado = textoAssinado
            };
        }
    }

    public class DocumentImportResult
    {
        public string? Template { get; set; }
        public Dictionary<string, object>? Placeholders { get; set; }
        public dynamic? SignedCombinations { get; set; }
        public string? PublicKey { get; set; }
        public string? OutputJson { get; set; }
        public string? Signature { get; set; }
        public bool SignatureMatched { get; set; }
        public string? TextoCompleto { get; set; }
        public string? TextoAssinado { get; set; }
    }
}
