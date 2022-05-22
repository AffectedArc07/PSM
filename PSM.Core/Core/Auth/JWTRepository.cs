using Microsoft.IdentityModel.Tokens;
using PSM.Core.Core.Database;
using PSM.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PSM.Core.Models.Auth;
using PSM.Core.Models.Database;

namespace PSM.Core.Core.Auth {
  public interface IJWTRepository {
    ClientTokenModel Authenticate(User           user, HttpContext context);
    User?            UserFromContext(HttpContext context);
  }

  public class JWTRepository : IJWTRepository {
    private readonly PSMContext _dbc;

    public JWTRepository(PSMContext dbc) {
      _dbc = dbc;
    }

    public ClientTokenModel Authenticate(User user, HttpContext context) {
      var tokenHandler   = new JwtSecurityTokenHandler();
      var expirationTime = DateTime.UtcNow.AddMinutes(15); // 15 minute lifetime
      var tokenDescriptor = new SecurityTokenDescriptor {
                                                          Subject = new ClaimsIdentity(new[] {
                                                                                               new Claim("id", user.Id.ToString())
                                                                                             }),
                                                          Expires            = expirationTime,
                                                          SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Constants.JWT.GetByteMap()), SecurityAlgorithms.HmacSha256Signature)
                                                        };

      var token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
      if(_dbc.UserTokens.FirstOrDefault(tok => tok.Id == user.Id) is { } userToken) {
        userToken.ExpiresAt         = expirationTime;
        userToken.OriginatorAddress = context.Connection.RemoteIpAddress.ToString();
        userToken.TokenValue        = token;
      } else {
        _dbc.UserTokens.Add(new UserToken {
                                            Id                = user.Id,
                                            ExpiresAt         = expirationTime,
                                            OriginatorAddress = context.Connection.RemoteIpAddress.ToString(),
                                            TokenValue        = token
                                          });
      }

      _dbc.SaveChanges();

      return new ClientTokenModel { Token = token, ExpiresAt = expirationTime };
    }

    public User? UserFromContext(HttpContext context) {
      if(context.User.Identity is not ClaimsIdentity identity)
        return null;
      if(!int.TryParse(identity.FindFirst("id").Value, out var userid))
        return null;
      if(_dbc.Users.FirstOrDefault(x => x.Id == userid) is not { } user)
        return null;
      // By only returning enabled users, we can handle disabling users while tokens are still alive
      return user.Enabled ? user : null;
    }
  }
}
