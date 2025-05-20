using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenSign.Services
{
    public class VerifyDocumentService
    {
        public bool ValidateSignature(string signedJson, string publicKeyJson)
        {
            try
            {
                var signedData = JObject.Parse(signedJson);
                var publicKeyData = JObject.Parse(publicKeyJson);

                string? originalContent = signedData["content"]?.ToString();
                string? signatureBase64 = signedData["signature"]?.ToString();
                string? publicKeyXml = publicKeyData["publicKey"]?.ToString();

                if (string.IsNullOrEmpty(originalContent) ||
                    string.IsNullOrEmpty(signatureBase64) ||
                    string.IsNullOrEmpty(publicKeyXml))
                {
                    return false;
                }

                byte[] dataBytes = Encoding.UTF8.GetBytes(originalContent);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

                using var rsa = RSA.Create();
                rsa.FromXmlString(publicKeyXml);

                // SHA256 is a common hash algorithm for signatures
                return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }
    }

}