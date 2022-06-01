using System.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using PSM.Core.Core.Database;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
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

      return new ClientTokenModel { Token = token, ExpiresAt = expirationTime, userId = user.Id };
    }

    public User? UserFromContext(HttpContext context) {
      var auth = context.Request.Headers.Authorization.ToString();
      if(!auth.StartsWith("Bearer"))
        return null;
      auth = auth[6..];

      User? user;
      if(auth[0] != ' ') {
        var lastP    = auth.IndexOf(')');
        var idString = auth[1..lastP];
        auth = auth[(lastP + 1)..].Trim();
        if(!int.TryParse(idString, out var id))
          return null;
        if(_dbc.UserTokens.FirstOrDefault(dbToken => dbToken.Id == id) is not { } userToken)
          return null;
        if(!userToken.OriginatorRoaming && userToken.OriginatorAddress != Constants.GetRemoteFromContext(context))
          return null;
        if(userToken.TokenValue != auth)
          return null;
        user = _dbc.Users.First(dbUser => dbUser.Id == id);
      } else {
        var originator = Constants.GetRemoteFromContext(context);
        if(_dbc.UserTokens.FirstOrDefault(dbToken => dbToken.OriginatorAddress == originator) is not { } userToken)
          return null;
        if(userToken.OriginatorRoaming)
          return null;
        if(userToken.TokenValue != auth)
          return null;
        user = _dbc.Users.First(dbUser => dbUser.Id == userToken.Id);
      }

      user.PermissionSet = _dbc.PermissionSets.Find(user.Id);
      return user.Enabled ? user : null;
    }
  }
}
