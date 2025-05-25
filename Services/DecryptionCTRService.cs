using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

/**
 * @class DecryptionCTRService
 * @brief Provides decryption functionality for private keys encrypted using AES-256 in CTR mode.
 *
 * This service reads a JSON file containing an encrypted private key and decryption metadata,
 * derives the key using PBKDF2, and decrypts the content using AES-256 in simulated CTR mode.
 */
public class DecryptionCTRService
{
    /**
     * @brief Decrypts a private key from a JSON file using AES-256 in CTR mode.
     *
     * Parses a JSON file that includes the encrypted private key, nonce, salt, and cipher mode.
     * Derives the AES key using the provided password and decrypts the data using CTR logic.
     *
     * @param jsonFilePath The path to the JSON file.
     * @param rawPassword The password used to derive the AES-256 key.
     * @return The decrypted private key as a UTF-8 string.
     *
     * @throws CryptographicException If the decryption fails due to an incorrect password or corrupted data.
     * @throws Exception If any other error occurs during parsing or decryption.
     */

    public string DecryptPrivateKeyFromJson(string jsonFilePath, string rawPassword)
    {
        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            var jsonData = JsonSerializer.Deserialize<JsonData>(jsonContent) ??
                throw new InvalidOperationException("Invalid .json format.");

            byte[] encryptedData = Convert.FromBase64String(jsonData.EncryptedSecretKey!);
            byte[] nonce = Convert.FromBase64String(jsonData.Nonce!);
            byte[] salt = Convert.FromBase64String(jsonData.Salt!);
            string cipherMode = jsonData.CipherMode ?? "aes-256-ctr";

            if (cipherMode.ToLower() != "aes-256-ctr")
                throw new NotSupportedException($"Cipher mode '{cipherMode}' is not supported yet.");

            if (nonce.Length > 16)
                throw new ArgumentException("Too much big nonce for 16 bytes AES block.");

            ///@brief Derive the AES-256 key from the password and salt
            byte[] key = DerivationService.DeriveKey(rawPassword, salt); // 32 bytes for AES-256

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            byte[] counterBlock = new byte[16];

            ///@brief copy nonce to the array
            Array.Copy(nonce, counterBlock, nonce.Length);

            byte[] decryptedBytes = new byte[encryptedData.Length];

            using var encryptor = aes.CreateEncryptor();

            for (int i = 0; i < encryptedData.Length; i += 16)
            {
                byte[] keyStream = new byte[16];

                ///@brief Use TransformBlock
                encryptor.TransformBlock(counterBlock, 0, 16, keyStream, 0);

                int blockSize = Math.Min(16, encryptedData.Length - i);

                for (int j = 0; j < blockSize; j++)
                {
                    ///@brief uploads the value doing the XOR 
                    decryptedBytes[i + j] = (byte)(encryptedData[i + j] ^ keyStream[j]);
                }
                //@ brief Increment from byte 8 onward (after nonce)
                IncrementCounter(counterBlock, 8);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
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
    /**
     * @brief Increments the counter portion of a 16-byte block.
     *
     * Performs a big-endian style increment on the block, starting from the specified offset.
     * This is used to simulate the CTR mode counter incrementation.
     *
     * @param counterBlock The 16-byte counter block containing the nonce and counter.
     * @param offset The index in the array where the counter begins (usually 8).
     */
    private static void IncrementCounter(byte[] counterBlock, int offset)
    {
        for (int i = counterBlock.Length - 1; i >= offset; i--)
        {
            counterBlock[i]++;

            if (counterBlock[i] != 0)
                break;
        }
    }
    /**
     * @class JsonData
     * @brief Represents the structure of the JSON file used for decryption.
     *
     * This class defines the properties expected in the encryption metadata JSON file,
     * including the encrypted private key, nonce, salt, and cipher mode.
     */
    private class JsonData
    {
        public string? EncryptedSecretKey { get; set; }
        public string? Nonce { get; set; }
        public string? Salt { get; set; }
        public string? CipherMode { get; set; }
    }
}
