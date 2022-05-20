using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSM.Core.Core.Auth;
using PSM.Core.Models;

namespace PSM.Core.Controllers.API {
    /// <summary>
    /// Controller to get service meta information
    /// </summary>
    [Authorize]
    [Route("api/meta")]
    public class MetaController : Controller {
        private readonly IJWTRepository auth;
        public MetaController(IJWTRepository _auth) {
            auth = _auth;
        }

        /// <summary>
        /// Get the server & current info 
        /// </summary>
        /// <returns>A user's username, or an error if their token is invalid.</returns>
        [HttpGet("info")]
        public IActionResult GetUsername() {
            User? user = auth.UserFromContext(HttpContext);
            if (user == null) {
                return Unauthorized("Your user either doesn't exist, or is disabled.");
            }

            ServerInfoModel SIM = new ServerInfoModel() { current_username = user.Username, server_version = typeof(Program).Assembly.GetName().Version.ToString() };
            return Ok(SIM);
        }
    }
}
