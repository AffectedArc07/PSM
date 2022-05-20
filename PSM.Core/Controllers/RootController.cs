using Microsoft.AspNetCore.Mvc;

namespace PSM.Core.Controllers {
    /// <summary>
    /// Index controller. This will show the view for the control webapp, when its made.
    /// </summary>
    [Route("")]
    public class RootController : Controller {
        [HttpGet]
        public IActionResult Index() {
            return View();
        }
    }
}
