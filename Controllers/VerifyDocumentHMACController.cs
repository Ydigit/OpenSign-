using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenSign.Services;
using System.Text.RegularExpressions;

namespace OpenSign.Controllers
{
    ////@class VerifyDocumentHMACController
    ///@brief Controller responsible for verifying documents using HMAC signatures.
    ///@brief This controller handles both the upload of JSON templates containing placeholders and
    /// @brief the verification of user-submitted input against signed combinations using HMAC-SHA256.
    [Route("VerifyDocumentHMAC")]
    public class VerifyDocumentHMACController : Controller
    {
        private readonly HmacService _hmacService;

        ///@brief Constructor that initializes the HMAC service.
        public VerifyDocumentHMACController()
        {
            _hmacService = new HmacService();
        }

        [HttpGet("")]
        public IActionResult VerifyDocumentHMAC()
        {
            return View();
        }

        /// @brief Handles the POST request to verify a document using HMAC.
        ///
        /// Accepts a JSON file or form fields, reconstructs the message using user inputs,computes the HMAC, and checks it against signed combinations in the JSON file.
        /// @param jsonFile The uploaded JSON file (optional if form fields are used).
        /// @param form The form collection containing placeholders, HMAC key, and salt.
        /// @return A view showing whether the HMAC matched a valid signature.
         
        [HttpPost]
        public async Task<IActionResult> VerifyDocumentHMAC(IFormFile jsonFile, IFormCollection form)
        {
            ///@brief Allows to verify if the HMAC key is present and valid
            if (!form.TryGetValue("hmacKey", out var hmacKeyValue) || string.IsNullOrWhiteSpace(hmacKeyValue))
            {
                TempData["Error"] = "Missing HMAC key.";
                return View();
            }

            string hmacKey = hmacKeyValue!;

            ///@brief Checks if the JSON file was uploaded corretly
            if (jsonFile != null)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var jsonText = await reader.ReadToEndAsync();

                ///@brief CVerify if file is empty
                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    TempData["Error"] = "Empty .json content.";
                    return View();
                }

                 ///@brief Deserialize JSON file to obtain dynamic object
                var json = JsonConvert.DeserializeObject<dynamic>(jsonText);
                if (json == null)
                {
                    TempData["Error"] = "Error processing .json file.";
                    return View();
                }

                ///@brief Fill the ViewBag with the loaded values to build dynamic fields.
                ViewBag.Template = (string?)json.original ?? "";
                ViewBag.Placeholders = json.placeholders?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                ViewBag.SignedCombinations = json.signed_combinations ?? new Dictionary<string, object>();
                ViewBag.HmacKey = hmacKey;

                ///@brief Extracts salt from JSON file)
                string saltBase64 = json.salt != null ? (string)json.salt : "";
                ViewBag.SaltBase64 = saltBase64;

                ///@brief Show all the fields to fill (options and free text)
                return View();
            }
            else
            {
                ///@brief Data sent from the form
                if (!form.TryGetValue("template", out var templateValue) ||
                    !form.TryGetValue("signedCombinations", out var signedJsonValue)||
                    !form.TryGetValue("saltBase64", out var saltBase64Value))
                {
                    TempData["Error"] = "Formulary data missing.";
                    return View();
                }

                string template = templateValue!;
                string signedJson = signedJsonValue!;
                string saltBase64 = saltBase64Value!; 

                if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(signedJson))
                {
                    TempData["Error"] = "Obligatory fields are empty.";
                    return View();
                }

                var signedCombinations = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(signedJson);
                if (signedCombinations == null)
                {
                    TempData["Error"] = "Invalid Signatures.";
                    return View();
                }

                ///@brief Retrieve the values filled by the user (except internal fields)
                var respostas = form
                    .Where(k => k.Key != "template" && k.Key != "signedCombinations" && k.Key != "hmacKey" && !k.Key.StartsWith("__"))
                    .ToDictionary(k => k.Key, k => k.Value.ToString());

                ///@brief Redo the final text by replacing all placeholders with the filled values
                string textoComFreeText = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
                {
                    var key = m.Groups[1].Value;
                    return respostas.ContainsKey(key) ? respostas[key] : "";
                });

                ///@brief Aplly all the placeholders who need to be signed, ignoring free text fields and do HMAC again.
                string textoParaHmac = Regex.Replace(template, @"\[(\@?\w*)(:[^\]]*)?\]", m =>
                {
                    var key = m.Groups[1].Value;
                    var temOpcoes = m.Groups[2].Success;

                    if (temOpcoes && respostas.ContainsKey(key))
                        return respostas[key];

                    ///@brief Ignore Free Text    
                    return "";
                });

                byte[] salt = Convert.FromBase64String(saltBase64);

                ///@brief Sign again with HMAC using key and salt provide
                string hmacHex = _hmacService.CalcularHmac(textoParaHmac, hmacKey, salt);

                ///@brief Verify if the HMAC signature exists in the signed combinations (present if JSON file)
                bool assinaturaValida = signedCombinations.ContainsKey(hmacHex);
                string? assinatura = assinaturaValida ? signedCombinations[hmacHex].hmac : null;

                ///@brief JSON for output if needed
                var outputJson = new
                {
                    selected_text = textoComFreeText,
                    inputs = respostas,
                    hmac = hmacHex,
                    signature = assinatura,
                    signature_matched = assinaturaValida,
                    salt = saltBase64,
                    signature_algorithm = "HMAC-SHA256 (hex)"
                };

                ViewBag.OutputJson = JsonConvert.SerializeObject(outputJson, Formatting.Indented);
                ViewBag.AssinaturaValida = assinaturaValida;
                ViewBag.TextoFinal = textoComFreeText;

                return View();
            }
        }
    }
}
