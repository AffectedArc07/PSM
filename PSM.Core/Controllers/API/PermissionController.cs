using Microsoft.AspNetCore.Mvc;
using PSM.Core.Core;
using PSM.Core.Core.Auth;
using PSM.Core.Core.Database;
using PSM.Core.Models.API;

namespace PSM.Core.Controllers.API;

[Route("api/permission")]
public class PermissionController : Controller {
  private readonly PSMContext     _psmContext;
  private readonly IJWTRepository _jwtRepository;

  public PermissionController(PSMContext psm, IJWTRepository jwt) {
    _psmContext    = psm;
    _jwtRepository = jwt;
  }

  [HttpGet("list")]
  [ProducesResponseType(typeof(PermissionInformationModel[]), 200)]
  public IActionResult ListPermissions() {
    return Ok(Enum.GetValues<PSMPermission>().Select(permission => permission.GetInformationModel()).ToArray());
  }

  [HttpPut("{userID:int}")]
  public IActionResult UpdateUserPermissions(PermissionUpdateModel permissions, int userID) {
    if(!permissions.ValidateModel() || permissions.UserId != userID)
      return Problem("Model did not validate");

    if(_jwtRepository.UserFromContext(HttpContext) is not { } user)
      return Problem("Unable to locate originator information");

    if(!_psmContext.CheckPermission(user, PSMPermission.UserModify))
      return Unauthorized("You are not authorized to modify users");

    if(_psmContext.Users.Find(userID) is not { } targetUser)
      return NotFound("Target user not found");

    targetUser.PermissionSet.PermissionString = permissions.NewPermissions;
    _psmContext.SaveChanges();
    return Ok();
  }

  [HttpGet("list/{userID:int}")]
  [ProducesResponseType(typeof(PermissionInformationModel[]), 200)]
  public IActionResult GetUserPermissions(int userID) {
    if(_jwtRepository.UserFromContext(HttpContext) is not { } user || !_psmContext.CheckPermission(user, PSMPermission.UserModify))
      return Forbid();
    if(_psmContext.PermissionSets.Find(userID) is not { } userPermissions)
      return NotFound();
    return Ok(userPermissions.AsList().Select(permission => permission.GetInformationModel()).ToArray());
  }
}
