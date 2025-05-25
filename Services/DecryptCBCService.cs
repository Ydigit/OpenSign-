using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenSign.Services
{
    /// @brief Service responsible for decrypting AES-256-CBC encrypted private keys from a JSON file.
    ///
    /// This class reads a JSON file containing a base64-encoded encrypted private key, IV, salt, and cipher mode.
    /// It derives a symmetric key using the user's password and decrypts the private key using AES-256-CBC.
    public class DecryptCBCService
    {
        /// @brief Decrypts a private key from a JSON file using AES-256-CBC.
        ///
        /// @param jsonFilePath Path to the JSON file with the encrypted key and metadata.
        /// @param rawPassword Password provided by the user to derive the AES key.
        /// @return Decrypted private key string (PEM format).
        ///
        /// @exception InvalidOperationException If JSON format is invalid or incomplete.
        /// @exception NotSupportedException If the cipher mode is unsupported.
        /// @exception ArgumentException If derived key is not 256 bits (32 bytes).
        /// @exception CryptographicException If the password is wrong or data is corrupted.
        /// @exception Exception For general errors during the decryption process.
        public string DecryptPrivateKeyFromJson(string jsonFilePath, string rawPassword)
        {
            try
            {
                /// @brief Read the encrypted JSON content from file
                string jsonContent = File.ReadAllText(jsonFilePath);

                /// @brief Deserialize JSON into object with required fields
                var jsonData = JsonSerializer.Deserialize<JsonData>(jsonContent) ??
                    throw new InvalidOperationException("Invalid JSON format");

                /// @brief Extract the necessary values from JSON
                byte[] encryptedData = Convert.FromBase64String(jsonData.EncryptedSecretKey!);
                byte[] iv = Convert.FromBase64String(jsonData.Iv!);
                byte[] salt = Convert.FromBase64String(jsonData.Salt!);
                string cipherMode = jsonData.CipherMode ?? "aes-256-cbc";

                /// @brief Verify the cipher mode is supported
                if (cipherMode.ToLower() != "aes-256-cbc")
                    throw new NotSupportedException($"Cipher mode '{cipherMode}' is not supported yet.");

                /// @brief Derive AES key from password and salt
                byte[] key = DerivationService.DeriveKey(rawPassword, salt);

                /// @brief Ensure the derived key is 256 bits
                if (key.Length != 32)
                    throw new ArgumentException($"Invalid key size. Expected 32 bytes, got {key.Length}");

                /// @brief Create AES instance with CBC mode and PKCS7 padding
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    /// @brief Create decryptor and stream to process encrypted data
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        /// @brief Read and return the decrypted PEM-formatted private key
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                /// @brief Likely due to incorrect password or corrupted input
                throw new CryptographicException("Decryption failed - likely wrong password", ex);
            }
            catch (Exception ex)
            {
                /// @brief Fallback for unexpected errors
                throw new Exception("Decryption error", ex);
            }
        }

        /// @brief Helper class representing the structure of the JSON input for decryption.
        private class JsonData
        {
            /// @brief Base64-encoded encrypted private key.
            public string? EncryptedSecretKey { get; set; }

            /// @brief Base64-encoded Initialization Vector (IV).
            public string? Iv { get; set; }

            /// @brief Base64-encoded salt used for key derivation.
            public string? Salt { get; set; }

            /// @brief Cipher mode used for encryption (expected: "aes-256-cbc").
            public string? CipherMode { get; set; }
        }
    }
}
