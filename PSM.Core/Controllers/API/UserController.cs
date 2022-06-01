using Microsoft.AspNetCore.Mvc;
using PSM.Core.Core;
using PSM.Core.Core.Auth;
using PSM.Core.Core.Database;
using PSM.Core.Models.API;

namespace PSM.Core.Controllers.API;

[Route("api/users")]
public class UserController : Controller {
  private readonly PSMContext     _dbc;
  private readonly IJWTRepository _jwt;

  public UserController(PSMContext dbc, IJWTRepository jwt) {
    _dbc = dbc;
    _jwt = jwt;
  }

  [HttpGet("list")]
  [ProducesResponseType(typeof(UserInformationModel[]), 200)]
  public IActionResult ListUsers() {
    var user = _jwt.UserFromContext(HttpContext);
    if(user is null || !user.PermissionSet.Contains(PSMPermission.UserList)) {
      return Forbid();
    }

    var users = _dbc.Users.Select(dbUser => new UserInformationModel {
                                                                       Username = dbUser.Username,
                                                                       Enabled  = dbUser.Enabled,
                                                                       UserID   = dbUser.Id
                                                                     }).ToArray();
    return Ok(users);
  }
}
