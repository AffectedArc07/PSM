using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace PSM.Core.Database.Tables;

[Table("Users"), UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class User {
  /// <summary>
  /// The ID of this user
  /// </summary>
  [Key, Required, DefaultValue(1)]
  public int Id { get; set; }

  /// <summary>
  /// Whether the user is enabled and can log-in
  /// </summary>
  [Required, DefaultValue(false)]
  public bool Enabled { get; set; }

  /// <summary>
  /// Whether the user is archived and is hidden from normal use
  /// </summary>
  [Required, DefaultValue(false)]
  public bool Archived { get; set; }

  /// <summary>
  /// The actual username of this user
  /// </summary>
  [Required, MinLength(Constants.System.UsernameMinimumLength), MaxLength(Constants.System.UsernameMaximumLength)]
  public string Username { get; set; } = new('_', Constants.System.UsernameMinimumLength);

  /// <summary>
  /// The computed hash of the user, is not salted nor peppered, maybe open for future change
  /// </summary>
  [Required, DefaultValue("_")]
  public string PasswordHash { get; set; } = "_";

  /// <summary>
  /// The ID of the user who created this user
  /// </summary>
  public int? CreatedBy { get; set; }

  [NotMapped]
  public GlobalPermissionSet GlobalPermissionSet { get; set; } = null!;
}
