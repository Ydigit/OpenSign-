//Queres gerar chaves ou criar o doc, ja com chaves
using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    [Route("Verify")]//esta e a noma rota base do controllador
    public class VerifyPageController : Controller
    {
        //QUANDO ACEDER AO ROUTE SIGN, VAI ACEDER AO SIGNVIEW E RETORNA 
        [HttpGet("")]// vai definir que a action SignView ser� chamada quando a URL /Sign for acessada.
        public IActionResult VerifyPageView()
        {
            return View();
        }
    }
}


//Controller para poder ter uma action que retorna a respetica view