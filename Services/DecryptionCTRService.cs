using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

public class DecryptionCTRService
{
    public string DecryptPrivateKeyFromJson(string jsonFilePath, string rawPassword)
    {
        try{
            string jsonContent = File.ReadAllText(jsonFilePath);
            var jsonData = JsonSerializer.Deserialize<JsonData>(jsonContent) ??
                throw new InvalidOperationException("Formato JSON inválido.");

            byte[] encryptedData = Convert.FromBase64String(jsonData.EncryptedSecretKey!);
            byte[] nonce = Convert.FromBase64String(jsonData.Nonce!);
            byte[] salt = Convert.FromBase64String(jsonData.Salt!);
            string cipherMode = jsonData.CipherMode ?? "aes-256-ctr";

            if (cipherMode.ToLower() != "aes-256-ctr")
                throw new NotSupportedException($"Cipher mode '{cipherMode}' is not supported yet.");

             if (nonce.Length > 16)
                throw new ArgumentException("Too much big nonce for 16 bytes AES block.");

            // Deriva a chave a partir da password
            int num_iter = 100000;
            using var rfcDerive = new Rfc2898DeriveBytes(rawPassword, salt, num_iter, HashAlgorithmName.SHA256);
            byte[] key = rfcDerive.GetBytes(32); //AES-256 = 32 bytes
            
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            byte[] counterBlock = new byte[16];
            Array.Copy(nonce, counterBlock, nonce.Length);

            byte[] decryptedBytes = new byte[encryptedData.Length];

            using var encryptor = aes.CreateEncryptor();
            
            for (int i = 0; i < encryptedData.Length; i += 16)
            {
                byte[] keyStream = encryptor.TransformFinalBlock(counterBlock, 0, 16);
                int blockSize = Math.Min(16, encryptedData.Length - i);

                for (int j = 0; j < blockSize; j++)
                {
                    decryptedBytes[i + j] = (byte)(encryptedData[i + j] ^ keyStream[j]);
                }
                IncrementCounter(counterBlock, 8);
            }
            return Encoding.UTF8.GetString(decryptedBytes);
        }catch(CryptographicException ex)
        {
            throw new CryptographicException("Decryption failed - likely wrong password", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("Decryption error", ex);
        }
        
    }

    private static void IncrementCounter(byte[] counterBlock, int offset)
    {
        for (int i = counterBlock.Length - 1; i >= offset; i--)
        {
            counterBlock[i]++;

            if (counterBlock[i] != 0)
                break;
        }
    }

    private class JsonData
    {
        public string? EncryptedSecretKey { get; set; }
        public string? Nonce { get; set; }
        public string? Salt { get; set; }
        public string? CipherMode { get; set; }
    }
}
