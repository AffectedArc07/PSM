using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSM.Core.Core.Auth;
using PSM.Core.Models;
using PSM.Core.Models.Server;

namespace PSM.Core.Controllers.API {
  /// <summary>
  /// Controller to get service meta information
  /// </summary>
  [Authorize]
  [Route("api/meta")]
  public class MetaController : Controller {
    private readonly IJWTRepository _auth;

    public MetaController(IJWTRepository auth) {
      _auth = auth;
    }

    /// <summary>
    /// Get the server & current info 
    /// </summary>
    /// <returns>A user's username, or an error if their token is invalid.</returns>
    [HttpGet("info")]
    public IActionResult GetUsername() {
      var user = _auth.UserFromContext(HttpContext);
      if(user == null)
        return Unauthorized("Your user either doesn't exist, or is disabled.");

      var serverInfoModel = new ServerInfoModel {
                                                  CurrentUsername = user.Username,
                                                  ServerVersion   = typeof(Program).Assembly.GetName().Version.ToString()
                                                };
      return Ok(serverInfoModel);
    }
  }
}
