using Microsoft.AspNetCore.Mvc;
using PSM.Core.Core;
using PSM.Core.Core.Auth;
using PSM.Core.Core.Database;
using PSM.Core.Models.API;

namespace PSM.Core.Controllers.API;

[Route("api/permission")]
public class PermissionController : Controller {
  private PSMContext     _psmContext;
  private IJWTRepository _jwtRepository;

  public PermissionController(PSMContext _psm, IJWTRepository _jwt) {
    _psmContext    = _psm;
    _jwtRepository = _jwt;
  }

  [HttpGet("list")]
  [ProducesResponseType(typeof(PermissionModel[]), 200)]
  public IActionResult ListPermissions() {
    return Ok(Constants.AllPermissions());
  }

  [HttpPut("{userID:int}")]
  public IActionResult UpdateUserPermissions(PermissionUpdateModel permissions, int userID) {
    if(!permissions.ValidateModel() || permissions.UserId != userID)
      return Problem("Model did not validate");

    if(_jwtRepository.UserFromContext(HttpContext) is not { } user)
      return Problem("Unable to locate originator information");

    if(!user.PermissionSet.Contains(PSMPermission.UserModify))
      return Unauthorized("You are not authorized to modify users");

    if(_psmContext.Users.Find(userID) is not { } targetUser)
      return Conflict("Target user not found");

    targetUser.PermissionSet.PermissionString = permissions.NewPermissions;
    _psmContext.SaveChanges();
    return Ok();
  }
}
