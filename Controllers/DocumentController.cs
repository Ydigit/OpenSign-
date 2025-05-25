/// @file DocumentController.cs
/// @brief Controller responsible for handling document creation requests.

using Microsoft.AspNetCore.Mvc;
using OpenSign.Models;

namespace OpenSign.Controllers
{
    /// @class DocumentController
    /// @brief Handles the creation of new documents with optional placeholder values.
    ///
    /// Provides both GET and POST endpoints for rendering the document creation view
    /// and processing user-submitted document content.
    public class DocumentController : Controller
    {
        /// @brief Displays the document creation form to the user.
        ///
        /// @return The CreateDocument view where the user can enter a document.
        [HttpGet]
        public IActionResult CreateDocument()
        {
            return View();
        }

        /// @brief Handles the submission of a new document from the user.
        ///
        /// Validates the input model, saves the document to disk, and notifies the user of success or failure.
        ///
        /// @param model The document data submitted from the form, including text and placeholders.
        /// @return A redirection to the same view on success, or the form with validation errors or exceptions.
        [HttpPost]
        public IActionResult CreateDocument(DocumentCreateModel model)
        {
            // If the model is invalid (e.g., required fields are empty), show error message
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid input. Please check the fields.";
                return View(model);
            }

            try
            {
                /// @brief Simulates document creation and stores it in the local file system.
                ///
                /// Creates a directory named "GeneratedDocuments" (if it doesn't exist) and
                /// stores the plain text document with a timestamped filename.

                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "GeneratedDocuments");
                Directory.CreateDirectory(outputPath);

                string fileName = $"documento_{DateTime.Now.Ticks}.txt";
                string fullPath = Path.Combine(outputPath, fileName);

                System.IO.File.WriteAllText(fullPath, model.DocumentText);

                TempData["Success"] = $"Document created successfully: {fileName}";
                return RedirectToAction("CreateDocument");
            }
            catch (Exception ex)
            {
                /// @warning Catches unexpected exceptions during file creation (e.g., permission denied, disk full).
                TempData["Error"] = $"Error: {ex.Message}";
                return View(model);
            }
        }
    }
}
