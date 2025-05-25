using Microsoft.AspNetCore.Mvc;

namespace OpenSign.Controllers
{
    /**
     * @class VerifyPageController
     * @brief Controller responsible for rendering the verification landing page.
     *
     * This controller handles the route used to display the main page where the user
     * can choose to generate keys or verify documents with digital signatures or HMACs.
     */

    ///@brief Base route for this controller
    [Route("VerifyPage")]
    public class VerifyPageController : Controller
    {
        /**
        * @brief Displays the verification options page.
        *
        * This method renders the view where the user chooses between generating keys
        * or uploading/verifying a document.
        *
        * @return The VerifyPage view.
        */
         
        ///@brief Called when accessing the /VerifyPage route
        [HttpGet("")]
        public IActionResult VerifyPage()
        {
            return View();
        }
    }
}