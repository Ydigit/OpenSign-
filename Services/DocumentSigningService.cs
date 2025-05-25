using OpenSign.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace PlaceholderTextApp.Services
{
    /// @brief Service responsible for generating RSA-signed JSON documents.
    public class DocumentSigningService
    {
        private readonly DecryptCBCService _decryptServiceCBC;
        private readonly DecryptionCTRService _decryptServiceCTR;

        /// @brief Initializes the signing service and its decryption helpers.
        public DocumentSigningService()
        {
            _decryptServiceCBC = new DecryptCBCService();
            _decryptServiceCTR = new DecryptionCTRService();
        }

        /// @brief Main entry point to process document signing.
        /// @param keyFile The encrypted private key file.
        /// @param password The password to decrypt the key.
        /// @param inputText The document text containing placeholders.
        /// @return A JSON object with placeholder definitions and digital signatures.
        public async Task<object> ProcessDocumentAsync(IFormFile keyFile, string password, string inputText)
        {
            /// @brief Decrypt the private key.
            var rsa = await DecryptPrivateKeyAsync(keyFile, password);

            /// @brief Generate the signed JSON.
            return GenerateSignedJson(inputText, rsa);
        }

        /// @brief Decrypts the private RSA key using the specified cipher mode.
        /// @param keyFile The uploaded encrypted key file.
        /// @param password The password used to decrypt the file.
        /// @return A usable RSA instance loaded with the private key.
        private async Task<RSA> DecryptPrivateKeyAsync(IFormFile keyFile, string password)
        {
            var tempFilePath = Path.GetTempFileName();

            using (var stream = File.Create(tempFilePath))
            {
                await keyFile.CopyToAsync(stream);
            }

            string jsonContent = await File.ReadAllTextAsync(tempFilePath);
            var keyData = JsonSerializer.Deserialize<KeyDataModel>(jsonContent);

            if (keyData == null || string.IsNullOrEmpty(keyData.CipherMode))
                throw new Exception("The JSON file is malformed or missing the cipher mode.");

            string decryptedPrivateKey;

            if (keyData.CipherMode == "aes-256-cbc")
            {
                decryptedPrivateKey = _decryptServiceCBC.DecryptPrivateKeyFromJson(tempFilePath, password);
            }
            else if (keyData.CipherMode == "aes-256-ctr")
            {
                decryptedPrivateKey = _decryptServiceCTR.DecryptPrivateKeyFromJson(tempFilePath, password);
            }
            else
            {
                throw new Exception("Unknown cipher mode specified in the JSON file.");
            }

            File.Delete(tempFilePath);

            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(decryptedPrivateKey), out _);
            return rsa;
        }

        /// @brief Generates JSON with all possible placeholder combinations signed using the provided RSA key.
        /// @param text The document text containing placeholders.
        /// @param rsa The private RSA key to sign the combinations.
        /// @return An object containing signed combinations, placeholder metadata, and the original text.
        private object GenerateSignedJson(string text, RSA rsa)
        {
            var regex = new Regex(@"\[([^\]]*)\]");
            var matches = regex.Matches(text);

            int maxPlaceholders = 7;
            if (matches.Count > maxPlaceholders)
                throw new Exception($"The text exceeds the limit of {maxPlaceholders} placeholders.");

            var placeholders = new Dictionary<string, object>();
            var fixedOptionValues = new List<List<string>>();
            var fixedPlaceholders = new List<string>();

            /// @brief Extract and classify placeholders: fixed-option or free text.
            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;

                /// @brief If placeholder contains options (e.g., [role:admin,user]).
                if (content.Contains(":"))
                {
                    var parts = content.Split(":", 2);
                    string name = parts[0].Trim();

                    var options = parts[1]
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToList();

                    /// @brief Enforce a maximum of 3 options per placeholder.
                    if (options.Count > 3)
                        throw new Exception($"The placeholder '{name}' exceeds the limit of 3 options.");

                    /// @brief Register placeholder options or fallback to free text.
                    placeholders[name] = options.Count > 0 ? options : "Free Text";

                    /// @brief Store placeholders with fixed options for combinations.
                    if (options.Count > 0)
                    {
                        fixedOptionValues.Add(options);
                        fixedPlaceholders.Add(match.Value);
                    }
                }
                else
                {
                    /// @brief If no options, treat as free-text placeholder.
                    string name = content.Trim();
                    placeholders[name] = "Free Text";
                }
            }

            /// @brief Generate all valid combinations of fixed options.
            var combinations = GenerateCombinations(fixedOptionValues);
            var signedCombinations = new Dictionary<string, object>();

            /// @brief Sign each generated text variation using the RSA private key.
            foreach (var combo in combinations)
            {
                string finalText = text;

                /// @brief Replace each fixed placeholder with the selected option.
                for (int i = 0; i < fixedPlaceholders.Count; i++)
                {
                    finalText = finalText.Replace(fixedPlaceholders[i], combo[i]);
                }

                /// @brief Hash the final text using SHA256.
                byte[] data = Encoding.UTF8.GetBytes(finalText);
                byte[] hash = SHA256.HashData(data);
                string hashBase64 = Convert.ToBase64String(hash);

                /// @brief Sign the hash with the RSA private key.
                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                string signatureBase64 = Convert.ToBase64String(signature);

                /// @brief Store the signed combination keyed by its hash.
                signedCombinations[hashBase64] = new
                {
                    text = finalText,
                    signature = signatureBase64,
                    hash = hashBase64
                };
            }

            /// @brief Return the final result structure.
            return new
            {
                original = text,
                placeholders = placeholders,
                signed_combinations = signedCombinations,
                signature_algorithm = "RSA"
            };
        }

        /// @brief Generates all possible combinations from a list of option lists.
        /// @param lists The list of value lists for each placeholder.
        /// @return A list of combinations, each as a list of strings.
        private List<List<string>> GenerateCombinations(List<List<string>> lists)
        {
            /// @brief Initialize with a single empty combination.
            var result = new List<List<string>> { new List<string>() };

            /// @brief Build combinations iteratively.
            foreach (var list in lists)
            {
                var temp = new List<List<string>>();

                foreach (var prefix in result)
                {
                    foreach (var item in list)
                    {
                        /// @brief Clone existing prefix and append new option.
                        var newCombo = new List<string>(prefix) { item };
                        temp.Add(newCombo);
                    }
                }

                /// @brief Update result with the new expanded combinations.
                result = temp;
            }

            /// @brief Return the complete set of combinations.
            return result;
        }
    }
}
