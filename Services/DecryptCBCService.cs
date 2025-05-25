using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenSign.Services
{
    /// <summary>
    /// Serviço responsável por decifrar uma chave privada usando o modo AES-256-CBC.
    /// </summary>
    public class DecryptCBCService
    {
        /// <summary>
        /// Decifra uma chave privada a partir de um arquivo JSON que contem os dados necessários.
        /// </summary>
        /// <param name="jsonFilePath">Caminho para o arquivo JSON que contem os dados criptografados.</param>
        /// <param name="rawPassword">Senha fornecida pelo usuário.</param>
        /// <returns>Chave privada decifrada em formato PEM.</returns>
        /// <exception cref="InvalidOperationException">Se o formato do JSON for inválido.</exception>
        /// <exception cref="NotSupportedException">Se o modo de cifra não for suportado.</exception>
        /// <exception cref="ArgumentException">Se o tamanho da chave derivada for inválido.</exception>
        /// <exception cref="CryptographicException">Se ocorrer uma falha na decifração (ex. senha incorreta).</exception>
        /// <exception cref="Exception">Para outros erros não tratados.</exception>
        public string DecryptPrivateKeyFromJson(string jsonFilePath, string rawPassword)
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                var jsonData = JsonSerializer.Deserialize<JsonData>(jsonContent) ??
                    throw new InvalidOperationException("Invalid JSON format");

                byte[] encryptedData = Convert.FromBase64String(jsonData.EncryptedSecretKey!);
                byte[] iv = Convert.FromBase64String(jsonData.Iv!);
                byte[] salt = Convert.FromBase64String(jsonData.Salt!);
                string cipherMode = jsonData.CipherMode ?? "aes-256-cbc";

                if (cipherMode.ToLower() != "aes-256-cbc")
                    throw new NotSupportedException($"Cipher mode '{cipherMode}' is not supported yet.");

                byte[] key = DerivationService.DeriveKey(rawPassword, salt);

                if (key.Length != 32)
                    throw new ArgumentException($"Invalid key size. Expected 32 bytes, got {key.Length}");

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Decryption failed - likely wrong password", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Decryption error", ex);
            }
        }

        /// <summary>
        /// Representa os dados esperados no arquivo JSON para o modo CBC.
        /// </summary>
        private class JsonData
        {
            /// <summary>Chave secreta criptografada em Base64.</summary>
            public string? EncryptedSecretKey { get; set; }

            /// <summary>Vetor de inicialização (IV) em Base64.</summary>
            public string? Iv { get; set; }

            /// <summary>Salt utilizado na derivação da chave, em Base64.</summary>
            public string? Salt { get; set; }

            /// <summary>Modo de cifra (ex: aes-256-cbc).</summary>
            public string? CipherMode { get; set; }
        }
    }
}
