// funcao de decifra do dinis

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

public class DecryptCBCService
{
    private class EncryptionData
    {
        public string EncryptedKey { get; set; }
        public string IV { get; set; }
        public string Method { get; set; }
        public string Salt { get; set; }
    }

    public string DecryptRsaKeyFromJson(string jsonFilePath, string password)
    {
        // Ler e parsear o ficheiro JSON
        var json = File.ReadAllText(jsonFilePath);
        var data = JsonConvert.DeserializeObject<EncryptionData>(json);

        // Validar o método de cifra
        if (data.Method?.ToLower() != "aes-256-cbc")
        {
            throw new InvalidOperationException($"Método de cifra não suportado: {data.Method}");
        }

        // Converter de Base64 para bytes
        byte[] encryptedKeyBytes = Convert.FromBase64String(data.EncryptedKey);
        byte[] ivBytes = Convert.FromBase64String(data.IV);
        byte[] saltBytes = Convert.FromBase64String(data.Salt);

        // Derivar a chave usando a password e o salt do JSON
        byte[] keyBytes = DeriveKeyWithSalt(password, saltBytes);

        // Descriptografar
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(encryptedKeyBytes);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }

    private byte[] DeriveKeyWithSalt(string password, byte[] salt)
    {
        int iterations = 100000; // Mesmo número usado na cifra
        
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256);
            
        return pbkdf2.GetBytes(32); // 32 bytes = 256 bits para AES-256
    }
}