/// @file DocumentCreateModel.cs
/// @brief View model for document creation in the OpenSign system.

namespace OpenSign.Models
{
    /// @class DocumentCreateModel
    /// @brief Represents the input model used when creating a new document.
    ///
    /// This model captures the raw document content, the password to derive encryption keys,
    /// and the chosen symmetric encryption mode (e.g., AES-256-CBC or AES-256-CTR).
    public class DocumentCreateModel
    {
        /// @brief The full text content of the document to be created.
        /// @note May include placeholders for user-defined or authorized values.
        public string DocumentText { get; set; }

        /// @brief The password provided by the user.
        ///
        /// This password is used to derive a symmetric encryption key
        /// (e.g., via PBKDF2 with HMAC-SHA256) to encrypt the private key.
        public string Password { get; set; }

        /// @brief The selected encryption mode for private key protection.
        ///
        /// Expected values include "AES-256-CBC" or "AES-256-CTR".
        public string EncryptionMode { get; set; }
    }
}
