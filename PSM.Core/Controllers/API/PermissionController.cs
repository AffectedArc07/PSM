using Microsoft.AspNetCore.Mvc;
using PSM.Core.Auth;
using PSM.Core.Database;
using PSM.Core.Models.API;

namespace PSM.Core.Controllers.API;

[Route("api/permission")]
public class PermissionController : Controller {
  private readonly UserContext       _userContext;
  private readonly IJWTRepository    _jwtRepository;

  public PermissionController(UserContext userContext, IJWTRepository jwt) {
    _userContext       = userContext;
    _jwtRepository     = jwt;
  }

  [HttpGet("list")]
  [ProducesResponseType(typeof(PermissionInformationModel[]), 200)]
  public IActionResult ListPermissions() {
    return Ok(Enum.GetValues<PSMPermission>().Select(permission => permission.GetInformationModel()).ToArray());
  }

  [HttpPut("{userID:int}")]
  public async Task<IActionResult> UpdateUserPermissions(PermissionUpdateModel permissions, int userID) {
    if(await _jwtRepository.UserFromContext(HttpContext) is not { } user)
      return Problem("Unable to locate originator information");

    if(!user.GlobalPermissionSet.CheckPermission(PSMPermission.UserEdit))
      return Unauthorized("You are not authorized to modify users");

    if(await _userContext.GetUser(userID) is not { } targetUser)
      return NotFound("Target user not found");

    targetUser.GlobalPermissionSet.PermissionString = permissions.NewPermissions;
    await _userContext.SaveChangesAsync();
    return Ok();
  }

  [HttpGet("list/{userID:int}")]
  [ProducesResponseType(typeof(PermissionInformationModel[]), 200)]
  public async Task<IActionResult> GetUserPermissions(int userID) {
    if(await _jwtRepository.UserFromContext(HttpContext) is not { } user || !user.GlobalPermissionSet.CheckPermission(PSMPermission.UserEdit))
      return Forbid();
    if(await _userContext.GetUser(userID) is not { } dbUser)
      return NotFound();
    return Ok(dbUser.GlobalPermissionSet.AsList().Select(permission => permission.GetInformationModel()).ToArray());
  }
}
