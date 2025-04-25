using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            //TempData["Success"] = "Sucesso";
            //TempData["Error"] = "Erro";

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


