using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using OpenSign.Services;
using System.Text.Json;


namespace OpenSign.Controllers
{
    [Route("Import")]
    public class ImportController : Controller
    {
        private readonly DecryptCBCService _decryptServiceCBC;
        private readonly DecryptionCTRService _decryptServiceCTR;


        public ImportController()
        {
            _decryptServiceCBC = new DecryptCBCService();
            _decryptServiceCTR= new DecryptionCTRService();

        }

        // GET /Import
        [HttpGet("")]
        public IActionResult Import()
        {
            return View();
        }

        // POST /Import/Decifrar
        [HttpPost("Decifrar")]
        public async Task<IActionResult> Decifrar(IFormFile keyFile, string pss)
        {
            if (keyFile == null || string.IsNullOrEmpty(pss))
            {
                ViewBag.Message = "Erro: Ficheiro ou password inválidos!";
                return View("Import");
            }

            try
            {

                // Salvar o arquivo temporariamente
                var tempFilePath = Path.GetTempFileName();
                using (var stream = System.IO.File.Create(tempFilePath))
                {
                    await keyFile.CopyToAsync(stream); //Passage da var que vem do formulario para o pat q foi criado
                }

                // Ler o JSON do arquivo
                string jsonContent = await System.IO.File.ReadAllTextAsync(tempFilePath);
                var keyData = JsonSerializer.Deserialize<KeyDataModel>(jsonContent);

                if (keyData == null || string.IsNullOrEmpty(keyData.CipherMode))
                {
                    throw new Exception("O arquivo JSON está malformado ou não contém o modo de cifração.");
                }

                // Escolher o serviço de decifração com base no modo
                string decryptedPrivateKey;
                if (keyData.CipherMode == "aes-256-cbc")
                {
                    //entra com o ficheiro json temporario
                    decryptedPrivateKey = _decryptServiceCBC.DecryptPrivateKeyFromJson(tempFilePath, pss);
                }
                else if (keyData.CipherMode == "aes-256-ctr")
                {
                    //entra com o ficheiro json temporario
                    decryptedPrivateKey = _decryptServiceCTR.DecryptPrivateKeyFromJson(tempFilePath, pss);
                }
                else
                {
                    throw new Exception("Modo de cifração desconhecido no arquivo JSON.");
                }

                // Limpar o arquivo temporário
                System.IO.File.Delete(tempFilePath);

                ViewBag.Message = "Secret Key decifrada com sucesso!";
                ViewBag.DecryptedPrivateKey = decryptedPrivateKey;

            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Erro ao decifrar: {ex.Message}";
            }

            return View("Import");
        }
    }
}