using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PSM.Core.Core.Database;
using PSM.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PSM.Core.Core.Auth {
    // This needs its interface type to work properly. I am displeased.
    public interface IAuthService {
        TokenResponseModel CreateJWT(User user);
        void ValidateJWT(HttpContext context, string token);
        User UserFromContext(HttpContext context);
    }

    public class AuthService : IAuthService {
        private readonly TokenValidationParameters validation_params;
        private readonly PSMContext dbcon;

        public AuthService(PSMContext _dbcon) {
            dbcon = _dbcon;
            // Setup validation params
            validation_params = new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(256)),
                ValidateIssuer = true,
                ValidIssuer = "PSM",
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidAudience = typeof(TokenResponseModel).Assembly.GetName().Name,
                ClockSkew = TimeSpan.FromMinutes(1), // 1 minute max time skew
                RequireSignedTokens = true,
                RequireExpirationTime = true,
            };
        }


        /// <summary>
        /// Creates a JWT for the user. Expires after 15 minutes.
        /// </summary>
        /// <param name="user">The user to make a token for</param>
        /// <returns>A <see cref="TokenResponseModel"/> containing the user's token and expiration time</returns>
        public TokenResponseModel CreateJWT(User user) {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long nowUnix = now.ToUnixTimeSeconds();

            DateTimeOffset expiry = now.AddMinutes(15); // 15 minutes
            Claim[] claims = new Claim[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Exp, expiry.ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Nbf, nowUnix.ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, validation_params.ValidIssuer),
                new Claim(JwtRegisteredClaimNames.Aud, validation_params.ValidAudience),
            };

            var token = new JwtSecurityToken(new JwtHeader(new SigningCredentials(validation_params.IssuerSigningKey, SecurityAlgorithms.HmacSha256)), new JwtPayload(claims));

            return new TokenResponseModel {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiry,
            };
        }

        /// <summary>
        /// Validates a JWT and adds the user to the context
        /// </summary>
        /// <param name="token"></param>
        public void ValidateJWT(HttpContext context, string token) {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, validation_params, out SecurityToken validatedToken);

            JwtSecurityToken jwtToken = (JwtSecurityToken) validatedToken;
            int userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
            context.Items["User"] = dbcon.Users.Where(x => x.Id == userId);
        }

        public User UserFromContext(HttpContext context) {
            // Make sure it exists
            if(!context.Items.ContainsKey("User")) {
                return null;
            }

            if(context.Items["User"].GetType() != typeof(User)) {
                return null;
            }

            return (User) context.Items["User"];
        }
    }
}
