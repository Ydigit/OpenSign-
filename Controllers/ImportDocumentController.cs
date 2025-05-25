using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenSign.Services;
using System.Text;

namespace PlaceholderTextApp.Controllers
{
    /// @brief Controller responsible for importing signed documents and verifying signatures.
    [Route("ImportDocument")]
    public class ImportDocumentController : Controller
    {
        private readonly DocumentImportService _documentImportService;

        /// @brief Constructor initializes the document import service.
        public ImportDocumentController()
        {
            _documentImportService = new DocumentImportService();
        }

        /// @brief Displays the import form for uploading and verifying signed documents.
        /// @return The ImportDocument view.
        [HttpGet("")]
        public IActionResult ImportDocument()
        {
            return View();
        }

        /// @brief Handles the POST request to import and verify a digitally signed document.
        /// @param jsonFile The uploaded signed document JSON.
        /// @param keyJsonFile The uploaded public key JSON file.
        /// @param form The form data (fallback if no files are uploaded).
        /// @return A view with verification results or error messages.
        [HttpPost("")]
        public async Task<IActionResult> ImportDocument(IFormFile jsonFile, IFormFile keyJsonFile, IFormCollection form)
        {
            try
            {
                /// @brief Processes the uploaded files or form data and returns the verification result.
                var result = await _documentImportService.ProcessAsync(jsonFile, keyJsonFile, form);

                /// @brief Populates the view with the deserialized document data and verification state.
                ViewBag.Template = result.Template;
                ViewBag.Placeholders = result.Placeholders;
                ViewBag.SignedCombinations = result.SignedCombinations;
                ViewBag.PublicKey = result.PublicKey;
                ViewBag.OutputJson = result.OutputJson;
                ViewBag.Assinatura = result.Signature;
                ViewBag.AssinaturaValida = result.SignatureMatched;
                ViewBag.TextoFinal = result.TextoCompleto;
                ViewBag.TextoAssinado = result.TextoAssinado;

                /// @return Renders the view with result information.
                return View();
            }
            catch (Exception ex)
            {
                /// @brief Displays the error message if verification fails.
                ViewBag.OutputJson = "Error: " + ex.Message;

                /// @return Renders the view with error feedback.
                return View();
            }
        }
    }
}
