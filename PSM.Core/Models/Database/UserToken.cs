using System.ComponentModel.DataAnnotations.Schema;

namespace PSM.Core.Models.Database;

[Table("user_tokens")]
public class UserToken {
  public int            Id                { get; set; }
  public DateTimeOffset ExpiresAt         { get; set; }
  public string         TokenValue        { get; set; }
  public string         OriginatorAddress { get; set; }
  public bool           OriginatorRoaming { get; set; } = false;
}
