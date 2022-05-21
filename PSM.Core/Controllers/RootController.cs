using Microsoft.AspNetCore.Mvc;

namespace PSM.Core.Controllers {
    /// <summary>
    /// Index controller. This will redirect you to the app static files dir.
    /// </summary>
    [Route("")]
    public class RootController : Controller {
        [HttpGet]
        public IActionResult Index() {
            return Redirect("app");
        }
    }
}
