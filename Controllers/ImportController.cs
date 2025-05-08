using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    [Route("Import")]
    public class ImportController : Controller
    {
        // GET /Import
        [HttpGet("")]
        public IActionResult Import()
        {
            return View();
        }

        // POST /Import/Assinar
        [HttpPost("Assinar")]
        public async Task<IActionResult> Assinar(IFormFile keyFile, string password)
        {
            // Simula o processo de assinatura
            await Task.Delay(1000); // Simula algum processamento

            // Verifica se a chave foi assinada corretamente
            // Aqui pode adicionar a lógica de validação real, se necessário
            if (keyFile != null && password != null)
            {
                ViewBag.Message = "Arquivo assinado com sucesso!";
            }
            else if (keyFile != null && password == "1")
            {
                ViewBag.Message = "Teste teste!";
            }
            else
            {
                ViewBag.Message = "Erro ao assinar o arquivo. Verifique os dados!";
            }

            // Retorna a mesma view com a mensagem
            return View("Import");
        }
    }
}
