using OpenSign.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace PlaceholderTextApp.Services
{
    public class DocumentSigningService
    {
        private readonly DecryptCBCService _decryptServiceCBC;
        private readonly DecryptionCTRService _decryptServiceCTR;

        public DocumentSigningService()
        {
            _decryptServiceCBC = new DecryptCBCService();
            _decryptServiceCTR = new DecryptionCTRService();
        }

        public async Task<object> ProcessDocumentAsync(IFormFile keyFile, string password, string inputText)
        {
            var rsa = await DecryptPrivateKeyAsync(keyFile, password);
            return GenerateSignedJson(inputText, rsa);
        }

        private async Task<RSA> DecryptPrivateKeyAsync(IFormFile keyFile, string password)
        {
            var tempFilePath = Path.GetTempFileName();

            using (var stream = System.IO.File.Create(tempFilePath))
            {
                await keyFile.CopyToAsync(stream);
            }

            string jsonContent = await File.ReadAllTextAsync(tempFilePath);
            var keyData = JsonSerializer.Deserialize<KeyDataModel>(jsonContent);
            if (keyData == null || string.IsNullOrEmpty(keyData.CipherMode))
                throw new Exception("O arquivo JSON está malformado ou não contém o modo de cifração.");

            string decryptedPrivateKey = keyData.CipherMode switch
            {
                "aes-256-cbc" => _decryptServiceCBC.DecryptPrivateKeyFromJson(tempFilePath, password),
                "aes-256-ctr" => _decryptServiceCTR.DecryptPrivateKeyFromJson(tempFilePath, password),
                _ => throw new Exception("Modo de cifração desconhecido no arquivo JSON.")
            };

            File.Delete(tempFilePath);

            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(decryptedPrivateKey), out _);
            return rsa;
        }

        private object GenerateSignedJson(string text, RSA rsa)
        {
            var regex = new Regex(@"\[([^\]]*)\]");
            var matches = regex.Matches(text);

            int maxPlaceholders = 7;
            if (matches.Count > maxPlaceholders)
                throw new Exception($"O texto excede o limite de {maxPlaceholders} placeholders.");

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
                    var opcoes = partes[1].Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

                    if (opcoes.Count > 3)
                        throw new Exception($"O placeholder '{nome}' excede o limite de 3 opções.");

                    placeholders[nome] = opcoes.Count > 0 ? opcoes : "Free Text";
                    if (opcoes.Count > 0)
                    {
                        fixedOptionValues.Add(opcoes);
                        fixedPlaceholders.Add(match.Value);
                    }
                }
                else
                {
                    string nome = conteudo.Trim();
                    placeholders[nome] = "Free Text";
                }
            }

            var combinations = GenerateCombinations(fixedOptionValues);
            var signedCombinations = new Dictionary<string, object>();

            foreach (var combo in combinations)
            {
                string finalText = text;
                for (int i = 0; i < fixedPlaceholders.Count; i++)
                    finalText = finalText.Replace(fixedPlaceholders[i], combo[i]);

                byte[] data = Encoding.UTF8.GetBytes(finalText);
                byte[] hash = SHA256.HashData(data);
                string hashBase64 = Convert.ToBase64String(hash);

                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                string signatureBase64 = Convert.ToBase64String(signature);

                signedCombinations[hashBase64] = new
                {
                    text = finalText,
                    signature = signatureBase64,
                    hash = hashBase64
                };
            }

            return new
            {
                original = text,
                placeholders = placeholders,
                signed_combinations = signedCombinations,
                signature_algorithm = "RSA"
            };
        }

        private List<List<string>> GenerateCombinations(List<List<string>> lists)
        {
            var result = new List<List<string>> { new List<string>() };
            foreach (var list in lists)
            {
                var temp = new List<List<string>>();
                foreach (var prefix in result)
                {
                    foreach (var item in list)
                    {
                        var newCombo = new List<string>(prefix) { item };
                        temp.Add(newCombo);
                    }
                }
                result = temp;
            }
            return result;
        }
    }
}
