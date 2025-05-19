//Queres gerar chaves ou criar o doc, ja com chaves
using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    [Route("Sign")]//esta e a noma rota base do controllador
    public class SignController : Controller
    {
        //QUANDO ACEDER AO ROUTE SIGN, VAI ACEDER AO SIGNVIEW E RETORNA 
        [HttpGet("")]// vai definir que a action SignView será chamada quando a URL /Sign for acessada.
        public IActionResult SignView()
        {
            return View();
        }
    }
}


//Controller para poder ter uma action que retorna a respetica view