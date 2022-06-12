using Microsoft.AspNetCore.Mvc;
using PSM.Core.Auth;
using PSM.Core.Database;
using PSM.Core.Models.API;

namespace PSM.Core.Controllers.API;

[Route("api/users")]
public class UserController : Controller {
  private readonly UserContext    _dbc;
  private readonly IJWTRepository _jwt;

  public UserController(UserContext dbc, IJWTRepository jwt, PermissionContext psm) {
    _dbc = dbc.WithPermissionContext(psm);
    _jwt = jwt;
  }

  [HttpGet("list")]
  [ProducesResponseType(typeof(UserInformationModel[]), 200)]
  public async Task<IActionResult> ListUsers() {
    return Ok((await _dbc.GetAllUsers()).Select(Constants.GetInformationModel));
  }

  [HttpGet("{userID:int}")]
  public async Task<IActionResult> GetUser(int userID) {
    if(await _dbc.GetUser(userID) is not { } user)
      return NotFound();
    return Ok(user.GetInformationModel());
  }

  [HttpGet("whoami")]
  [ProducesResponseType(typeof(UserInformationModel), 200)]
  public async Task<IActionResult> WhoAmI() {
    if(await _jwt.UserFromContext(HttpContext) is { } user)
      return Ok(new UserInformationModel { Username = user.Username, Enabled = user.Enabled, UserID = user.Id });
    return NotFound();
  }

  [HttpPost("create")]
  [ProducesResponseType(typeof(UserInformationModel), 200)]
  public async Task<IActionResult> CreateUser() {
    if(await _jwt.UserFromContext(HttpContext) is not { } user)
      return Forbid();
    if(!HttpContext.Request.Form.TryGetValue("username", out var username) || username.Count != 1)
      return Problem();
    return Ok((await _dbc.CreateUser(username, user)).GetInformationModel());
  }

  [HttpDelete("{userID:int}")]
  public async Task<IActionResult> ArchiveUser(int userID) {
    if(await _jwt.UserFromContext(HttpContext) is not { } user || !user.GlobalPermissionSet.CheckPermission(PSMPermission.UserArchive))
      return Forbid();
    if(await _dbc.GetUser(userID) is not { } target)
      return NotFound();
    if(target.Id == user.Id || target.Id is Constants.System.SystemUserID or Constants.System.AdminUserID)
      return Conflict();
    target.Archived = !target.Archived;
    await _dbc.SaveChangesAsync();
    return target.Archived ? Ok() : StatusCode(201); // 201 - Created
  }

  [HttpPatch("{userID:int}")]
  public async Task<IActionResult> UpdateUserDetails([FromBody] UserUpdateModel userUpdate, int userID) {
    if(await _jwt.UserFromContext(HttpContext) is not { } user || !user.GlobalPermissionSet.CheckPermission(PSMPermission.UserEdit))
      return Forbid();
    if(await _dbc.GetUser(userID) is not { } target)
      return NotFound();
    if(target.Id == user.Id)
      return Conflict();
    if(target.Enabled != userUpdate.enabled && !user.GlobalPermissionSet.CheckPermission(PSMPermission.UserEnable))
      return Forbid();
    if(!target.Username.Equals(userUpdate.username)) {
      if(!user.GlobalPermissionSet.CheckPermission(PSMPermission.UserRename))
        return Forbid();
      if(await _dbc.GetUserByUsername(userUpdate.username) is { })
        return Conflict();
    }

    var current   = target.GlobalPermissionSet.AsList();
    var expected  = userUpdate.permissions.ConvertToPermissionList();
    var unchanged = current.Intersect(expected);
    var aggregate = current.Concat(expected).DistinctBy(p => (int)p).ToList();
    aggregate.RemoveAll(unchanged.Contains);
    if(!aggregate.All(p => user.GlobalPermissionSet.CheckPermission(p)))
      return Forbid();

    target.GlobalPermissionSet.FromList(expected);
    target.Enabled  = userUpdate.enabled;
    target.Username = userUpdate.username;
    await _dbc.SaveChangesAsync();
    return Ok();
  }
}
