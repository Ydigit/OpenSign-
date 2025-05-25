using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

/**
 * @class EncryptionCTRService
 * @brief Service that provides encryption using AES in CTR (Counter) mode.
 *
 * This service simulates CTR mode using AES in ECB mode, manually managing the counter.
 * It performs XOR operations block by block to encrypt the data.
 */
public class EncryptionCTRService
{
    /**
     * @brief Encrypts a string using AES in simulated CTR mode.
     *
     * CTR mode is implemented by encrypting a counter block and XORing it with the plaintext.
     * No padding is needed, as each byte is XORed independently.
     *
     * @param data The plaintext string to encrypt.
     * @param key The AES encryption key (must be 16, 24, or 32 bytes).
     * @return A tuple containing:
     *         - EncryptedPrivateKey: the encrypted byte array.
     *         - nonce: the first 8 bytes used as the nonce.
     */
    public (byte[] EncryptedPrivateKey, byte[] nonce) EncryptCTR(string data, byte[] key)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            
            //@brief ECB is used to simulate CTR
            aes.Mode = CipherMode.ECB;

            ///@brief Randomly generated nonce
            byte[] nonce = new byte[8]; 
            RandomNumberGenerator.Fill(nonce);

            ///@brief 8-byte nonce + 8-byte counter
            byte[] counterBlock = new byte[16];

            ///@brief add nonce to the array
            Array.Copy(nonce, counterBlock, nonce.Length); 

            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                ///@brief CTR mode uses XOR, no padding
                byte[] cypherBytes = new byte[dataBytes.Length]; 

                //@brief block by block(each one of 16 bytes) 
                for (int i = 0; i < dataBytes.Length; i += 16)
                {
                    ///@brief Generate keystream block by encrypting the counter block
                    byte[] keyStream = encryptor.TransformFinalBlock(counterBlock, 0, 16); //encriptar cada bloco

                    ///@brief Handle last block if smaller
                    int blockSize = Math.Min(16, dataBytes.Length - i);

                    ///@brief XOR plaintext with keystream (byte by byte)
                    for (int j = 0; j < blockSize; j++)
                    {
                        cypherBytes[i + j] = (byte)(dataBytes[i + j] ^ keyStream[j]);
                    }
                    ///@brief Increment counter (starting after the nonce)
                    IncrementCounter(counterBlock, 8);
                }
                return (cypherBytes, nonce);
            }
        }
    }

    /**
     * @brief Increments the counter portion of a 16-byte counter block.
     *
     * Increments from the given offset, typically skipping the nonce prefix.
     * Supports overflow by propagating the carry byte-wise.
     *
     * @param counterBlock The 16-byte counter block (nonce + counter).
     * @param offset The byte index where the counter starts (usually 8).
     */
    private static void IncrementCounter(byte[] counterBlock, int offset)
    {
        for (int i = counterBlock.Length - 1; i >= offset; i--)
        {

            counterBlock[i]++;

             ///@brief Stop incrementing if no overflow occurred
            if (counterBlock[i] != 0)
                break;
        }
    }

}
