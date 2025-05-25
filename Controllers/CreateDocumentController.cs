using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenSign.Services;
using PlaceholderTextApp.Services;

namespace PlaceholderTextApp.Controllers
{
    /// @brief Controller responsible for document creation and RSA-based digital signing.
    [Route("CreateDocument")]
    public class CreateDocumentController : Controller
    {
        private readonly DocumentSigningService _documentSigningService;

        /// @brief Constructor initializes the document signing service.
        public CreateDocumentController()
        {
            _documentSigningService = new DocumentSigningService();
        }

        /// @brief Displays the document creation form.
        /// @return The CreateDocument view.
        [HttpGet]
        public IActionResult CreateDocument()
        {
            return View();
        }

        /// @brief Processes the user input and encrypted key file to generate digitally signed document data.
        /// @param form The form collection containing user inputs (text, password).
        /// @param keyFile The encrypted key file uploaded by the user.
        /// @return A downloadable JSON file with signatures, or a redirect back to the form on error.
        [HttpPost("GerarEAssinarJson")]
        public async Task<IActionResult> GerarEAssinarJson(IFormCollection form, IFormFile keyFile)
        {
            /// @brief Extracts the user input text from the form.
            string? textoInput = form["novoInput"];

            /// @brief Extracts the password used to decrypt the private key.
            string? password = form["pss"];

            /// @brief Validates input text and password.
            if (string.IsNullOrWhiteSpace(textoInput) || string.IsNullOrWhiteSpace(password))
                return BadRequest("Text or password is invalid.");

            /// @brief Validates uploaded key file.
            if (keyFile == null || keyFile.Length == 0)
                return BadRequest("Key file was not provided.");

            try
            {
                /// @brief Calls the service to decrypt the key and sign the document.
                var resultJson = await _documentSigningService.ProcessDocumentAsync(keyFile, password, textoInput);

                /// @brief Serializes the signed document object to indented JSON.
                var jsonString = System.Text.Json.JsonSerializer.Serialize(resultJson, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                /// @brief Encodes the JSON to UTF-8 byte array for file download.
                var bytes = Encoding.UTF8.GetBytes(jsonString);

                /// @brief File name used in the response download.
                var fileName = "Signatures.json";

                /// @brief Stores a success message for the view.
                TempData["Success"] = "Document generated with success!";

                /// @brief Returns the signed document as a downloadable file.
                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                /// @brief Stores the error message for display in the view.
                TempData["Error"] = $"Error: {ex.Message}";

                /// @brief Redirects user back to the form view in case of failure.
                return RedirectToAction("CreateDocument");
            }
        }
    }
}
