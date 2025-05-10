using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using OpenSign.Shared;

public class KeyService
{
    private string _publicKey = string.Empty; //direta no disco
    private string _privateKey = string.Empty; //cifrar e guardar no disco
    private string _keyCreationDate = string.Empty;

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
            //Based on settings defined by the user, internal vars get the actual values for this void callback
            GenerateKeys(keySize, format);
        }
        else
        {
            LoadKeysFromFiles();
        }
    }
    //Gerar as Keys
    public void GenerateKeys(int keySize,string format)
    {
        GenerateRSAKeyPair(keySize, format);
    }

    /// <summary>
    /// Generates RSA keys based on the specified key size and format.
    /// </summary>
    /// 
    //meter : iv, salt, pk, skc, modo
    //mediante o modo ele escolhe a cifra


    private void GenerateRSAKeyPairPEM(int keySize, byte[] derivekey, ) // meter sempre pem
    {
        //prepare paths for the file location
        string dateTicks = DateTime.Now.Ticks.ToString();//momento de geracao
        string pubfilePath = null;
        string privfilePath = null;
        string ivPath = null;


        using (var rsa = RSA.Create())
        {
                rsa.KeySize = keySize;
                    //rsa values
                    _publicKey = ExportPublicKeyPEM(rsa);
                    _privateKey = ExportPrivateKeyPEM(rsa); //ta em memo aqui a pk
                    //file paths
                     pubfilePath = AppPaths.GetKeyPathPEMpublic($"pk-{dateTicks}");
                     privfilePath = AppPaths.SecurePrivateBackupPathKEY($"sk-{dateTicks}");
                     //ivPath = AppPaths.GetKeyPathGeneral($"iv-{dateTicks}.iv");
                     //Check if the directory exists, if not create it
                     if (!System.IO.Directory.Exists(AppPaths.KeysPath))
                     {
                                System.IO.Directory.CreateDirectory(AppPaths.KeysPath);
                     }

                    //guardar a chave publica
                    System.IO.File.WriteAllBytes(pubfilePath,_publicKey);
                    //chamar derivado é no controller
                    
                    //cifrar e guardar cifrado, cifra com o derivekey
                    var encryptionService = new EncryptionCBCService();
                    //cifrar a chave privada
                    var result = encryptionService.EncryptCBC(_privateKey, derivekey);//tupl com a cifra e iv
                    //guardar chave cifrada , 
                    File.WriteAllBytes(privfilePath, result.EncryptedData);
                    File.WriteAllBytes(ivPath, result.Iv);
        }
    }

    /// <summary>
    /// Carrega as chaves RSA do serviço a partir de arquivos existentes.
    /// </summary>
    /// 
    //Aux Functions
    // Extract nr o Ticks from the file name
    private long ExtractTicksFromFileName(string fileNameWithoutExtension)
    {
        //array for separation
        var parts = fileNameWithoutExtension.Split('_');//unecessary chars from fileName
        if (parts.Length == 2 && long.TryParse(parts[1], out long ticks)) //Str->Long if success -> ticks
        {
            return ticks;
        }
        return 0;
    }

    // Get timeStamp function
    private string ExtractTimestampFromFileName(string fileNameWithoutExtension)
    {
        var parts = fileNameWithoutExtension.Split('_');
        if (parts.Length == 2)
            return parts[1]; //returns only the raw ticks value as a String
        return "";
    }

    private void EnsureKeyPathExists()
    {
        if (Directory.Exists(AppPaths.KeysPath))
        {
            Console.WriteLine("KeyPath is there");
        }
        else
        {
            Directory.CreateDirectory(AppPaths.KeysPath);
        }
    }




    private void LoadKeysFromFiles()
    {
        EnsureKeyPathExists(); //KeysPAth

        //List for files present in the KeysPath dir with public & ext of pem || xml
        var publicKeys = Directory.GetFiles(AppPaths.KeysPath, "public_*.pem")
            .Concat(Directory.GetFiles(AppPaths.KeysPath, "public_*.xml"))
            .ToList();

        if (!publicKeys.Any())//if empty
        {
            Console.WriteLine("No public keys available for you!");
            return;
        }

        // Extract ticks from the previous list
        //Here we are able to sort the files, where for each string(path/filename) remove extension
        //inject on the tick extractor as a string without any .pem or .xml
        //Basically orders from the highest to the lowest value of ticks bcs they are longs, then select the first one
        //All for public ofc -> reuse for private search
        //Selecting the latest fileName /w ext so for private just replace the pub to priv
        var latestPublicKey = publicKeys
            .OrderByDescending(path => ExtractTicksFromFileName(Path.GetFileNameWithoutExtension(path)))
            .First();

        // Extract ticks as string from array of the trim of the fileName
        string timestamp = ExtractTimestampFromFileName(Path.GetFileNameWithoutExtension(latestPublicKey));
        
        // Construir o nome do ficheiro privado correspondente
        string latestPrivateKey = latestPublicKey.Replace("public_", "private_");

        if (File.Exists(latestPrivateKey) && File.Exists(latestPublicKey))
        {
            //If they exist on the sys->2 string for each content
            _publicKey = File.ReadAllText(latestPublicKey);
            _privateKey = File.ReadAllText(latestPrivateKey);
            _keyCreationDate = timestamp;//equal for both
        }
        else
        {
            Console.WriteLine("Public or Private Keyy Missing:(");
        }
    }





    //------------------------------------------------------------------------
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