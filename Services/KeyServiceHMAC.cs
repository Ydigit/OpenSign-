using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using OpenSign.Shared;

namespace OpenSign.Services // Adicione esta linha
{
    public class KeyServiceHmac
    {
        public string GenerateHmacKeyJSON(int keySize, string rawpass, string encmode)
        {
            if (keySize != 128 && keySize != 256 && keySize != 512)
                throw new ArgumentException("Tamanho de chave HMAC inválido.");

            // Convert bits to bytes
            int keySizeInBytes = keySize / 8;
            byte[] hmacKey = new byte[keySizeInBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(hmacKey);
            }

            // Derivar senha (gera chave + salt)
            var derivation = DerivationService.DeriveKey(rawpass);
            var derivedKey = derivation.Kderivada;
            var salt = derivation.salt;

            string jsonfilePath = AppPaths.SecurePrivateBackupPathJSON($"hmacKey-{DateTime.Now.Ticks}");

            object jsonObject;

            if (encmode.Equals("aes-256-cbc", StringComparison.OrdinalIgnoreCase))
            {
                var encryptionServiceCBC = new EncryptionCBCService();
                var result = encryptionServiceCBC.EncryptCBC(Convert.ToBase64String(hmacKey), derivedKey);

                jsonObject = new
                {
                    EncryptedHmacKey = Convert.ToBase64String(result.EncryptedData),
                    Iv = Convert.ToBase64String(result.Iv),
                    Salt = Convert.ToBase64String(salt),
                    CipherMode = "aes-256-cbc"
                };
            }
            else if (encmode.Equals("aes-256-ctr", StringComparison.OrdinalIgnoreCase))
            {
                var encryptionServiceCTR = new EncryptionCTRService();
                var result = encryptionServiceCTR.EncryptCTR(Convert.ToBase64String(hmacKey), derivedKey);

                jsonObject = new
                {
                    EncryptedHmacKey = Convert.ToBase64String(result.EncryptedPrivateKey),
                    Nonce = Convert.ToBase64String(result.nonce),
                    Salt = Convert.ToBase64String(salt),
                    CipherMode = "aes-256-ctr"
                };
            }
            else
            {
                throw new ArgumentException("Modo de cifra inválido.");
            }

            string json = JsonSerializer.Serialize(jsonObject);

            string directory = Path.GetDirectoryName(jsonfilePath)!;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(jsonfilePath, json);

            return jsonfilePath;
        }
    }

    public class KeysController
    {
        private readonly KeyServiceHmac _keyService;

        public KeysController(KeyServiceHmac keyService)
        {
            _keyService = keyService;
        }

        // ...existing code...
    }
}
