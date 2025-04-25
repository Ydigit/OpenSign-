using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    public class KeysController : Controller
    {
        public IActionResult Generate()
        {
            return View();
        }

        public IActionResult Import()
        {
            return View();
        }
    }
}
