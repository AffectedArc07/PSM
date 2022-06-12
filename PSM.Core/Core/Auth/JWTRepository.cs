using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PSM.Core.Database;
using PSM.Core.Database.Tables;
using PSM.Core.Models.Auth;

namespace PSM.Core.Auth {
  public interface IJWTRepository {
    Task<ClientTokenModel> Authenticate(User           user, HttpContext context);
    Task<User?>            UserFromContext(HttpContext context);
  }

  public class JWTRepository : IJWTRepository {
    private readonly UserContext _dbc;

    public JWTRepository(UserContext dbc, PermissionContext psm) {
      _dbc = dbc.WithPermissionContext(psm);
    }

    public async Task<ClientTokenModel> Authenticate(User user, HttpContext context) {
      var tokenHandler   = new JwtSecurityTokenHandler();
      var expirationTime = DateTime.UtcNow.AddMinutes(15); // 15 minute lifetime
      var tokenDescriptor = new SecurityTokenDescriptor {
                                                          Subject = new ClaimsIdentity(new[] {
                                                                                               new Claim("id", user.Id.ToString())
                                                                                             }),
                                                          Expires            = expirationTime,
                                                          SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Constants.JWT.GetByteMap()), SecurityAlgorithms.HmacSha256Signature)
                                                        };

      var token     = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
      var userToken = await _dbc.GetToken(user.Id);
      userToken.ExpiresAt         = expirationTime;
      userToken.OriginatorAddress = Constants.GetRemoteFromContext(context);
      userToken.TokenValue        = token;
      await _dbc.SaveChangesAsync();

      return new ClientTokenModel { Token = token, ExpiresAt = expirationTime, userId = user.Id };
    }

    public async Task<User?> UserFromContext(HttpContext context) {
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
        var userToken = await _dbc.GetToken(id);
        if(!userToken.OriginatorRoaming && userToken.OriginatorAddress != Constants.GetRemoteFromContext(context))
          return null;
        if(userToken.TokenValue != auth)
          return null;
        user = await _dbc.GetUser(userToken.UserID);
      } else {
        if(await _dbc.GetTokenFromValue(auth) is not { } userToken)
          return null;
        if(!userToken.OriginatorRoaming && !userToken.OriginatorAddress.Equals(Constants.GetRemoteFromContext(context)))
          return null;
        user = await _dbc.GetUser(userToken.UserID);
      }

      return user.Enabled ? user : null;
    }
  }
}
