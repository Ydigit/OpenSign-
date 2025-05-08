using Microsoft.AspNetCore.Mvc;  // Only this one

namespace OpenSign.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

    }
}
