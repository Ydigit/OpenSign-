using System;
using System.Security.Cryptography;
using System.Text;

public class KeyService
{
    private string _publicKey = string.Empty;
    private string _privateKey = string.Empty;

    /// <summary>
    /// Inicializa as chaves RSA do serviço.
    /// <para>
    /// Se o usuário optar por gerar novas chaves, elas serão geradas com o tamanho
    /// especificado e salvas em arquivos XML.
    /// </para>
    /// <para>
    /// Se o usuário optar por não gerar novas chaves, elas serão carregadas dos
    /// arquivos XML existentes.
    /// </para>
    /// </summary>
    public void InitializeKeys(int keySize, string format)
    {
        if (keySize == 2048 || keySize == 3072 || keySize == 4096)
        {
            GenerateRSAKeyPair(keySize, format);
        }
        else
        {
            LoadKeysFromFiles();
        }
    }

    /// <summary>
    /// Generates RSA keys based on the specified key size and format.
    /// </summary>
    private void GenerateRSAKeyPair(int keySize, string format)
    {
        using (var rsa = RSA.Create())
        {
            rsa.KeySize = keySize;

            if (format == "pem")
            {
                _publicKey = ExportPublicKeyPEM(rsa);
                _privateKey = ExportPrivateKeyPEM(rsa);

                System.IO.File.WriteAllText("publicKey.pem", _publicKey);
                System.IO.File.WriteAllText("privateKey.pem", _privateKey);
            }
            else // xml
            {
                _publicKey = rsa.ToXmlString(false); // public only
                _privateKey = rsa.ToXmlString(true);  // public + private

                System.IO.File.WriteAllText("publicKey.xml", _publicKey);
                System.IO.File.WriteAllText("privateKey.xml", _privateKey);
            }
        }
    }

    /// <summary>
    /// Carrega as chaves RSA do serviço a partir de arquivos existentes.
    /// </summary>
    private void LoadKeysFromFiles()
    {
        if (System.IO.File.Exists("publicKey.pem") && System.IO.File.Exists("privateKey.pem"))
        {
            _publicKey = System.IO.File.ReadAllText("publicKey.pem");
            _privateKey = System.IO.File.ReadAllText("privateKey.pem");
        }
        else if (System.IO.File.Exists("publicKey.xml") && System.IO.File.Exists("privateKey.xml"))
        {
            _publicKey = System.IO.File.ReadAllText("publicKey.xml");
            _privateKey = System.IO.File.ReadAllText("privateKey.xml");
        }
        else
        {
            Console.WriteLine("Nenhum arquivo de chave encontrado.");
        }
    }

    /// <summary>
    /// Exporta a chave pública em formato PEM.
    /// </summary>
    private string ExportPublicKeyPEM(RSA rsa)
    {
        byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        return PemEncode("PUBLIC KEY", publicKeyBytes);
    }

    /// <summary>
    /// Exports the private key in PEM format.
    /// </summary>
    private string ExportPrivateKeyPEM(RSA rsa)
    {
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        return PemEncode("PRIVATE KEY", privateKeyBytes);
    }

    /// <summary>
    /// Encodes the given byte array in PEM format with the specified label.
    /// </summary>
    private string PemEncode(string label, byte[] data)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"-----BEGIN {label}-----");
        builder.AppendLine(Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine($"-----END {label}-----");
        return builder.ToString();
    }

    /// <summary>
    /// Returns a new object with the current values of the public and private keys.
    /// </summary>
    public object GetCurrentKeys()
    {
        return new
        {
            PublicKey = _publicKey,
            PrivateKey = _privateKey
        };
    }
}


/*
dotnet new console -n HelloWorldApp
cd HelloWorldApp
dotnet run

http://localhost:5016/api/key/keys/2048
http://localhost:5016/api/key/keys/3072
http://localhost:5016/api/key/keys/4096
*/