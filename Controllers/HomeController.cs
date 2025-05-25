/// @file HomeController.cs
/// @brief Default controller for rendering static views like homepage, help, and error pages.

using Microsoft.AspNetCore.Mvc;  ///< Provides controller base class and action result types

namespace OpenSign.Controllers
{
    /// @class HomeController
    /// @brief Handles navigation to general/static pages of the OpenSign application.
    ///
    /// This includes the homepage, help section, about page, and error page.
    public class HomeController : Controller
    {
        /// @brief Displays the main landing page.
        /// @return The Index view.
        public IActionResult Index()
        {
            return View();
        }

        /// @brief Displays the help or "how it works" section.
        /// @return The Help view.
        public IActionResult Help()
        {
            return View();
        }

        /// @brief Displays a general error page.
        /// @return The Error view.
        public IActionResult Error()
        {
            return View();
        }

        /// @brief Displays information about the developers and the project.
        /// @return The About Us view.
        public IActionResult AboutUs()
        {
            return View();
        }
    }
}
