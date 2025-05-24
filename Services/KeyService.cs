using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
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
    /// 
    //meter a rawpass
    public void InitializeKeys(int keySize, string rawpass, string format)
    {
        if (keySize == 2048 || keySize == 3072 || keySize == 4096)
        {
            //Based on settings defined by the user, internal vars get the actual values for this void callback
            GenerateKeys(keySize,rawpass,format);
        }
        else
        {
            LoadKeysFromFiles();
        }
    }
    //Gerar as KeysF
    public void GenerateKeys(int keySize, string rawpass, string format)
    {
        GenerateRSAKeyPairJSON(keySize,rawpass, format);
    }

    /// <summary>
    /// Generates RSA keys based on the specified key size and format.
    /// </summary>
    /// 
    //meter : iv, salt, pk, skc, modo
    //mediante o modo ele escolhe a cifra

    public (string jsonfilePath, string pubfilePath) GenerateRSAKeyPairJSON(int keySize, string rawpass, string encmode) // meter sempre pem
    {
        //prepare paths for the file location
        string dateTicks = DateTime.Now.Ticks.ToString();//momento de geracao
        string pubfilePath = null;
        string jsonfilePath = null;//tirar
        EncryptionCBCService? encryptionServiceCBC = null;
        EncryptionCTRService? encryptionServiceCTR = null;
        object? jsonDownload = null;


        using (var rsa = RSA.Create())
        {
                rsa.KeySize = keySize;
                    //rsa values
                    _publicKey = ExportPublicKeyPEM(rsa);
                    _privateKey = ExportPrivateKeyPEM(rsa); //ta em memo aqui a pk
                    //file paths
                     pubfilePath = AppPaths.GetKeyPathPEMpublic($"pk-{dateTicks}");
                     //tira o da key
                     jsonfilePath = AppPaths.SecurePrivateBackupPathJSON($"sk-{dateTicks}");
                    //Check if the directory exists, if not create it
                    string directory = Path.GetDirectoryName(pubfilePath)!;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    //var deriveService = new DerivationService();
                    var passderivada = DerivationService.DeriveKey(rawpass); // sem new DerivationService()

                    var passwordDerivada = passderivada.Kderivada;
                    //**********+//ERRO da geracao random****************
                    var salt = passderivada.salt;

                    //guardar a chave publica 
            //Ate aqui gera as chaves

            if (encmode.Equals("aes-256-cbc"))
            {
                encryptionServiceCBC = new EncryptionCBCService();
                var result = encryptionServiceCBC.EncryptCBC(_privateKey, passwordDerivada);//tupl com a cifra e iv
                var cbcIv = result.Iv;
                var encsk = result.EncryptedData;
                //json pra decifrar: cippher, iv, salt, cipherMode
                jsonDownload = new
                {
                    EncryptedSecretKey = Convert.ToBase64String(encsk),
                    Iv = Convert.ToBase64String(cbcIv),
                    Salt = Convert.ToBase64String(salt),
                    CipherMode = encmode
                };

            }
            else if(encmode.Equals("aes-256-ctr"))
            {
                encryptionServiceCTR = new EncryptionCTRService();
                var result = encryptionServiceCTR.EncryptCTR(_privateKey, passwordDerivada);
                var ctrNounce = result.nonce;
                var encsk = result.EncryptedPrivateKey;
                //json pra decifrar: cipher, nounce, salt, cipherMode
                jsonDownload = new
                {
                    EncryptedSecretKey = Convert.ToBase64String(encsk),
                    Nonce = Convert.ToBase64String(ctrNounce),
                    Salt = Convert.ToBase64String(salt),
                    CipherMode = encmode
                };
            }
            else
            {
                throw new ArgumentException("Invalid encryption mode.");
            }

            var jsonDownloadPublicKey = new
                {
                    //PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(_publicKey)),
                    PublicKey = _publicKey,
                };


            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            string jsonDownloadString = JsonSerializer.Serialize(jsonDownload);
            string jsonDownloadStringPublicKey = JsonSerializer.Serialize(jsonDownloadPublicKey, options);
            // Verifica se a pasta securekeys/private existe
            string privateDirectory = Path.GetDirectoryName(jsonfilePath)!;
            if (!Directory.Exists(privateDirectory))
            {
                Directory.CreateDirectory(privateDirectory);
            }

            //guardar o JSON
            File.WriteAllText(jsonfilePath, jsonDownloadString);//escreve em string o json
            File.WriteAllText(pubfilePath,jsonDownloadStringPublicKey);


           return (jsonfilePath, pubfilePath);

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
        //return PemEncode("PUBLIC KEY", publicKeyBytes);
        return Convert.ToBase64String(publicKeyBytes);
    }

    /// <summary>
    /// Exports the private key in PEM format.
    /// </summary>
    private string ExportPrivateKeyPEM(RSA rsa)
    {
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();   
        //return PemEncode("PRIVATE KEY", privateKeyBytes);
        return Convert.ToBase64String(privateKeyBytes);
    }

    /// <summary>
    /// Encodes the given byte array in PEM format with the specified label.
    /// </summary>
    public static string PemEncode(string label, byte[] data)
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