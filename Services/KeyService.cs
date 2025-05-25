using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenSign.Shared;

/// @brief Service that manages RSA key generation, encryption, storage, and retrieval.
public class KeyService
{
    private string _publicKey = string.Empty;
    private string _privateKey = string.Empty;
    private string _keyCreationDate = string.Empty;

    /// @brief Initializes RSA keys. Generates new ones if valid size is provided, or loads existing ones.
    /// @param keySize RSA key size (2048, 3072, or 4096).
    /// @param rawpass User password for encryption.
    /// @param format Format for key output.
    public void InitializeKeys(int keySize, string rawpass, string format)
    {
        if (keySize == 2048 || keySize == 3072 || keySize == 4096)
        {
            GenerateKeys(keySize, rawpass, format);
        }
        else
        {
            LoadKeysFromFiles();
        }
    }

    /// @brief Wrapper to generate RSA keys.
    public void GenerateKeys(int keySize, string rawpass, string format)
    {
        GenerateRSAKeyPairJSON(keySize, rawpass, format);
    }

    /// @brief Generates RSA key pair and stores it in JSON and PEM format.
    /// @param keySize The RSA key size.
    /// @param rawpass Password to derive symmetric encryption key.
    /// @param encmode Encryption mode ("aes-256-cbc" or "aes-256-ctr").
    /// @return Tuple with private and public key file paths.
    public (string jsonfilePath, string pubfilePath) GenerateRSAKeyPairJSON(int keySize, string rawpass, string encmode)
    {
        string dateTicks = DateTime.Now.Ticks.ToString();
        string pubfilePath;
        string jsonfilePath;
        object? jsonDownload = null;

        using (var rsa = RSA.Create())
        {
            rsa.KeySize = keySize;
            _publicKey = ExportPublicKeyPEM(rsa);
            _privateKey = ExportPrivateKeyPEM(rsa);

            pubfilePath = AppPaths.GetKeyPathPEMpublic($"pk-{dateTicks}");
            jsonfilePath = AppPaths.SecurePrivateBackupPathJSON($"sk-{dateTicks}");

            Directory.CreateDirectory(Path.GetDirectoryName(pubfilePath)!);

            var passderivada = DerivationService.DeriveKey(rawpass);
            var passwordDerivada = passderivada.Kderivada;
            var salt = passderivada.salt;

            if (encmode.Equals("aes-256-cbc"))
            {
                var result = new EncryptionCBCService().EncryptCBC(_privateKey, passwordDerivada);
                jsonDownload = new
                {
                    EncryptedSecretKey = Convert.ToBase64String(result.EncryptedData),
                    Iv = Convert.ToBase64String(result.Iv),
                    Salt = Convert.ToBase64String(salt),
                    CipherMode = encmode
                };
            }
            else if (encmode.Equals("aes-256-ctr"))
            {
                var result = new EncryptionCTRService().EncryptCTR(_privateKey, passwordDerivada);
                jsonDownload = new
                {
                    EncryptedSecretKey = Convert.ToBase64String(result.EncryptedPrivateKey),
                    Nonce = Convert.ToBase64String(result.nonce),
                    Salt = Convert.ToBase64String(salt),
                    CipherMode = encmode
                };
            }
            else
            {
                throw new ArgumentException("Invalid encryption mode.");
            }

            var jsonDownloadPublicKey = new { PublicKey = _publicKey };
            var options = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            File.WriteAllText(jsonfilePath, JsonSerializer.Serialize(jsonDownload));
            File.WriteAllText(pubfilePath, JsonSerializer.Serialize(jsonDownloadPublicKey, options));

            return (jsonfilePath, pubfilePath);
        }
    }

    /// @brief Loads the most recent RSA key pair from the storage path.
    private void LoadKeysFromFiles()
    {
        EnsureKeyPathExists();

        var publicKeys = Directory.GetFiles(AppPaths.KeysPath, "public_*.pem")
            .Concat(Directory.GetFiles(AppPaths.KeysPath, "public_*.xml"))
            .ToList();

        if (!publicKeys.Any())
        {
            Console.WriteLine("No public keys available for you!");
            return;
        }

        var latestPublicKey = publicKeys
            .OrderByDescending(path => ExtractTicksFromFileName(Path.GetFileNameWithoutExtension(path)))
            .First();

        string timestamp = ExtractTimestampFromFileName(Path.GetFileNameWithoutExtension(latestPublicKey));
        string latestPrivateKey = latestPublicKey.Replace("public_", "private_");

        if (File.Exists(latestPrivateKey) && File.Exists(latestPublicKey))
        {
            _publicKey = File.ReadAllText(latestPublicKey);
            _privateKey = File.ReadAllText(latestPrivateKey);
            _keyCreationDate = timestamp;
        }
        else
        {
            Console.WriteLine("Public or Private Key Missing :(");
        }
    }

    /// @brief Ensures the key path directory exists.
    private void EnsureKeyPathExists()
    {
        if (!Directory.Exists(AppPaths.KeysPath))
        {
            Directory.CreateDirectory(AppPaths.KeysPath);
        }
    }

    /// @brief Extracts ticks (timestamp) from a filename.
    private long ExtractTicksFromFileName(string fileNameWithoutExtension)
    {
        var parts = fileNameWithoutExtension.Split('_');
        if (parts.Length == 2 && long.TryParse(parts[1], out long ticks))
        {
            return ticks;
        }
        return 0;
    }

    /// @brief Extracts timestamp string from filename.
    private string ExtractTimestampFromFileName(string fileNameWithoutExtension)
    {
        var parts = fileNameWithoutExtension.Split('_');
        return parts.Length == 2 ? parts[1] : "";
    }

    /// @brief Exports the public RSA key in base64 (SPKI format).
    private string ExportPublicKeyPEM(RSA rsa)
    {
        byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        return Convert.ToBase64String(publicKeyBytes);
    }

    /// @brief Exports the private RSA key in base64 (PKCS#8 format).
    private string ExportPrivateKeyPEM(RSA rsa)
    {
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        return Convert.ToBase64String(privateKeyBytes);
    }

    /// @brief Encodes given key bytes into PEM format.
    public static string PemEncode(string label, byte[] data)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"-----BEGIN {label}-----");
        builder.AppendLine(Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine($"-----END {label}-----");
        return builder.ToString();
    }

    /// @brief Gets the current key pair as a dynamic object.
    public object GetCurrentKeys()
    {
        return new
        {
            PublicKey = _publicKey,
            PrivateKey = _privateKey
        };
    }
}
