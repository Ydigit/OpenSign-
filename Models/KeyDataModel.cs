/// @file KeyDataModel.cs
/// @brief Data structure representing the key-related JSON input.

public class KeyDataModel
{
    /// @brief The symmetric cipher mode used for encrypting the private key.
    /// @details Expected values are "CBC" or "CTR".
    public string CipherMode { get; set; } // "CBC" ou "CTR"

    /// @brief The encrypted private key, encoded as a string.
    /// @note Typically encoded in Base64 and produced via AES encryption.
    public string EncryptedPrivateKey { get; set; }
}
