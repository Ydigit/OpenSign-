/// @file DocumentEditViewModel.cs
/// @brief Contains models used for document editing and placeholder representation in OpenSign.

namespace OpenSign.Models
{
    /// @class Placeholder
    /// @brief Represents a placeholder field in a document.
    /// 
    /// Used to define dynamic sections within documents, which can be either 
    /// multiple-choice selections or free-text input.
    public class Placeholder
    {
        /// @brief The identifier name of the placeholder (e.g., "UserName").
        /// @note Should not contain spaces to ensure proper signature generation and validation.
        public string Name { get; set; } = string.Empty;

        /// @brief The type of the placeholder.
        /// @details Expected values are:
        /// - "multiple_choice": for predefined selectable values
        /// - "free_text": for user-provided input (not signed)
        public string Type { get; set; } = string.Empty; // "multiple_choice" ou "free_text"

        /// @brief List of selectable options for multiple-choice placeholders.
        /// @note Ignored if the placeholder is of type "free_text".
        public List<string> Options { get; set; } = new List<string>();
    }

    /// @class DocumentEditViewModel
    /// @brief View model for editing and completing a document with placeholders.
    /// 
    /// Holds the base document content, encryption metadata, and parsed placeholders to be filled by the user.
    public class DocumentEditViewModel
    {
        /// @brief The raw document content containing placeholder markers.
        public string DocumentText { get; set; } = string.Empty;

        /// @brief Password used to derive the encryption key (e.g., via HMAC or PBKDF2).
        public string Password { get; set; } = string.Empty;

        /// @brief The selected symmetric encryption mode (e.g., "AES-256-CBC").
        public string EncryptionMode { get; set; } = string.Empty;

        /// @brief List of placeholders found within the document.
        public List<Placeholder> Placeholders { get; set; } = new List<Placeholder>();
    }
}
