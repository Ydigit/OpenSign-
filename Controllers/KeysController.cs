using Microsoft.AspNetCore.Mvc;

namespace OpenSign_.Controllers
{
    public class KeysController : Controller
    {
        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }
    }
}