using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PSM.Core.Core.Database;
using PSM.Core.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography;
using PSM.Core.Core.Auth;
using Microsoft.AspNetCore.Authorization;

namespace PSM.Core.Controllers.API {
    [Route("api/auth")]
    public class AuthController : Controller {
        private readonly PSMContext dbc;
        private readonly PasswordHasher<User> hasher;
        private readonly IAuthService auth;
        public AuthController(PSMContext _dbc, IAuthService _auth) {
            dbc = _dbc;
            hasher = new PasswordHasher<User>();
            auth = _auth;
        }

        [HttpPost]
        [Route("login")]
        [ProducesResponseType(typeof(TokenResponseModel), 200)]
        public IActionResult Login() {
            // Make sure they set the headers
			if(!Request.Headers.ContainsKey("Authorization")) {
				return Unauthorized("No Authorization header! Please authenticate with basic auth, using your username and password");
            }
            string authheader_str = Request.Headers["Authorization"];
            if(!authheader_str.StartsWith("Basic")) {
                return Unauthorized("Invalid auth header! Must be basic auth!");
            }

            AuthenticationHeaderValue authheader_obj = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);

            if(authheader_obj.Parameter == null) {
                return Unauthorized("Invalid auth header!");
            }

            // Get the info from basic auth
            byte[] credentialBytes = Convert.FromBase64String(authheader_obj.Parameter);
            string[] credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            string username = credentials[0];
            string password = credentials[1];

            if(username == null || username.Length == 0 || password == null || password.Length == 0) {
                return Unauthorized("Username or password not supplied!");
            }

            // Now see if a user exists
            if(!dbc.Users.Where(x => x.Username.Equals(username)).Any()) {
                return Unauthorized(string.Format("User {0} not found. Please make sure you used the correct case.", username));
            }

            // We passed above check, so the user exists
            User target_user = dbc.Users.Where(x => x.Username.Equals(username)).First();

            // Make sure theyre actually enabled
            if(!target_user.Enabled) {
                return Unauthorized(string.Format("User {0} is not enabled.", username));
            }

            // Now verify the password
            PasswordVerificationResult hashres = hasher.VerifyHashedPassword(target_user, target_user.PasswordHash, password);
            if(hashres == PasswordVerificationResult.Failed) {
                return Unauthorized(string.Format("Invalid password for user {0}!", username));
            }

            // Now generate a JWT
            TokenResponseModel trm = auth.CreateJWT(target_user);
            return Ok(trm);
        }

        [PSMAuth]
        [HttpGet("authtest")]
        public IActionResult AuthTest() {
            User user = auth.UserFromContext(HttpContext);
            return Ok(string.Format("Hello {0}", user.Username));
        }
    }
}
