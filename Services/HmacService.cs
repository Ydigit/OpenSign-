using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenSign.Services
{
    public class HmacService
    {
        public bool VerificarHmac(string signedJson, string secretKey)
        {
            try
            {
                var signedData = JObject.Parse(signedJson);

                string? originalContent = signedData["content"]?.ToString();
                string? hmacProvided = signedData["hmac"]?.ToString();

                if (string.IsNullOrEmpty(originalContent) || string.IsNullOrEmpty(hmacProvided))
                {
                    return false;
                }

                string computedHmac = CalcularHmac(originalContent, secretKey);

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

        public string CalcularHmac(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);

            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
