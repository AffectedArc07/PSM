using System.ComponentModel.DataAnnotations.Schema;
using PSM.Core.Core.Database;

namespace PSM.Core.Models.Database;

[Table("users")]
public class User {
  public int    Id           { get; set; }
  public bool   Enabled      { get; set; }
  public string Username     { get; set; } = null!;
  public string PasswordHash { get; set; } = null!;
  public User?  CreatedBy    { get; set; }

  [NotMapped]
  public PermissionSet? PermissionSet { get; set; }
}
