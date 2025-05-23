using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenSign.Services
{
    // Serviço responsável por calcular e verificar HMACs
    public class HmacService
    {
        // Verifica se o HMAC fornecido confere com o conteúdo e a chave
        public bool VerificarHmac(string signedJson, string secretKey)
        {
            try
            {
                // Faz o parse do JSON assinado para extrair os dados
                var signedData = JObject.Parse(signedJson);

                // Extrai o conteúdo original e o HMAC fornecido no JSON
                string? originalContent = signedData["content"]?.ToString();
                string? hmacProvided = signedData["hmac"]?.ToString();
                string? saltBase64 = signedData["salt"]?.ToString();

                var salt = Convert.FromBase64String(saltBase64);

                // Recalcula o HMAC localmente com a chave fornecida
                string computedHmac = CalcularHmac(originalContent, secretKey, salt);

                // Compara os HMACs de forma segura (protege contra ataques de timing)
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHmac),
                    Encoding.UTF8.GetBytes(hmacProvided)
                );
            }
            catch
            {
                // Em caso de erro no parsing ou na verificação, retorna false
                return false;
            }
        }

        // Calcula um HMAC-SHA256 em formato hexadecimal
        public String CalcularHmac(string message, string key, byte[] salt)
        {

            // Deriva a chave usando o salt já dado
            var chaveDerivada = DerivationService.DeriveKey(key, salt);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            // Cria o HMAC-SHA256 com a chave
            using var hmac = new HMACSHA256(chaveDerivada);
            var hashBytes = hmac.ComputeHash(messageBytes); // Aplica o hash

            // Converte o resultado para string hexadecimal (lowercase)
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2")); // Cada byte vira 2 caracteres hexadecimais
            }
            return sb.ToString();
        }
    }
}
