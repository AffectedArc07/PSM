using Newtonsoft.Json;

namespace PSM.Core.Models.Auth;

public class ClientTokenModel : ModelBase {
  /// <summary>
  /// The value of the JWT.
  /// </summary>
  [JsonRequired]
  public string? Token { get; set; }

  /// <summary>
  /// When the token expires.
  /// </summary>
  [JsonRequired]
  public DateTimeOffset? ExpiresAt { get; set; }

  /// <summary>
  /// The user ID of this token
  /// </summary>
  public int userId { get; set; }

  protected override bool ValidateModel() {
    if(ExpiresAt!.Value.CompareTo(DateTimeOffset.UtcNow) <= 0)
      return false;

    return true;
  }
}
