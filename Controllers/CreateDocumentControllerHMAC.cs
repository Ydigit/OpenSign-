using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;
using OpenSign.Services;

namespace OpenSignControllers
{
    /// @brief Controller responsible for creating and HMAC-signing documents.
    [Route("CreateDocumentHMAC")]
    public class CreateDocumentHMACController : Controller
    {
        private readonly DocumentSigningServiceHmac _documentSigningServiceHmac;

        /// @brief Initializes the HMAC document signing service.
        public CreateDocumentHMACController()
        {
            _documentSigningServiceHmac = new DocumentSigningServiceHmac();
        }

        /// @brief Displays the HMAC document creation form.
        /// @return The CreateDocumentHMAC view.
        [HttpGet]
        public IActionResult CreateDocumentHMAC()
        {
            return View();
        }

        /// @brief Handles HMAC-based signing of documents from user input.
        /// @param form The form data containing the document text and HMAC key.
        /// @return A downloadable JSON file with HMAC signatures or a redirect with error.
        [HttpPost("GerarEAssinarJson")]
        public IActionResult GerarEAssinarJson(IFormCollection form)
        {
            /// @brief Extracts document text input from the form.
            string? textoInput = form["novoInput"];

            /// @brief Extracts HMAC key input from the form.
            string? chaveHmacInput = form["hmacKey"];

            /// @brief Validates form inputs.
            if (string.IsNullOrWhiteSpace(textoInput) || string.IsNullOrWhiteSpace(chaveHmacInput))
                return BadRequest("Text or HMAC Key Invalid");

            try
            {
                /// @brief Generates a signed JSON object using HMAC from user input.
                var resultJson = _documentSigningServiceHmac.GenerateHmacSignedJson(textoInput, chaveHmacInput);

                /// @brief Converts the signed object to formatted JSON string.
                var jsonString = JsonConvert.SerializeObject(resultJson, Formatting.Indented);

                /// @brief Encodes the JSON string into bytes for file response.
                var bytes = Encoding.UTF8.GetBytes(jsonString);

                /// @brief Filename to be used in the file download response.
                var fileName = "Signatures.json";

                /// @brief Stores a success message to display after download.
                TempData["Success"] = "Document Generated with Success!";

                /// @return File download result containing the signed document JSON.
                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                /// @brief Stores the error message in TempData to show in view.
                TempData["Error"] = $"Error generating HMAC: {ex.Message}";

                /// @return Redirects to the form view on error.
                return RedirectToAction("CreateDocumentHMAC");
            }
        }
    }
}
