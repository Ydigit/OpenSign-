/// @file ErrorViewModel.cs
/// @brief Model used for displaying error information in OpenSign.

namespace OpenSign_.Models
{
    /// @class ErrorViewModel
    /// @brief Represents diagnostic information related to an application error.
    /// 
    /// Typically used to display error details and request identifiers to assist debugging.
    public class ErrorViewModel
    {
        /// @brief The unique request identifier associated with the current error.
        /// @note Can be used to trace logs and debug server-side issues.
        public string? RequestId { get; set; }

        /// @brief Indicates whether the RequestId should be shown in the UI.
        /// @return True if RequestId is not null or empty; otherwise, false.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
