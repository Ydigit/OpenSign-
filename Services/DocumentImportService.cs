using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenSign.Services
{
    /// @brief Service for importing and verifying signed document data.
    public class DocumentImportService
    {
        /// @brief Processes a signed document and verifies its signature using the provided public key.
        /// @param jsonFile The signed document JSON file containing original text, placeholders, and signatures.
        /// @param keyJsonFile The JSON file containing the public key used to verify the signature.
        /// @param form A fallback input method if files are not provided (used for manual form submission).
        /// @return A result object containing signature verification details and document content.
        public async Task<DocumentImportResult> ProcessAsync(IFormFile jsonFile, IFormFile keyJsonFile, IFormCollection form)
        {
            /// @brief If files are uploaded, deserialize and return base document + metadata.
            if (jsonFile != null && keyJsonFile != null)
            {
                /// @brief Read and deserialize signed document JSON.
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var json = JsonConvert.DeserializeObject<dynamic>(await reader.ReadToEndAsync());

                /// @brief Read and deserialize public key JSON.
                using var pkReader = new StreamReader(keyJsonFile.OpenReadStream());
                var pkFile = JsonConvert.DeserializeObject<dynamic>(await pkReader.ReadToEndAsync());

                /// @brief Return base result when using uploaded files.
                return new DocumentImportResult
                {
                    Template = (string)json.original,
                    Placeholders = json.placeholders.ToObject<Dictionary<string, object>>(),
                    SignedCombinations = json.signed_combinations,
                    PublicKey = (string)pkFile.PublicKey
                };
            }

            /// @brief Extract template and metadata manually from form input.
            string template = form["template"];
            var signedCombinations = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(form["signedCombinations"]);
            string publicKeyBase64 = form["PublicKey"];

            /// @brief Extract user-filled fields (ignores system fields).
            var respostas = form
                .Where(k => k.Key != "template" && k.Key != "signedCombinations" && !k.Key.StartsWith("__"))
                .ToDictionary(k => k.Key, k => k.Value.ToString());

            /// @brief Generate final document text with all placeholders filled.
            string textoCompleto = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
            {
                var key = m.Groups[1].Value;
                return respostas.ContainsKey(key) ? respostas[key] : m.Value;
            });

            /// @brief Generate text to match signature (only fixed-option placeholders filled).
            string textoAssinado = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
            {
                var key = m.Groups[1].Value;
                var hasOptions = m.Groups[2].Success;
                return hasOptions && respostas.ContainsKey(key) ? respostas[key] : m.Value;
            });

            /// @brief Hash the signed text using SHA-256.
            byte[] dados = Encoding.UTF8.GetBytes(textoAssinado);
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(dados);
            string hashBase64 = Convert.ToBase64String(hash);

            /// @brief Initialize signature variables.
            string? assinatura = null;
            bool assinaturaValida = signedCombinations.TryGetValue(hashBase64, out var match);

            /// @brief If hash matches a known signed version, verify the signature.
            if (assinaturaValida)
            {
                assinatura = match.signature;
                using RSA rsa = RSA.Create();

                try
                {
                    /// @brief Convert and import public key from Base64.
                    byte[] pkBytes = Convert.FromBase64String(publicKeyBase64);
                    rsa.ImportSubjectPublicKeyInfo(pkBytes, out _);

                    /// @brief Convert Base64 hash and signature for verification.
                    byte[] hashBytes = Convert.FromBase64String(hashBase64);
                    byte[] assinaturaBytes = Convert.FromBase64String(assinatura);

                    /// @brief Perform RSA signature verification.
                    assinaturaValida = rsa.VerifyHash(hashBytes, assinaturaBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
                catch (Exception)
                {
                    /// @brief Mark signature as invalid if any exception occurs.
                    assinaturaValida = false;
                }
            }

            /// @brief Build output object with all relevant info.
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

            /// @brief Return result object to controller/view.
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

    /// @brief Data structure returned from the import/verification process.
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