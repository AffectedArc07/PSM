using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Net.Http.Headers;
using PSM.Core.Core;
using PSM.Core.Core.Auth;
using PSM.Core.Core.Database;

namespace PSM.Core.Models.Database;

[Table("user_tokens")]
public class UserToken {
  public int            Id                { get; set; }
  public DateTimeOffset ExpiresAt         { get; set; }
  public string         TokenValue        { get; set; }
  public string         OriginatorAddress { get; set; }
  public bool           OriginatorRoaming { get; set; } = false;

  public static bool AttemptValidateToken(HttpContext context, IJWTRepository jwtRepository, PSMContext psmContext) {
    UserToken? userToken = null;
    try {
      var user = jwtRepository.UserFromContext(context);
      userToken = psmContext.UserTokens.FirstOrDefault(userTok => userTok.Id == user.Id);
    } catch(Exception e) {
      Constants.AppLog.LogCritical("Exception validating Token: {Exception}", e);
    }

    if(userToken is null)
      return false;

    if(!userToken.OriginatorRoaming && userToken.OriginatorAddress != context.Connection.RemoteIpAddress.ToString())
      return false;

    var token = context.Request.Headers[HeaderNames.Authorization];
    if(!userToken.TokenValue.Equals(token))
      return false;

    return true;
  }
}
