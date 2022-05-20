using Microsoft.IdentityModel.Tokens;
using PSM.Core.Core.Database;
using PSM.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PSM.Core.Core.Auth {
    public interface IJWTRepository {
        TokenResponseModel Authenticate(User user);
		User? UserFromContext(HttpContext context);
    }

    public class JWTRepository : IJWTRepository {
        private readonly PSMContext dbc;
        public JWTRepository(PSMContext _dbc) {
            dbc = _dbc;
        }

		public TokenResponseModel Authenticate(User user) {
			JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
			DateTime expiration_time = DateTime.UtcNow.AddMinutes(15); // 15 minute lifetime
			SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor {
				Subject = new ClaimsIdentity(new Claim[] {
					new Claim("id", user.Id.ToString())
				}),
				Expires = expiration_time, 
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Constants.GetJWTBytes()), SecurityAlgorithms.HmacSha256Signature)
			};

			SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
			return new TokenResponseModel { Token = tokenHandler.WriteToken(token), ExpiresAt = expiration_time };
		}

        public User? UserFromContext(HttpContext context) {
            ClaimsIdentity? identity = context.User.Identity as ClaimsIdentity;
            
            if (identity != null) {
                int userid = int.Parse(identity.FindFirst("id").Value);
                if (dbc.Users.Where(x => x.Id == userid).Any()) {
                    User user = dbc.Users.Where(x => x.Id == userid).First();
                    // By only returning enabled users, we can handle disabling users while tokens are still alive
                    if(user.Enabled) {
                        return user;
                    }
                }
            }

            return null;
        }
    }
}
