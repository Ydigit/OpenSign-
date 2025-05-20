using Microsoft.AspNetCore.Mvc;
using OpenSign.Models;

namespace OpenSign.Controllers
{
    public class DocumentController : Controller
    {
        // GET: Document/CreateDocument
        [HttpGet]
        public IActionResult CreateDocument()
        {
            return View();
        }

        // POST: Document/CreateDocument
        [HttpPost]
        public IActionResult CreateDocument(DocumentCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid input. Please check the fields.";
                return View(model);
            }

            try
            {
                // ✅ Simular criação do documento e assinaturas
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
                TempData["Error"] = $"Error: {ex.Message}";
                return View(model);
            }
        }
    }
}
