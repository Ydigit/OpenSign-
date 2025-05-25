using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenSign.Services
{
    /**
     * \class DecryptCBCService
     * \brief Service that handles decryption of an AES-256-CBC encrypted private key stored in a JSON file.
     *
     * This service reads a JSON file containing an encrypted private key, initialization vector (IV), and salt.
     * It uses the AES-256-CBC algorithm with PKCS7 padding and a derived key from the user-provided password.
     */
    public class DecryptCBCService
    {
        /**
         * \brief Decrypts a private key from a JSON file using AES-256-CBC.
         * 
         * \param jsonFilePath The path to the JSON file containing encryption metadata and the encrypted key.
         * \param rawPassword The password input by the user, which is used to derive the decryption key.
         * \return A string containing the decrypted private key (in PEM format).
         *
         * \throws InvalidOperationException if the JSON format is invalid.
         * \throws NotSupportedException if the cipher mode is unsupported.
         * \throws ArgumentException if the derived key length is incorrect.
         * \throws CryptographicException if decryption fails (e.g., due to a wrong password).
         * \throws Exception for any general error during the process.
         */
        public string DecryptPrivateKeyFromJson(string jsonFilePath, string rawPassword)
        {
            try
            {
                // Read the encrypted key content from the file
                string jsonContent = File.ReadAllText(jsonFilePath);

                // Parse the content into a JsonData object
                var jsonData = JsonSerializer.Deserialize<JsonData>(jsonContent) ??
                    throw new InvalidOperationException("Invalid JSON format");

                // Extract the encrypted key, IV, salt, and cipher mode from the JSON
                byte[] encryptedData = Convert.FromBase64String(jsonData.EncryptedSecretKey!);
                byte[] iv = Convert.FromBase64String(jsonData.Iv!);
                byte[] salt = Convert.FromBase64String(jsonData.Salt!);
                string cipherMode = jsonData.CipherMode ?? "aes-256-cbc";

                // Only AES-256-CBC is supported for now
                if (cipherMode.ToLower() != "aes-256-cbc")
                    throw new NotSupportedException($"Cipher mode '{cipherMode}' is not supported yet.");

                // Derive a 256-bit (32-byte) key from the password and salt
                byte[] key = DerivationService.DeriveKey(rawPassword, salt);

                if (key.Length != 32)
                    throw new ArgumentException($"Invalid key size. Expected 32 bytes, got {key.Length}");

                // Configure AES with derived key, IV and padding
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Decrypt the data using CryptoStream
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        // Return the decrypted string (PEM private key)
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                // Likely a wrong password or corrupted data
                throw new CryptographicException("Decryption failed - likely wrong password", ex);
            }
            catch (Exception ex)
            {
                // General error handling
                throw new Exception("Decryption error", ex);
            }
        }

        /**
         * \class JsonData
         * \brief Helper class used to deserialize JSON content with encryption metadata.
         */
        private class JsonData
        {
            /// Encrypted secret key in Base64.
            public string? EncryptedSecretKey { get; set; }

            /// Initialization Vector (IV) in Base64.
            public string? Iv { get; set; }

            /// Salt used for key derivation, in Base64.
            public string? Salt { get; set; }

            /// Cipher mode identifier, e.g., "aes-256-cbc".
            public string? CipherMode { get; set; }
        }
    }
}
