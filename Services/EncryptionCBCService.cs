using System.Security.Cryptography;
using System.Text;

///@class EncryptionCBCService
/// @brief Provides encryption functionality using AES-256 in CBC mode.
/// 
/// This service encrypts UTF-8 encoded text using AES in CBC mode.
/// 
/// A random IV is generated for each encryption and returned alongside the ciphertext.
public class EncryptionCBCService
{
    /// @brief Encrypts a string using AES-CBC with a derived key.
    /// 
    /// The method generates a random IV, encrypts the input string using AES in CBC mode,
    /// 
    /// and returns both the encrypted data and the IV.
    /// @param data The plaintext string to encrypt.
    /// @param key A 32-byte AES key that was previously derived (e.g., using PBKDF2).
    /// @return A tuple containing:
    /// 
    ///          - EncryptedData: the AES-CBC encrypted byte array.
    /// 
    ///          - Iv: the randomly generated initialization vector used in encryption.
    public (byte[] EncryptedData, byte[] Iv) EncryptCBC(string data, byte[] key)//key ja e derivada
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;

            ///@brief Generate a new random IV
            aes.GenerateIV(); 
            aes.Mode = CipherMode.CBC;

            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] encryptSK = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                return (encryptSK, aes.IV);
            }
        }
    }
}
