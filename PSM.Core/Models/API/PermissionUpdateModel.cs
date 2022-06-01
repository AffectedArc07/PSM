namespace PSM.Core.Models.API;

public class PermissionUpdateModel : ModelBase {
  /// <summary>
  /// UserID being modified
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  /// The new permission string of the user
  /// </summary>
  public string NewPermissions { get; set; } = null!;
}
