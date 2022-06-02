using Microsoft.AspNetCore.Mvc;
using PSM.Core.Core;
using PSM.Core.Core.Auth;
using PSM.Core.Core.Database;
using PSM.Core.Models.API;
using PSM.Core.Models.Database;

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

  [HttpGet("{userID:int}")]
  public IActionResult GetUser(int userID) {
    if(_dbc.GetUser(userID) is not { } user)
      return NotFound();
    return Ok(user.GetInformationModel());
  }

  [HttpGet("whoami")]
  [ProducesResponseType(typeof(UserInformationModel), 200)]
  public IActionResult WhoAmI() {
    if(_jwt.UserFromContext(HttpContext) is { } user)
      return Ok(new UserInformationModel { Username = user.Username, Enabled = user.Enabled, UserID = user.Id });
    return NotFound();
  }

  [HttpPost("create")]
  [ProducesResponseType(typeof(UserInformationModel), 200)]
  public IActionResult CreateUser() {
    if(_jwt.UserFromContext(HttpContext) is not { } user)
      return Forbid();
    if(!HttpContext.Request.Form.TryGetValue("username", out var username) || username.Count != 1)
      return Problem();
    if(_dbc.CreatePSMUser(user, new User {
                                           Username     = username[0],
                                           CreatedBy    = user,
                                           Enabled      = false,
                                           PasswordHash = "_"
                                         }) !=
       PSMResponse.Ok)
      return Problem();
    return Ok(_dbc.Users.First(dbUser => dbUser.Username.Equals(username)).GetInformationModel());
  }

  [HttpPut("{userID:int}")]
  public IActionResult UpdateUserDetails([FromBody] UserUpdateModel userUpdate, int userID) {
    var form = new StreamReader(HttpContext.Request.Body).ReadToEndAsync().GetAwaiter().GetResult();
    if(_jwt.UserFromContext(HttpContext) is not { } user || !_dbc.CheckPermission(user, PSMPermission.UserModify))
      return Forbid();
    if(_dbc.GetUser(userID) is not { } target)
      return NotFound();
    if(target.Id == user.Id)
      return Conflict();
    if(target.Enabled != userUpdate.enabled && !_dbc.CheckPermission(user, PSMPermission.UserEnable))
      return Forbid();

    var current   = target.PermissionSet.AsList();
    var expected  = userUpdate.permissions.ConvertToPermissionList();
    var unchanged = current.Intersect(expected);
    var aggregate = current.Concat(expected).DistinctBy(p => (int)p).ToList();
    aggregate.RemoveAll(unchanged.Contains);
    if(!aggregate.All(p => _dbc.CheckPermission(user, p)))
      return Forbid();
    target.PermissionSet.PermissionString = expected.ConvertToPermissionString();

    target.Enabled = userUpdate.enabled;
    _dbc.SaveChanges();
    return Ok();
  }
}
