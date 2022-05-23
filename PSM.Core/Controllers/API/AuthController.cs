using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PSM.Core.Core.Database;
using System.Net.Http.Headers;
using System.Text;
using PSM.Core.Core.Auth;
using PSM.Core.Models.Auth;
using PSM.Core.Models.Database;

namespace PSM.Core.Controllers.API {
  [Route("api/auth")]
  public class AuthController : Controller {
    private readonly PSMContext           _dbc;
    private readonly PasswordHasher<User> _hasher;
    private readonly IJWTRepository       _auth;

    public AuthController(PSMContext dbc, IJWTRepository auth) {
      _dbc    = dbc;
      _hasher = new PasswordHasher<User>();
      _auth   = auth;
    }

    /// <summary>
    /// Takes a username and password using basic auth, and will return a token and expiration timestamp.
    /// </summary>
    /// <returns>A token response or reason for token refusal</returns>
    [HttpPost]
    [Route("login")]
    [ProducesResponseType(typeof(ClientTokenModel), 200)]
    public IActionResult Login() {
      // Make sure they set the headers
      if(!Request.Headers.ContainsKey("Authorization"))
        return Unauthorized("No Authorization header! Please authenticate with basic auth, using your username and password");

      string authStr = Request.Headers["Authorization"];
      if(!authStr.StartsWith("Basic"))
        return Unauthorized("Invalid auth header! Must be basic auth!");

      var authHeader = AuthenticationHeaderValue.Parse(authStr);
      if(authHeader.Parameter == null)
        return Unauthorized("Invalid auth header!");

      // Get the info from basic auth
      var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
      var credentials     = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
      var username        = credentials[0];
      var password        = credentials[1];

      if(string.IsNullOrEmpty(username) || username.Length == 0 || string.IsNullOrEmpty(password) || password.Length == 0)
        return BadRequest("Username or password not supplied!");

      // Now see if a user exists
      if(!_dbc.Users.Any(x => x.Username.Equals(username)))
        return BadRequest($"User {username} not found. Please make sure you used the correct case.");

      // We passed above check, so the user exists
      var targetUser = _dbc.Users.First(x => x.Username.Equals(username));

      // Make sure they're actually enabled
      if(!targetUser.Enabled)
        return Unauthorized($"User {username} is not enabled.");

      // Now verify the password
      var hash = _hasher.VerifyHashedPassword(targetUser, targetUser.PasswordHash, password);
      if(hash == PasswordVerificationResult.Failed)
        return Unauthorized($"Invalid password for user {username}!");

      // Now generate a JWT
      var trm = _auth.Authenticate(targetUser, HttpContext);
      return Ok(trm);
    }

    [Route("debug_verify")]
    public IActionResult DebugVerify() {
      return UserToken.AttemptValidateToken(HttpContext, _auth, _dbc) ? Ok() : Unauthorized();
    }
  }
}
