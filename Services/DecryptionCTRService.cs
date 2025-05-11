using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public class DecryptionCTRService
{
    public string Decrypt(byte[] encryptedData, byte[] key, byte[] nonce)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.ECB;

            byte[] counterBlock = new byte[16];
            Array.Copy(nonce, counterBlock, nonce.Length);

            byte[] decryptedBytes = new byte[encryptedData.Length];

            using (var encryptor = aes.CreateEncryptor())
            {
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
            }

            return Encoding.UTF8.GetString(decryptedBytes);
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
}
