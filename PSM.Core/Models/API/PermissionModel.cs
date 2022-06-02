namespace PSM.Core.Models.API;

public class PermissionInformationModel : ModelBase {
  /// <summary>
  /// Name of the permission
  /// </summary>
  public string Name { get; set; }

  /// <summary>
  /// Internal ID of the permission
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  /// Description of the permission
  /// </summary>
  public string? Description { get; set; }
}
