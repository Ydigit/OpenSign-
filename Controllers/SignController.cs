/// @file SignController.cs
/// @brief Controller responsible for handling the signing view and workflow.using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    /// @class SignController
    /// @brief Handles routing to the sign page.
    ///
    /// This controller manages the route "/Sign" and displays the signing interface.
    [Route("Sign")]///< @note This is the base route for all actions in this controller.
    public class SignController : Controller
    {
        /// @brief Displays the signing interface view.
        ///
        /// This action is triggered when a GET request is made to "/Sign".
        /// @return A view result corresponding to the signing interface.
        [HttpGet("")]///< @note Maps GET requests to /Sign to this action.
        {
            return View();///< Returns the Sign view to the user.
    }
    }
}