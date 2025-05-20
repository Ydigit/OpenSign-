using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using OpenSign.Services;
using System.Text.Json;

namespace PlaceholderTextApp.Controllers
{
    [Route("CreateDocument")]
    public class CreateDocumentController : Controller
    {
        [HttpGet]
        public IActionResult CreateDocument()
        {
            return View();
        }

        private readonly DecryptCBCService _decryptServiceCBC;
        private readonly DecryptionCTRService _decryptServiceCTR;


        public CreateDocumentController()
        {
            _decryptServiceCBC = new DecryptCBCService();
            _decryptServiceCTR = new DecryptionCTRService();

        }

        [HttpPost("GerarEAssinarJson")]
        public async Task<IActionResult> GerarEAssinarJson(IFormCollection form, IFormFile keyFile)
        {
            string? textoInput = form["novoInput"];
            string? password = form["pss"];

            if (string.IsNullOrWhiteSpace(textoInput) || string.IsNullOrWhiteSpace(password))
                return BadRequest("Texto ou senha inválido.");

            if (keyFile == null || keyFile.Length == 0)
                return BadRequest("Arquivo de chave não fornecido.");

            RSA rsa;
            try
            {
                // Pass the IFormFile directly to your decrypt method
                rsa = await Decifrar(keyFile, password);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erro ao decifrar a chave: {ex.Message}";
                return RedirectToAction("CreateDocument");
            }

            var resultadoJson = GerarJsonAssinaturas(textoInput!, rsa);
            var jsonString = JsonConvert.SerializeObject(resultadoJson, Formatting.Indented);
            var bytes = Encoding.UTF8.GetBytes(jsonString);
            var fileName = "assinaturas.json";

            TempData["Success"] = "Documento gerado com sucesso!";
            return File(bytes, "application/json", fileName);
        }

        private object GerarJsonAssinaturas(string texto, RSA rsa)
        {
            var regex = new Regex(@"\[([^\]]*)\]");
            var matches = regex.Matches(texto);

            var placeholders = new Dictionary<string, object>();
            var fixedOptionValues = new List<List<string>>();
            var fixedPlaceholders = new List<string>();

            foreach (Match match in matches)
            {
                string conteudo = match.Groups[1].Value;

                if (conteudo.Contains(":"))
                {
                    var partes = conteudo.Split(":", 2);
                    string nome = partes[0].Trim();
                    var opcoes = partes[1]
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToList();

                    if (opcoes.Count > 0)
                    {
                        placeholders[nome] = opcoes;
                        fixedOptionValues.Add(opcoes);
                        fixedPlaceholders.Add(match.Value);
                    }
                    else
                    {
                        placeholders[nome] = "Free Text";
                    }
                }
                else
                {
                    string nome = conteudo.Trim();
                    placeholders[nome] = "Free Text";
                }
            }

            var combinacoes = GerarCombinacoes(fixedOptionValues);
            var combinacoesAssinadas = new Dictionary<string, object>();

            foreach (var combinacao in combinacoes)
            {
                string textoFinal = texto;
                for (int i = 0; i < fixedPlaceholders.Count; i++)
                {
                    textoFinal = textoFinal.Replace(fixedPlaceholders[i], combinacao[i]);
                }

                byte[] dados = Encoding.UTF8.GetBytes(textoFinal);
                using SHA256 sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(dados);
                string hashBase64 = Convert.ToBase64String(hash);

                byte[] assinatura = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                string assinaturaBase64 = Convert.ToBase64String(assinatura);

                combinacoesAssinadas[hashBase64] = new
                {
                    text = textoFinal,
                    signature = assinaturaBase64
                };
            }

            return new
            {
                original = texto,
                placeholders = placeholders,
                signed_combinations = combinacoesAssinadas
            };
        }

        private List<List<string>> GerarCombinacoes(List<List<string>> listas)
        {
            var resultado = new List<List<string>> { new List<string>() };

            foreach (var lista in listas)
            {
                var temp = new List<List<string>>();
                foreach (var prefixo in resultado)
                {
                    foreach (var item in lista)
                    {
                        var nova = new List<string>(prefixo) { item };
                        temp.Add(nova);
                    }
                }
                resultado = temp;
            }

            return resultado;
        }

        public async Task<RSA> Decifrar(IFormFile keyFile, string pss)
        {
            if (keyFile == null || string.IsNullOrEmpty(pss))
            {
                throw new ArgumentException("Erro: Ficheiro ou password inválidos!");
            }

            try
            {
                // Salvar o arquivo temporariamente
                var tempFilePath = Path.GetTempFileName();
                using (var stream = System.IO.File.Create(tempFilePath))
                {
                    await keyFile.CopyToAsync(stream);
                }

                // Ler o JSON do arquivo
                string jsonContent = await System.IO.File.ReadAllTextAsync(tempFilePath);
                var keyData = System.Text.Json.JsonSerializer.Deserialize<KeyDataModel>(jsonContent);

                if (keyData == null || string.IsNullOrEmpty(keyData.CipherMode))
                {
                    throw new Exception("O arquivo JSON está malformado ou não contém o modo de cifração.");
                }

                // Escolher o serviço de decifração com base no modo
                string decryptedPrivateKey;
                if (keyData.CipherMode == "aes-256-cbc")
                {
                    decryptedPrivateKey = _decryptServiceCBC.DecryptPrivateKeyFromJson(tempFilePath, pss);
                }
                else if (keyData.CipherMode == "aes-256-ctr")
                {
                    decryptedPrivateKey = _decryptServiceCTR.DecryptPrivateKeyFromJson(tempFilePath, pss);
                }
                else
                {
                    throw new Exception("Modo de cifração desconhecido no arquivo JSON.");
                }

                //aqui tenho de pegar

                // Limpar o arquivo temporário
                System.IO.File.Delete(tempFilePath);

                // Criar o objeto RSA a partir da chave privada decifrada (em formato PEM)
                RSA rsa = RSA.Create();
                rsa.ImportFromPem(decryptedPrivateKey.ToCharArray());
                return rsa;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao decifrar: {ex.Message}");
            }
        }
    }
}
