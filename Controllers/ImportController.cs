using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    [Route("Import")]//esta e a noma rota base do controllador
    public class ImportController : Controller
    {
        //QUANDO ACEDER AO ROUTE SIGN, VAI ACEDER AO SIGNVIEW E RETORNA A VIEW
        [HttpGet("")]// vai definir que a action SignView será chamada quando a URL /Sign for acessada.
        public IActionResult Import()
        {
            return View();
        }
    }
}


//Controller para poder ter uma action que retorna a respetica view