/// @file KeysController.cs
/// @brief Controller responsible for generating RSA key pairs and delivering them to the user.

using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    /// @class KeysController
    /// @brief Handles RSA key pair generation and packaging.
    ///
    /// This controller allows users to request key generation with custom parameters and download the keys.
    [Route("Generate")]
    public class KeysController : Controller
    {
        private readonly KeyService _keyService;

        /// @brief Constructor with dependency injection of the key generation service.
        /// @param keyService The service that handles RSA key pair creation and encryption.
        public KeysController(KeyService keyService)
        {
            _keyService = keyService;
        }

        /// @brief Displays the key generation view.
        /// @return The view that allows users to input parameters for key generation.
        [HttpGet("")]
        public IActionResult Generate()
        {
            return View();
        }

        /// @brief Handles POST requests for RSA key generation.
        ///
        /// Validates parameters and either shows a success message or returns a ZIP file containing the keys, this ZIP file should be maintained locally (Check attack diagram!).
        ///
        /// @param keySize The RSA modulus size (must be 2048, 3072 or 4096).
        /// @param encmode The symmetric encryption mode used to encrypt the private key (e.g., AES-256-CBC).
        /// @param pss The password used for key derivation and encryption.
        /// @param keys Determines whether to proceed with the key generation and packaging (e.g., "genKeys" for generating both keys).
        /// @return Either a view with success/error messages or a downloadable ZIP archive with the key files.
        [HttpPost("")]
        public IActionResult Generate(int keySize, string encmode, string pss, string keys){
            if (keySize != 2048 && keySize != 3072 && keySize != 4096 )
            {
                //Handle invalid key size
                TempData["Error"] = "Invalid public key size.";
                return View();
            }


            // Generate RSA key pair and encrypt the private key
            var jsonPath = _keyService.GenerateRSAKeyPairJSON(keySize, pss, encmode);

            // If the user requested to receive the key files as a ZIP archive
            if (keys.Equals("genKeys")){
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)){
                        archive.CreateEntryFromFile(jsonPath.jsonfilePath, Path.GetFileName(jsonPath.jsonfilePath));
                        archive.CreateEntryFromFile(jsonPath.pubfilePath, Path.GetFileName(jsonPath.pubfilePath));
                    }

                    memoryStream.Seek(0, SeekOrigin.Begin);//point to the begining of the memory 
                    return File(memoryStream.ToArray(), "application/zip", "rsa_keypair.zip");
                }
            }
            TempData["Success"] = $"Keys {encmode.ToUpper()} of {keySize} bits generated with success.";
            return View();
        }
    }
}
