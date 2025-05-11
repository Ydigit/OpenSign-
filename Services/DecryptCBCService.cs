using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenSign.Services
{
    public class DecryptCBCService
    {
        public string DecryptPrivateKeyFromJson(string jsonFilePath, string rawPassword)
        {
            try
            {
                // Read and parse JSON
                string jsonContent = File.ReadAllText(jsonFilePath);
                var jsonData = JsonSerializer.Deserialize<JsonData>(jsonContent) ??
                    throw new InvalidOperationException("Invalid JSON format");

                // Convert from Base64
                byte[] encryptedData = Convert.FromBase64String(jsonData.EncryptedSecretKey!);
                byte[] iv = Convert.FromBase64String(jsonData.Iv!);
                byte[] salt = Convert.FromBase64String(jsonData.Salt!);
                string cipherMode = jsonData.CipherMode ?? "aes-256-cbc";

                if (cipherMode.ToLower() != "aes-256-cbc")
                    throw new NotSupportedException($"Cipher mode '{cipherMode}' is not supported yet.");

                // Derive key manually (same as DerivationService, but local)
                int num_iter = 100000;
                using var rfcDerive = new Rfc2898DeriveBytes(rawPassword, salt, num_iter, HashAlgorithmName.SHA256);
                byte[] key = rfcDerive.GetBytes(32);

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

        private class JsonData
        {
            public string? EncryptedSecretKey { get; set; }
            public string? Iv { get; set; }
            public string? Salt { get; set; }
            public string? CipherMode { get; set; }
        }
    }
}
