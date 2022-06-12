using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using PSM.Core.Auth;
using PSM.Core.Database;
using PSM.Core.Database.Tables;
using PSM.Core.Models.Auth;

namespace PSM.Core.Controllers.API {
  [Route("api/auth")]
  public class AuthController : Controller {
    private readonly UserContext          _dbc;
    private readonly PasswordHasher<User> _hasher;
    private readonly IJWTRepository       _auth;

    public AuthController(UserContext dbc, IJWTRepository auth) {
      _dbc    = dbc;
      _hasher = new PasswordHasher<User>();
      _auth   = auth;
    }

    /// <summary>
    /// Takes a username and password using basic auth, and will return a token and expiration timestamp.
    /// </summary>
    /// <returns>A token response or reason for token refusal</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ClientTokenModel), 200)]
    public async Task<IActionResult> Login() {
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
      if(await _dbc.GetUserByUsername(username) is not { } dbUser)
        return BadRequest($"User {username} not found. Please make sure you used the correct case.");

      // Make sure they're actually enabled
      if(!dbUser.Enabled)
        return Unauthorized($"User {username} is not enabled.");

      // Now verify the password
      var hash = _hasher.VerifyHashedPassword(dbUser, dbUser.PasswordHash, password);
      if(hash == PasswordVerificationResult.Failed)
        return Unauthorized($"Invalid password for user {username}!");

      // Now generate a JWT
      var trm = await _auth.Authenticate(dbUser, HttpContext);
      return Ok(trm);
    }

    [HttpGet("debug_verify")]
    public async Task<IActionResult> DebugVerify() {
      return await _auth.UserFromContext(HttpContext) is { } ? Ok() : Unauthorized();
    }
  }
}
