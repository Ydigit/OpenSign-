//funcao cifra leo

using System.Security.Cryptography;
using System.Text;

public class EncryptionCBCService
{
    public (byte[] EncryptedPrivateKey, byte[] cbcIv) EncryptCBC(string data, byte[] key)//key ja e derivada
    {
        using (var aes = Aes.Create())//depois do using ele vai remover o obj instanciado aes
        {
            aes.Key = key;//ja vem derivada
            aes.GenerateIV(); // Random iv, but need to return it also
            aes.Mode = CipherMode.CBC;

            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);//parsing
                byte[] encryptSK = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                return ( encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length), aes.IV);
            }
        }
    }
}
