using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenSign.Services
{
    /**
     * @class HmacService
     * @brief Service responsible for computing and verifying HMACs (Hash-based Message Authentication Codes).
     */
    public class HmacService
    {
        /**
         * @brief Verifies whether the provided HMAC matches the content and secret key.
         *
         * This method parses a signed JSON object to extract the original content, the provided HMAC,
         * and the salt. It then derives a key from the password and salt, computes a local HMAC, and
         * compares it securely against the provided one using constant-time comparison.
         *
         * @param signedJson The signed JSON string containing "content", "hmac", and "salt".
         * @param secretKey The password used to derive the HMAC key.
         * @return true if the HMAC is valid and matches; false otherwise.
         */
        public bool VerificarHmac(string signedJson, string secretKey)
        {
            try
            {
                // Parse the signed JSON to extract its components
                var signedData = JObject.Parse(signedJson);

                string? originalContent = signedData["content"]?.ToString();
                string? hmacProvided = signedData["hmac"]?.ToString();
                string? saltBase64 = signedData["salt"]?.ToString();

                var salt = Convert.FromBase64String(saltBase64);

               // Recompute HMAC locally using the same secret key and salt
                string computedHmac = CalcularHmac(originalContent, secretKey, salt);

                 // Compare HMACs using constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHmac),
                    Encoding.UTF8.GetBytes(hmacProvided)
                );
            }
            catch
            {
                return false;
            }
        }

        /**
         * @brief Computes an HMAC-SHA256 from a message and a derived key.
         *
         * The key is derived using PBKDF2 from the given password and salt. The output HMAC is returned
         * as a lowercase hexadecimal string.
         *
         * @param message The message to sign.
         * @param key The password used to derive the cryptographic key.
         * @param salt The salt used during key derivation.
         * @return The computed HMAC as a hexadecimal string.
         */
        public string CalcularHmac(string message, string key, byte[] salt)
        {

            // Derive a cryptographic key from the password and salt
            var chaveDerivada = DerivationService.DeriveKey(key, salt);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            // Create the HMAC-SHA256 using the derived key
            using var hmac = new HMACSHA256(chaveDerivada);
            var hashBytes = hmac.ComputeHash(messageBytes); // Aplica o hash

            // Convert hash bytes to a lowercase hexadecimal string
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2")); // Each byte turns 2 hexadecimal caracters
            }
            return sb.ToString();
        }
    }
}
