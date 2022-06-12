using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace PSM.Core.Database.Tables;

[Table("UserTokens"), UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class UserToken {
  /// <summary>
  /// UserID this token belongs to
  /// </summary>
  [Key, Required]
  public int UserID { get; set; }

  /// <summary>
  /// The DateTimeOffset that this token expires
  /// </summary>
  [Required]
  public DateTimeOffset ExpiresAt { get; set; }

  /// <summary>
  /// The actual bearer value of this token
  /// </summary>
  [Required]
  public string TokenValue { get; set; } = null!;

  /// <summary>
  /// The address this token was originally created
  /// </summary>
  public string OriginatorAddress { get; set; } = null!;
  
  /// <summary>
  /// If this token allows cross-address usage
  /// </summary>
  public bool   OriginatorRoaming { get; set; }
}
